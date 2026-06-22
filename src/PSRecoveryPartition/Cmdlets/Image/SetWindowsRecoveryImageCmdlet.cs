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

        [Parameter(Mandatory = true, Position = 0)]
        public FileInfo DestinationPath { get; set; }

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

            var destFile = DestinationPathResolver.Resolve(DestinationPath, SourceImagePath.Name);
            DestinationPathResolver.EnsureParentDirectoryExists(destFile);

            var skip = !Force.IsPresent && destFile.Exists
                && destFile.Length == SourceImagePath.Length
                && destFile.LastWriteTimeUtc == SourceImagePath.LastWriteTimeUtc;

            var target = SourceImagePath.FullName + " -> " + destFile.FullName;
            if (!skip && ShouldProcess(target, "Copy recovery image"))
            {
                File.Copy(SourceImagePath.FullName, destFile.FullName, true);
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
