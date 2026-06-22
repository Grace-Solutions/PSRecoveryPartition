using System;
using System.IO;
using System.Management.Automation;
using System.Security.Cryptography;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Finds Windows RE or Windows PE images. Either probes the supplied
    /// <c>-Path</c> or scans common default locations.
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

        [Parameter]
        public SwitchParameter ComputeHash { get; set; }

        protected override void ProcessRecord()
        {
            ExecutionMethod = RecoveryExecutionMethod.Native;
            var roots = new System.Collections.Generic.List<DirectoryInfo>();
            if (Path != null) { roots.Add(Path); }
            else
            {
                var sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
                if (!string.IsNullOrEmpty(sys))
                {
                    var systemRoot = Directory.GetParent(sys);
                    if (systemRoot != null) { roots.Add(new DirectoryInfo(System.IO.Path.Combine(systemRoot.FullName, "System32", "Recovery"))); }
                }
                roots.Add(new DirectoryInfo(@"C:\Recovery"));
            }

            var pattern = ImageKind == "WindowsRE" ? "Winre.wim" :
                          ImageKind == "WindowsPE" ? "winpe.wim" :
                          ImageKind == "Boot"      ? "boot.wim"  : "*.wim";

            foreach (var root in roots)
            {
                if (!root.Exists) { continue; }
                foreach (var file in root.EnumerateFiles(pattern, SearchOption.AllDirectories))
                {
                    var info = new WindowsRecoveryImageInfo
                    {
                        ImagePath = file,
                        ImageKind = ImageKind,
                        SizeBytes = file.Length,
                        LastWriteTimeUtc = file.LastWriteTimeUtc
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
