using System;
using System.IO;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Copies or updates a Windows RE or Windows PE image. Idempotent: the
    /// copy is skipped when the destination already matches the source by size
    /// and last-write timestamp.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "WindowsRecoveryImage", SupportsShouldProcess = true)]
    [OutputType(typeof(WindowsRecoveryImageInfo))]
    public sealed class SetWindowsRecoveryImageCmdlet : RecoveryCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public FileInfo SourceImagePath { get; set; }

        [Parameter(Mandatory = true)]
        public DirectoryInfo DestinationDirectory { get; set; }

        [Parameter]
        public string DestinationFileName { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            ExecutionMethod = RecoveryExecutionMethod.Native;
            if (SourceImagePath == null || !SourceImagePath.Exists)
            {
                throw new FileNotFoundException("Source recovery image was not found.", SourceImagePath != null ? SourceImagePath.FullName : "<null>");
            }
            if (!DestinationDirectory.Exists) { DestinationDirectory.Create(); }

            var destName = string.IsNullOrEmpty(DestinationFileName) ? SourceImagePath.Name : DestinationFileName;
            var destPath = Path.Combine(DestinationDirectory.FullName, destName);
            var destFile = new FileInfo(destPath);

            var skip = !Force.IsPresent && destFile.Exists
                && destFile.Length == SourceImagePath.Length
                && destFile.LastWriteTimeUtc == SourceImagePath.LastWriteTimeUtc;

            var target = SourceImagePath.FullName + " -> " + destPath;
            if (!skip && ShouldProcess(target, "Copy recovery image"))
            {
                File.Copy(SourceImagePath.FullName, destPath, true);
                destFile.Refresh();
            }

            if (PassThru.IsPresent)
            {
                var info = new WindowsRecoveryImageInfo
                {
                    ImagePath = destFile,
                    ImageKind = "WindowsRE",
                    SizeBytes = destFile.Length,
                    LastWriteTimeUtc = destFile.LastWriteTimeUtc
                };
                Stamp(info);
                WriteObject(info);
            }
        }
    }
}
