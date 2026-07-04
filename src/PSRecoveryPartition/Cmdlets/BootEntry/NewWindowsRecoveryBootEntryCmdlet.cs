using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using PSRecoveryPartition.Native;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Creates a custom recovery boot entry from a boot image. This is the
    /// customizable counterpart to the fixed Windows RE mechanism: the source
    /// image (<c>-BootImagePath</c>) is staged onto a destination and a matching
    /// BCD entry is created.
    ///
    /// <para>Destination (parameter sets):</para>
    /// <list type="bullet">
    ///   <item><b>ByRecoveryPartition</b> - a <see cref="RecoveryPartitionInfo"/>
    ///   (typically piped from <c>Get-RecoveryPartition</c>); files are copied
    ///   straight onto the partition through its <c>\\?\GLOBALROOT</c> path with no
    ///   drive letter or persistent mount.</item>
    ///   <item><b>ByTargetPath</b> - an already-mounted directory.</item>
    ///   <item><b>ByImagePath</b> (default) - the image is already at its final
    ///   location; only the BCD entry (and a co-located boot.sdi) is created.</item>
    /// </list>
    ///
    /// <para>By default the WIM is staged as-is and booted as a ramdisk (needs a
    /// boot.sdi, resolved automatically or via <c>-BootSdiPath</c>). With
    /// <c>-ExpandBootImage</c> the image is expanded flat onto the destination
    /// (non-RAM boot) and the entry is wired to boot it in place. The staging
    /// sub-path defaults to <c>\Recovery\WindowsRE</c> but is fully overridable
    /// via <c>-StagingRelativePath</c> (including the volume root).</para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "WindowsRecoveryBootEntry",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High,
        DefaultParameterSetName = "ByImagePath")]
    [OutputType(typeof(WindowsRecoveryBootEntryInfo))]
    public sealed class NewWindowsRecoveryBootEntryCmdlet : RecoveryCmdletBase
    {
        // Source boot image for every parameter set.
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("SourceImagePath")]
        public FileInfo BootImagePath { get; set; }

        [Parameter(ParameterSetName = "ByRecoveryPartition", Mandatory = true, ValueFromPipeline = true)]
        public RecoveryPartitionInfo RecoveryPartition { get; set; }

        [Parameter(ParameterSetName = "ByTargetPath", Mandatory = true)]
        public DirectoryInfo TargetPath { get; set; }

        // Volume-relative folder the image (and boot.sdi) are staged into. Default
        // \Recovery\WindowsRE; pass an empty string to stage at the volume root.
        [Parameter]
        public string StagingRelativePath { get; set; } = @"\Recovery\WindowsRE";

        // Expand the image flat onto the destination (non-RAM boot) instead of
        // staging the WIM for ramdisk boot.
        [Parameter]
        public SwitchParameter ExpandBootImage { get; set; }

        // 1-based image index inside the WIM to expand (ExpandBootImage only).
        [Parameter]
        public int ImageIndex { get; set; } = 1;

        // Explicit boot.sdi (ramdisk mode). When omitted it is resolved from the
        // live OS, then extracted from the boot image.
        [Parameter]
        public FileInfo BootSdiPath { get; set; }

        [Parameter]
        public string Name { get; set; } = "Grace Solutions Recovery";

        [Parameter]
        public TimeSpan BootTimeout { get; set; } = TimeSpan.FromSeconds(10);

        [Parameter]
        public RecoveryBootEntryVisibility BootEntryVisibility { get; set; } = RecoveryBootEntryVisibility.Visible;

        [Parameter]
        public SwitchParameter SetDefault { get; set; }

        [Parameter]
        public SwitchParameter AddLast { get; set; }

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
            if (ExpandBootImage.IsPresent)
            {
                var count = WimgApi.GetImageCount(BootImagePath.FullName);
                if (ImageIndex < 1 || ImageIndex > count)
                {
                    throw new ArgumentOutOfRangeException("ImageIndex", ImageIndex,
                        "The boot image contains " + count + " image(s).");
                }
            }

            WindowsRecoveryBootEntryInfo created;
            switch (ParameterSetName)
            {
                case "ByRecoveryPartition": created = CreateOnRecoveryPartition(); break;
                case "ByTargetPath":        created = CreateOnMountedPath();       break;
                default:                    created = CreateFromInPlaceImage();    break;
            }

            if (created != null)
            {
                Stamp(created);
                if (PassThru.IsPresent) { WriteObject(created); }
            }
        }

        // --- Destination: a recovery partition, written via \\?\GLOBALROOT (no mount) ---
        private WindowsRecoveryBootEntryInfo CreateOnRecoveryPartition()
        {
            var part = RecoveryPartition;
            var nativeRoot = !string.IsNullOrEmpty(part.GlobalRootPath) ? part.GlobalRootPath : part.VolumePath;
            if (string.IsNullOrEmpty(nativeRoot))
            {
                throw new InvalidOperationException(
                    "The recovery partition exposes neither a GlobalRootPath nor a VolumePath to stage onto.");
            }
            var target = "disk " + part.DiskNumber + " partition " + part.PartitionNumber +
                         " (" + (ExpandBootImage.IsPresent ? "flat expand" : "stage WIM") + ")";
            if (!Force.IsPresent && !ShouldProcess(target, "Create recovery boot entry")) { return null; }

            var req = BuildBaseRequest();

            if (ExpandBootImage.IsPresent)
            {
                // WIMGAPI needs an ordinary FS path; use a transient junction (no
                // drive letter) to expand, then the drive letter only for bcdedit.
                VolumeStaging.WithVolumeRoot(part.DiskNumber, part.PartitionNumber, root =>
                {
                    var destDir = JoinLocal(root, StagingRelativePath);
                    ReportActivity("Expanding boot image (flat)");
                    WimgApi.ApplyImage(BootImagePath.FullName, ImageIndex, destDir, null, p => ReportProgress("Expanding boot image", p));
                });
                ConfigureFlat(req);
            }
            else
            {
                // Copy WIM + boot.sdi straight onto the partition with no mount.
                var wimDest = NativeFileStaging.Combine(nativeRoot, VolRel(StagingRelativePath, BootImagePath.Name));
                NativeFileStaging.EnsureDirectory(NativeFileStaging.Combine(nativeRoot, VolRel(StagingRelativePath)));
                ReportActivity("Staging boot image");
                NativeFileStaging.CopyFile(BootImagePath.FullName, wimDest, p => ReportProgress("Staging boot image", p));

                var sdi = ResolveSdi();
                var sdiDest = NativeFileStaging.Combine(nativeRoot, VolRel(StagingRelativePath, "boot.sdi"));
                NativeFileStaging.CopyFile(sdi.FullName, sdiDest, null);
                ConfigureRamdisk(req);
            }

            WindowsRecoveryBootEntryInfo created = null;
            VolumeStaging.WithDriveLetter(part.DiskNumber, part.PartitionNumber, letter =>
            {
                req.VolumeToken = letter;
                created = new BcdEditEngine(this).Create(req);
            });
            return created;
        }

        // --- Destination: an already-mounted directory ---
        private WindowsRecoveryBootEntryInfo CreateOnMountedPath()
        {
            var root = TargetPath.FullName;
            var token = (Path.GetPathRoot(root) ?? root).TrimEnd('\\');
            var target = root + " (" + (ExpandBootImage.IsPresent ? "flat expand" : "stage WIM") + ")";
            if (!Force.IsPresent && !ShouldProcess(target, "Create recovery boot entry")) { return null; }

            var req = BuildBaseRequest();
            req.VolumeToken = token;

            if (ExpandBootImage.IsPresent)
            {
                var destDir = JoinLocal(root, StagingRelativePath);
                ReportActivity("Expanding boot image (flat)");
                WimgApi.ApplyImage(BootImagePath.FullName, ImageIndex, destDir, null, p => ReportProgress("Expanding boot image", p));
                ConfigureFlat(req);
            }
            else
            {
                var destDir = JoinLocal(root, StagingRelativePath);
                Directory.CreateDirectory(destDir);
                var wimDest = Path.Combine(destDir, BootImagePath.Name);
                ReportActivity("Staging boot image");
                NativeFileStaging.CopyFile(BootImagePath.FullName, wimDest, p => ReportProgress("Staging boot image", p));

                var sdi = ResolveSdi();
                File.Copy(sdi.FullName, Path.Combine(destDir, "boot.sdi"), true);
                ConfigureRamdisk(req);
            }

            return new BcdEditEngine(this).Create(req);
        }

        // --- Destination: image already staged; only wire the BCD entry ---
        private WindowsRecoveryBootEntryInfo CreateFromInPlaceImage()
        {
            if (ExpandBootImage.IsPresent)
            {
                throw new PSNotSupportedException(
                    "-ExpandBootImage requires a destination: use -RecoveryPartition or -TargetPath. " +
                    "The default ByImagePath set only wires a BCD entry to an image already in place.");
            }
            if (!Force.IsPresent && !ShouldProcess("BCD: '" + Name + "' -> " + BootImagePath.FullName, "Create recovery boot entry"))
            {
                return null;
            }

            var dir = BootImagePath.Directory;
            var volumeRoot = dir != null ? dir.Root.FullName.TrimEnd('\\') : "C:";
            var imageRel = BootImagePath.FullName.Substring(volumeRoot.Length);

            // Place boot.sdi alongside the in-place image.
            var sdi = ResolveSdi();
            var sdiTarget = dir != null ? Path.Combine(dir.FullName, "boot.sdi") : null;
            if (sdiTarget != null && !string.Equals(sdi.FullName, sdiTarget, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sdi.FullName, sdiTarget, true);
            }
            var sdiRel = sdiTarget != null ? sdiTarget.Substring(volumeRoot.Length) : VolRel(null, "boot.sdi");

            var req = BuildBaseRequest();
            req.VolumeToken = volumeRoot;
            req.Mode = RecoveryBootMode.Ramdisk;
            req.ImageRelativePath = imageRel;
            req.SdiRelativePath = sdiRel;
            req.SystemRoot = @"\Windows";
            req.LoaderPath = @"\windows\system32\" + WinloadLeaf();

            return new BcdEditEngine(this).Create(req);
        }

        // --- Shared request assembly ---
        private BcdBootEntryRequest BuildBaseRequest()
        {
            return new BcdBootEntryRequest
            {
                Name = Name,
                Timeout = BootTimeout,
                Visibility = BootEntryVisibility,
                SetDefault = SetDefault.IsPresent,
                AddLast = AddLast.IsPresent,
                StagedImage = BootImagePath
            };
        }

        private void ConfigureRamdisk(BcdBootEntryRequest req)
        {
            req.Mode = RecoveryBootMode.Ramdisk;
            req.ImageRelativePath = VolRel(StagingRelativePath, BootImagePath.Name);
            req.SdiRelativePath = VolRel(StagingRelativePath, "boot.sdi");
            req.SystemRoot = @"\Windows";
            req.LoaderPath = @"\windows\system32\" + WinloadLeaf();
        }

        private void ConfigureFlat(BcdBootEntryRequest req)
        {
            req.Mode = RecoveryBootMode.Flat;
            // device is partition=<token>; systemroot / loader path include the
            // staging sub-path so an image expanded into a subfolder still boots.
            req.SystemRoot = VolRel(StagingRelativePath, "Windows");
            req.LoaderPath = VolRel(StagingRelativePath, @"windows\system32\" + WinloadLeaf());
        }

        private FileInfo ResolveSdi()
        {
            var scratch = Path.Combine(Path.GetTempPath(), "PSRecoveryPartition-sdi-" + Guid.NewGuid().ToString("N"));
            return BootSdiResolver.Resolve(BootSdiPath, BootImagePath, ImageIndex, scratch, WriteVerbose);
        }

        private static string WinloadLeaf()
        {
            return FirmwareInfo.WinloadLeaf();
        }

        // Join a managed root with a volume-relative sub-path for local file I/O.
        private static string JoinLocal(string root, string relative)
        {
            var rel = (relative ?? string.Empty).Replace('/', '\\').Trim('\\');
            return rel.Length == 0 ? root : Path.Combine(root, rel);
        }

        // Build a leading-backslash volume-relative path from segments.
        private static string VolRel(params string[] parts)
        {
            var sb = new StringBuilder();
            if (parts != null)
            {
                foreach (var p in parts)
                {
                    if (string.IsNullOrEmpty(p)) { continue; }
                    var seg = p.Replace('/', '\\').Trim('\\');
                    if (seg.Length == 0) { continue; }
                    sb.Append('\\').Append(seg);
                }
            }
            return sb.Length == 0 ? "\\" : sb.ToString();
        }

        private void ReportActivity(string activity)
        {
            WriteVerbose(activity + "...");
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
