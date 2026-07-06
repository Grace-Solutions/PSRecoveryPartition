using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Internal engine for Windows Boot Configuration Data manipulation. Wraps
    /// <c>bcdedit.exe</c> via the controlled process fallback. Surface is kept
    /// internal because the public design forbids exposing the shell-out
    /// decision to users.
    /// </summary>
    internal sealed class BcdEditEngine
    {
        private readonly RecoveryCmdletBase _owner;

        public BcdEditEngine(RecoveryCmdletBase owner) { _owner = owner; }

        public IList<WindowsRecoveryBootEntryInfo> Enumerate(bool includeHidden = false)
        {
            var args = new List<string> { "/enum", "all" };
            var result = Run(args, "enumerate boot entries");
            var parsed = DeduplicateByIdentifier(ParseEnumerate(result.StandardOutput ?? string.Empty));
            foreach (var e in parsed)
            {
                e.ExecutionMethod = RecoveryExecutionMethod.ProcessFallback;
                e.ProcessFallbackUsed = true;
                e.ProcessResults.Add(result);
            }
            return includeHidden
                ? parsed
                : parsed.Where(p => p.Visibility == RecoveryBootEntryVisibility.Visible).ToList();
        }

        /// <summary>
        /// Creates a custom recovery boot entry. In <see cref="RecoveryBootMode.Ramdisk"/>
        /// mode a dedicated device-options object is created that points at the
        /// staged boot.sdi, and the loader boots the WIM as a ramdisk. In
        /// <see cref="RecoveryBootMode.Flat"/> mode the loader boots an
        /// already-expanded image in place on the partition (no ramdisk, no sdi).
        /// The caller supplies a transient drive-letter token that stays valid for
        /// the duration of these calls; bcdedit rewrites it to a persistent device
        /// id at parse time.
        /// </summary>
        public WindowsRecoveryBootEntryInfo Create(BcdBootEntryRequest req)
        {
            if (req == null) { throw new ArgumentNullException("req"); }
            if (string.IsNullOrEmpty(req.VolumeToken)) { throw new ArgumentException("A volume token (e.g. \"X:\") is required.", "req"); }

            var name = string.IsNullOrEmpty(req.Name) ? "Recovery" : req.Name;
            var token = req.VolumeToken.TrimEnd('\\');
            var loaderPath = string.IsNullOrEmpty(req.LoaderPath) ? @"\windows\system32\boot\winload.efi" : req.LoaderPath;

            // Track the objects we create so a failure partway through does not
            // leave a half-configured entry (and an orphaned device-options
            // object) behind. On any error we best-effort delete them and rethrow.
            string sdiId = null;
            string identifier = null;
            try
            {
                string deviceElement;
                if (req.Mode == RecoveryBootMode.Ramdisk)
                {
                    var imageRel = EnsureLeadingBackslash(req.ImageRelativePath);
                    var sdiRel = EnsureLeadingBackslash(req.SdiRelativePath);

                    // A dedicated device-options object so the entry points at *our*
                    // staged boot.sdi rather than the global {ramdiskoptions}.
                    var createSdi = Run(new List<string> { "/create", "/d", name + " ramdisk options", "/device" }, "create ramdisk device options");
                    sdiId = ExtractIdentifier(createSdi.StandardOutput);
                    if (string.IsNullOrEmpty(sdiId))
                    {
                        throw new InvalidOperationException(
                            "bcdedit.exe did not return a device-options identifier. Output: " + (createSdi.StandardOutput ?? "<empty>"));
                    }
                    Run(new List<string> { "/set", sdiId, "ramdisksdidevice", "partition=" + token }, "set ramdisk sdi device");
                    Run(new List<string> { "/set", sdiId, "ramdisksdipath", sdiRel }, "set ramdisk sdi path");
                    deviceElement = "ramdisk=[" + token + "]" + imageRel + "," + sdiId;
                }
                else
                {
                    deviceElement = "partition=" + token;
                }

                var create = Run(new List<string> { "/create", "/d", name, "/application", "osloader" }, "create boot entry");
                identifier = ExtractIdentifier(create.StandardOutput);
                if (string.IsNullOrEmpty(identifier))
                {
                    throw new InvalidOperationException(
                        "bcdedit.exe did not return a new boot entry identifier. Output: " + (create.StandardOutput ?? "<empty>"));
                }

                Run(new List<string> { "/set", identifier, "device", deviceElement }, "configure boot device");
                Run(new List<string> { "/set", identifier, "osdevice", deviceElement }, "configure boot osdevice");
                Run(new List<string> { "/set", identifier, "path", loaderPath }, "configure boot loader path");
                Run(new List<string> { "/set", identifier, "systemroot", req.SystemRoot ?? @"\Windows" }, "configure boot systemroot");
                if (req.Mode == RecoveryBootMode.Flat)
                {
                    Run(new List<string> { "/set", identifier, "detecthal", "yes" }, "configure hardware abstraction layer detection");
                }
                Run(new List<string> { "/set", identifier, "winpe", "yes" }, "mark boot entry as Windows PE");

                if (req.Visibility == RecoveryBootEntryVisibility.Hidden)
                {
                    Run(new List<string> { "/set", identifier, "displaymessage", "Recovery" }, "configure boot displaymessage");
                }
                Run(new List<string> { "/displayorder", identifier, req.AddLast ? "/addlast" : "/addfirst" }, "update displayorder");
                if (req.Timeout > TimeSpan.Zero)
                {
                    Run(new List<string> { "/timeout", ((int)req.Timeout.TotalSeconds).ToString() }, "set boot manager timeout");
                }
                if (req.SetDefault)
                {
                    Run(new List<string> { "/default", identifier }, "set boot entry as default");
                }

                var ramdisk = req.Mode == RecoveryBootMode.Ramdisk;
                return new WindowsRecoveryBootEntryInfo
                {
                    Identifier = identifier,
                    Name = name,
                    ObjectType = "Windows Boot Loader",
                    BootImagePath = req.StagedImage,
                    StagedImagePath = ramdisk ? EnsureLeadingBackslash(req.ImageRelativePath) : null,
                    SdiPath = ramdisk ? EnsureLeadingBackslash(req.SdiRelativePath) : null,
                    DeviceOptionsIdentifier = sdiId,
                    BootTimeout = req.Timeout,
                    Visibility = req.Visibility,
                    IsDefault = req.SetDefault,
                    IsRecoveryEntry = true,
                    ExecutionMethod = RecoveryExecutionMethod.ProcessFallback,
                    ProcessFallbackUsed = true
                };
            }
            catch
            {
                if (!string.IsNullOrEmpty(identifier)) { TryDelete(identifier); }
                if (!string.IsNullOrEmpty(sdiId)) { TryDelete(sdiId); }
                throw;
            }
        }

        private void TryDelete(string id)
        {
            try { Run(new List<string> { "/delete", id, "/f" }, "roll back BCD object " + id); }
            catch { /* best effort rollback */ }
        }

        private static readonly Regex RamdiskDeviceRegex = new Regex(
            @"ramdisk=\[(?<vol>[^\]]+)\](?<path>[^,\r\n]+),(?<sdi>\{[0-9a-fA-F\-]+\})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Reads a boot entry from the BCD store and resolves where its ramdisk
        /// payload (staged WIM + boot.sdi) lives, including the physical disk and
        /// partition when they can be mapped from the volume token. Returns an
        /// object with <see cref="RamdiskStagingLocation.IsRamdisk"/> = false when
        /// the entry is not a staged-WIM ramdisk entry (for example a flat/in-place
        /// entry), in which case there is no single image file to act on.
        /// </summary>
        public RamdiskStagingLocation ResolveRamdiskStaging(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) { throw new ArgumentNullException("identifier"); }

            var loc = new RamdiskStagingLocation();
            var enumRes = Run(new List<string> { "/enum", identifier, "/v" }, "read boot entry " + identifier);
            var m = RamdiskDeviceRegex.Match(enumRes.StandardOutput ?? string.Empty);
            if (!m.Success)
            {
                return loc; // not a ramdisk entry
            }

            loc.IsRamdisk = true;
            loc.VolumeToken = m.Groups["vol"].Value.Trim();
            loc.ImageRelativePath = EnsureLeadingBackslash(m.Groups["path"].Value.Trim());
            var sdiId = m.Groups["sdi"].Value.Trim();
            if (!sdiId.Equals("{ramdiskoptions}", StringComparison.OrdinalIgnoreCase))
            {
                loc.DeviceOptionsId = sdiId;
            }

            // The boot.sdi path lives on the referenced device-options object.
            var sdiTarget = loc.DeviceOptionsId ?? "{ramdiskoptions}";
            try
            {
                var sdiRes = Run(new List<string> { "/enum", sdiTarget, "/v" }, "read ramdisk device options " + sdiTarget);
                var sm = Regex.Match(sdiRes.StandardOutput ?? string.Empty,
                    @"ramdisksdipath\s+(?<p>\S.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (sm.Success) { loc.SdiRelativePath = EnsureLeadingBackslash(sm.Groups["p"].Value.Trim()); }
            }
            catch { /* best effort: sdi path is optional for cleanup */ }

            ResolveDiskPartition(loc);
            return loc;
        }

        // Maps the BCD volume token to a physical disk/partition so file I/O can go
        // through the module's transient-junction staging path.
        private static void ResolveDiskPartition(RamdiskStagingLocation loc)
        {
            var token = loc.VolumeToken;
            if (string.IsNullOrEmpty(token)) { return; }

            string deviceName = null;
            if (token.StartsWith(@"\Device\", StringComparison.OrdinalIgnoreCase))
            {
                deviceName = @"\\?\GLOBALROOT" + token;
            }
            else if (token.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                deviceName = token;
            }
            else if (token.Length == 2 && token[1] == ':')
            {
                // Drive-letter token: match by mount point.
                var vol = Native.Win32VolumeMapper.EnumerateAll()
                    .FirstOrDefault(v => v.MountPoints != null &&
                        v.MountPoints.Any(mp => mp.TrimEnd('\\').Equals(token, StringComparison.OrdinalIgnoreCase)));
                if (vol != null) { deviceName = vol.VolumeName; }
            }
            if (deviceName == null) { return; }

            int disk;
            long offset;
            if (!Native.Win32VolumeMapper.TryGetDiskAndOffset(deviceName, out disk, out offset)) { return; }

            try
            {
                var info = Native.Win32DiskInfoReader.Read(disk);
                var part = info.Partitions.FirstOrDefault(p => p.StartingOffset == offset);
                if (part != null)
                {
                    loc.DiskNumber = disk;
                    loc.PartitionNumber = part.PartitionNumber;
                    loc.LocationResolved = true;
                }
            }
            catch { /* disk not readable; leave unresolved */ }
        }

        private static string EnsureLeadingBackslash(string relative)
        {
            var r = (relative ?? string.Empty).Replace('/', '\\');
            if (r.Length == 0) { return "\\"; }
            return r.StartsWith("\\", StringComparison.Ordinal) ? r : "\\" + r;
        }

        public void Remove(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) { throw new ArgumentNullException("identifier"); }

            // Capture the ramdisk device-options object this entry references (if
            // any) before we delete the loader, so we can clean it up afterwards
            // and not leave it orphaned.
            var deviceOptions = TryGetRamdiskDeviceOptionsId(identifier);

            Run(new List<string> { "/delete", identifier, "/f", "/cleanup" }, "remove boot entry " + identifier);

            if (!string.IsNullOrEmpty(deviceOptions) && !IsDeviceOptionsReferenced(deviceOptions))
            {
                try { Run(new List<string> { "/delete", deviceOptions, "/f" }, "remove orphaned ramdisk device options " + deviceOptions); }
                catch { /* best effort: shared or already gone */ }
            }
        }

        // Reads a boot entry and returns the device-options {guid} its ramdisk
        // device element references, or null. The global {ramdiskoptions} is never
        // returned so we never delete the shared object.
        private string TryGetRamdiskDeviceOptionsId(string identifier)
        {
            try
            {
                var res = Run(new List<string> { "/enum", identifier, "/v" }, "read boot entry " + identifier);
                var m = Regex.Match(res.StandardOutput ?? string.Empty,
                    @"ramdisk=\[[^\]]*\][^,\r\n]*,(\{[0-9a-fA-F\-]+\})", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var g = m.Groups[1].Value;
                    if (!g.Equals("{ramdiskoptions}", StringComparison.OrdinalIgnoreCase)) { return g; }
                }
            }
            catch { /* best effort */ }
            return null;
        }

        // True when any remaining boot entry still references the device-options
        // object (a ramdisk reference appears as ",{guid}" in a device/osdevice
        // line; the object's own "identifier {guid}" line has no leading comma).
        private bool IsDeviceOptionsReferenced(string deviceOptionsId)
        {
            try
            {
                var res = Run(new List<string> { "/enum", "all", "/v" }, "enumerate boot entries");
                return (res.StandardOutput ?? string.Empty)
                    .IndexOf("," + deviceOptionsId, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return true; } // if unsure, keep the object
        }

        internal RecoveryProcessExecutionResult Run(IList<string> argumentList, string operationDescription)
        {
            var request = new RecoveryProcessExecutionRequest
            {
                FilePath = InboxTools.Bcdedit,
                ArgumentList = argumentList,
                AcceptableExitCodeList = new List<int> { 0 },
                LogOutput = true,
                ExecutionTimeout = TimeSpan.FromMinutes(2),
                ExecutionTimeoutInterval = TimeSpan.FromMilliseconds(250),
                WindowStyle = "Hidden",
                Priority = "Normal"
            };
            var result = ProcessExecution.Run(request, _owner);
            _owner.RecordProcessResult(result);
            if (!result.ExitCodeAccepted)
            {
                throw ProcessFallbackExceptions.BuildFailure(
                    "Could not " + operationDescription, result);
            }
            return result;
        }

        private static readonly Regex IdentifierRegex = new Regex(@"\{[0-9a-fA-F\-]+\}", RegexOptions.Compiled);
        private static readonly Regex EntryHeaderRegex = new Regex(@"^[A-Za-z].*$", RegexOptions.Compiled);

        private static string ExtractIdentifier(string output)
        {
            if (string.IsNullOrEmpty(output)) { return null; }
            var m = IdentifierRegex.Match(output);
            return m.Success ? m.Value : null;
        }

        /// <summary>
        /// Collapses entries that share a boot identifier. <c>bcdedit /enum all</c>
        /// can list the same BCD object under more than one group, which otherwise
        /// yields visually identical rows. The first occurrence wins; entries with
        /// no identifier are always kept.
        /// </summary>
        internal static IList<WindowsRecoveryBootEntryInfo> DeduplicateByIdentifier(IList<WindowsRecoveryBootEntryInfo> entries)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<WindowsRecoveryBootEntryInfo>(entries.Count);
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.Identifier) || seen.Add(e.Identifier))
                {
                    result.Add(e);
                }
            }
            return result;
        }

        /// <summary>
        /// True when a BCD object's group is "Windows Boot Loader" -- the only
        /// group that represents an actual bootable OS/WinPE entry. Supporting
        /// objects (Device options, Firmware Application, Resume from Hibernate,
        /// settings groups) are excluded from recovery-entry classification.
        /// </summary>
        private static bool IsBootLoader(string objectType)
        {
            return objectType != null &&
                objectType.Trim().Equals("Windows Boot Loader", StringComparison.OrdinalIgnoreCase);
        }

        internal static IList<WindowsRecoveryBootEntryInfo> ParseEnumerate(string output)
        {
            var entries = new List<WindowsRecoveryBootEntryInfo>();
            if (string.IsNullOrWhiteSpace(output)) { return entries; }

            var lines = output.Replace("\r", string.Empty).Split('\n');
            WindowsRecoveryBootEntryInfo current = null;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) { continue; }
                if (line.StartsWith("---"))
                {
                    if (current != null) { entries.Add(current); }
                    var header = i > 0 ? lines[i - 1].Trim() : string.Empty;
                    current = new WindowsRecoveryBootEntryInfo
                    {
                        ObjectType = header,
                        Name = header,
                        Visibility = RecoveryBootEntryVisibility.Visible
                    };
                    continue;
                }
                if (current == null) { continue; }
                if (line.StartsWith("identifier", StringComparison.OrdinalIgnoreCase))
                {
                    current.Identifier = line.Substring("identifier".Length).Trim();
                }
                else if (line.StartsWith("description", StringComparison.OrdinalIgnoreCase))
                {
                    var v = line.Substring("description".Length).Trim();
                    if (!string.IsNullOrEmpty(v)) { current.Name = v; }
                    // Only a bootable loader object counts as a recovery entry. A
                    // "Device options" object can also carry a "Windows Recovery"
                    // description but is just the ramdisk SDI support object.
                    if (IsBootLoader(current.ObjectType) && v.IndexOf("Recovery", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        current.IsRecoveryEntry = true;
                    }
                }
                else if (line.StartsWith("winpe", StringComparison.OrdinalIgnoreCase))
                {
                    var v = line.Substring("winpe".Length).Trim();
                    if (IsBootLoader(current.ObjectType) && v.IndexOf("Yes", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        current.IsRecoveryEntry = true;
                    }
                }
                else if (line.StartsWith("default", StringComparison.OrdinalIgnoreCase))
                {
                    current.IsDefault = true;
                }
            }
            if (current != null) { entries.Add(current); }
            return entries;
        }
    }
}
