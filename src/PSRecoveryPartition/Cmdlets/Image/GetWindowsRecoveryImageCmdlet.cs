using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Security.Cryptography;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Finds Windows RE or Windows PE images. By default probes the active
    /// SystemRoot Recovery folder, what <c>reagentc /info</c> reports, and any
    /// recovery-tagged partition on every disk (transiently attaching NTFS via
    /// VolumeStaging when the volume has no drive letter). The <c>-Path</c>
    /// parameter restricts the search to a user-supplied directory and
    /// <c>-Source</c> filters the discovery channels.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "WindowsRecoveryImage")]
    [OutputType(typeof(WindowsRecoveryImageInfo))]
    public sealed class GetWindowsRecoveryImageCmdlet : RecoveryCmdletBase
    {
        [Parameter(ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
        public DirectoryInfo Path { get; set; }

        [Parameter]
        [ValidateSet("WindowsRE", "WindowsPE", "Boot", "Custom")]
        public string ImageKind { get; set; } = "WindowsRE";

        // Restricts discovery to one or more channels. UserPath honours -Path,
        // SystemRoot probes %WINDIR%\System32\Recovery and C:\Recovery, Reagent
        // reads what reagentc /info reports, and RecoveryPartition enumerates
        // recovery-tagged partitions across all disks (transient attach via
        // VolumeStaging). Default: all four.
        [Parameter]
        [ValidateSet("UserPath", "SystemRoot", "Reagent", "RecoveryPartition")]
        public string[] Source { get; set; }

        [Parameter]
        public SwitchParameter ComputeHash { get; set; }

        protected override void ProcessRecord()
        {
            ExecutionMethod = RecoveryExecutionMethod.Native;

            var sources = new HashSet<string>(
                Source != null && Source.Length > 0
                    ? Source
                    : new[] { "UserPath", "SystemRoot", "Reagent", "RecoveryPartition" },
                StringComparer.OrdinalIgnoreCase);

            var pattern = ImageKind == "WindowsRE" ? "Winre.wim" :
                          ImageKind == "WindowsPE" ? "winpe.wim" :
                          ImageKind == "Boot"      ? "boot.wim"  : "*.wim";

            if (Path != null && sources.Contains("UserPath"))
            {
                EmitFromDirectory(Path, pattern, "UserPath", null, null, null);
            }
            if (sources.Contains("SystemRoot"))
            {
                EmitSystemRootCandidates(pattern);
            }
            if (sources.Contains("Reagent"))
            {
                EmitReagentRegistered(pattern);
            }
            if (sources.Contains("RecoveryPartition"))
            {
                EmitFromRecoveryPartitions(pattern);
            }
        }

        private void EmitSystemRootCandidates(string pattern)
        {
            var sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (!string.IsNullOrEmpty(sys))
            {
                var systemRoot = Directory.GetParent(sys);
                if (systemRoot != null)
                {
                    EmitFromDirectory(new DirectoryInfo(System.IO.Path.Combine(systemRoot.FullName, "System32", "Recovery")),
                        pattern, "SystemRoot", null, null, null);
                }
            }
            EmitFromDirectory(new DirectoryInfo(@"C:\Recovery"), pattern, "SystemRoot", null, null, null);
        }

        private void EmitReagentRegistered(string pattern)
        {
            WindowsRecoveryEnvironmentInfo info;
            try { info = new WinReEngine(this).GetInfo(); }
            catch (Exception ex) { WriteVerbose("reagentc /info failed: " + ex.Message); return; }
            if (info == null || info.WindowsRELocation == null) { return; }
            EmitFromDirectory(info.WindowsRELocation, pattern, "Reagent", null, null, null);
        }

        private void EmitFromRecoveryPartitions(string pattern)
        {
            var engine = new RecoveryPartitionEngine(this);
            IList<RecoveryPartitionInfo> parts;
            try { parts = engine.Get(diskNumber: null, recoveryOnly: true); }
            catch (Exception ex) { WriteVerbose("Recovery partition enumeration failed: " + ex.Message); return; }
            foreach (var p in parts)
            {
                try
                {
                    VolumeStaging.WithVolumeRoot(p.DiskNumber, p.PartitionNumber, root =>
                    {
                        var winreDir = new DirectoryInfo(System.IO.Path.Combine(root, "Recovery", "WindowsRE"));
                        EmitFromDirectory(winreDir, pattern, "RecoveryPartition", p.DiskNumber, p.PartitionNumber, p.VolumePath);
                    });
                }
                catch (Exception ex)
                {
                    WriteVerbose("Disk " + p.DiskNumber + " partition " + p.PartitionNumber +
                                 ": staging failed (" + ex.Message + ")");
                }
            }
        }

        private void EmitFromDirectory(DirectoryInfo root, string pattern, string source, int? disk, int? partition, string volumePath)
        {
            if (root == null || !root.Exists) { return; }
            foreach (var file in root.EnumerateFiles(pattern, SearchOption.AllDirectories))
            {
                var info = new WindowsRecoveryImageInfo
                {
                    ImagePath        = file,
                    ImageKind        = ImageKind,
                    SizeBytes        = file.Length,
                    LastWriteTimeUtc = file.LastWriteTimeUtc,
                    Source           = source,
                    DiskNumber       = disk,
                    PartitionNumber  = partition,
                    VolumePath       = volumePath
                };
                if (ComputeHash.IsPresent)
                {
                    info.HashAlgorithm = "SHA256";
                    info.Hash = ComputeSha256(file);
                }
                Stamp(info);
                WriteObject(info);
            }
        }

        private static string ComputeSha256(FileInfo file)
        {
            using (var sha = SHA256.Create())
            using (var stream = file.OpenRead())
            {
                var bytes = sha.ComputeHash(stream);
                var sb = new System.Text.StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) { sb.Append(b.ToString("x2")); }
                return sb.ToString();
            }
        }
    }
}
