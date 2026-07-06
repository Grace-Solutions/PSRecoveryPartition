using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Replaces the boot image associated with an existing recovery boot entry.
    /// The new image (<c>-BootImagePath</c>) is staged over the entry's current
    /// staged WIM on the recovery partition, so the entry immediately boots the
    /// new image. The BCD entry itself is left untouched (the staged path and
    /// boot.sdi are preserved), which keeps the operation atomic from the boot
    /// manager's point of view.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "WindowsRecoveryBootImage",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High,
        DefaultParameterSetName = "ByName")]
    [OutputType(typeof(WindowsRecoveryBootEntryInfo))]
    public sealed class SetWindowsRecoveryBootImageCmdlet : RecoveryCmdletBase
    {
        [Parameter(ParameterSetName = "ByInput", Mandatory = true, ValueFromPipeline = true)]
        public WindowsRecoveryBootEntryInfo InputObject { get; set; }

        [Parameter(ParameterSetName = "ByName", Mandatory = true, Position = 0)]
        [SupportsWildcards]
        public string Name { get; set; }

        [Parameter(ParameterSetName = "ByIdentifier", Mandatory = true)]
        public string Identifier { get; set; }

        // New source boot image to stage over the entry's current image.
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("SourceImagePath")]
        public FileInfo BootImagePath { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            if (BootImagePath == null || !BootImagePath.Exists)
            {
                throw new FileNotFoundException("Boot image was not found.",
                    BootImagePath != null ? BootImagePath.FullName : "<null>");
            }

            var bcd = new BcdEditEngine(this);

            // Resolve the set of entries to update. -Name is a wildcard and may
            // match several entries; -Identifier / -InputObject target exactly one.
            var targets = new System.Collections.Generic.List<KeyValuePair<string, string>>();
            if (InputObject != null)
            {
                targets.Add(new KeyValuePair<string, string>(InputObject.Identifier, InputObject.Name));
            }
            else if (!string.IsNullOrEmpty(Identifier))
            {
                targets.Add(new KeyValuePair<string, string>(Identifier, Identifier));
            }
            else
            {
                var pattern = WildcardPattern.Get(Name, WildcardOptions.IgnoreCase);
                foreach (var e in bcd.Enumerate(includeHidden: true).Where(e => pattern.IsMatch(e.Name ?? string.Empty)))
                {
                    targets.Add(new KeyValuePair<string, string>(e.Identifier, e.Name));
                }
            }

            if (targets.Count == 0)
            {
                WriteWarning("No matching boot entry was found.");
                return;
            }

            foreach (var t in targets)
            {
                ReplaceOne(bcd, t.Key, t.Value);
            }
        }

        private void ReplaceOne(BcdEditEngine bcd, string identifier, string entryName)
        {
            if (string.IsNullOrEmpty(identifier)) { return; }

            var loc = bcd.ResolveRamdiskStaging(identifier);
            if (!loc.IsRamdisk)
            {
                WriteError(new ErrorRecord(
                    new PSNotSupportedException(
                        "Boot entry '" + entryName + "' (" + identifier + ") is not a staged-WIM ramdisk entry, " +
                        "so there is no single boot image to replace. Recreate it with New-WindowsRecoveryBootEntry."),
                    "NotRamdiskEntry", ErrorCategory.InvalidOperation, identifier));
                return;
            }
            if (!loc.LocationResolved)
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException(
                        "Could not resolve the recovery partition backing boot entry '" + entryName + "' (" + identifier +
                        ", volume token '" + loc.VolumeToken + "'). The partition may be offline."),
                    "PartitionUnresolved", ErrorCategory.ObjectNotFound, identifier));
                return;
            }

            var target = "boot entry '" + entryName + "' (" + identifier + ") image -> " + BootImagePath.FullName;
            if (!Force.IsPresent && !ShouldProcess(target, "Replace recovery boot image")) { return; }

            VolumeStaging.WithVolumeRoot(loc.DiskNumber, loc.PartitionNumber, root =>
            {
                var destFull = JoinLocal(root, loc.ImageRelativePath);
                var destDir = Path.GetDirectoryName(destFull);
                if (!string.IsNullOrEmpty(destDir)) { Directory.CreateDirectory(destDir); }

                TransferLog.Attempt(this, download: false,
                    source: BootImagePath.FullName,
                    destination: loc.ImageRelativePath + " on disk " + loc.DiskNumber + " partition " + loc.PartitionNumber);
                WriteVerbose("Replacing staged boot image");
                NativeFileStaging.CopyFile(BootImagePath.FullName, destFull, p => ReportProgress("Replacing boot image", p));
            });

            if (PassThru.IsPresent)
            {
                var info = new WindowsRecoveryBootEntryInfo
                {
                    Identifier = identifier,
                    Name = entryName,
                    ObjectType = "Windows Boot Loader",
                    BootImagePath = BootImagePath,
                    StagedImagePath = loc.ImageRelativePath,
                    SdiPath = loc.SdiRelativePath,
                    DeviceOptionsIdentifier = loc.DeviceOptionsId,
                    DiskNumber = loc.DiskNumber,
                    PartitionNumber = loc.PartitionNumber,
                    IsRecoveryEntry = true
                };
                Stamp(info);
                WriteObject(info);
            }
        }

        // Join a managed junction root with a volume-relative path for file I/O.
        private static string JoinLocal(string root, string relative)
        {
            var rel = (relative ?? string.Empty).Replace('/', '\\').Trim('\\');
            return rel.Length == 0 ? root : Path.Combine(root, rel);
        }

        private void ReportProgress(string activity, int percent)
        {
            var pr = new ProgressRecord(1, activity, percent + "% complete")
            {
                PercentComplete = Math.Min(100, Math.Max(0, percent))
            };
            WriteProgress(pr);
        }
    }
}
