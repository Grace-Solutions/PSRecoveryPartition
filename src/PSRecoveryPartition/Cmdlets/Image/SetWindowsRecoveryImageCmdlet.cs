using System;
using System.Collections;
using System.IO;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Copies or updates a Windows RE or Windows PE image. Accepts either a
    /// local source (<c>-SourceImagePath</c>) or an HTTP/HTTPS URI
    /// (<c>-SourceUri</c> with optional <c>-Headers</c> for authenticated
    /// downloads). Idempotent on the local source path: the copy is skipped
    /// when the destination already matches the source by size and last-write
    /// timestamp.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "WindowsRecoveryImage",
        SupportsShouldProcess = true,
        DefaultParameterSetName = "ByPath")]
    [OutputType(typeof(WindowsRecoveryImageInfo))]
    public sealed class SetWindowsRecoveryImageCmdlet : RecoveryCmdletBase
    {
        [Parameter(ParameterSetName = "ByPath", Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public FileInfo SourceImagePath { get; set; }

        [Parameter(ParameterSetName = "ByUri", Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public Uri SourceUri { get; set; }

        // Optional request headers for ByUri downloads. IDictionary so callers
        // can pass a hashtable literal; values are coerced via Convert.ToString.
        [Parameter(ParameterSetName = "ByUri")]
        public IDictionary Headers { get; set; }

        [Parameter(Mandatory = true, Position = 0)]
        public FileInfo DestinationPath { get; set; }

        // NTFS Hidden / System attributes applied to the staged image after
        // copy. Off by default; callers opt in. The engine staging path inside
        // New-RecoveryPartition still applies them on its own.
        [Parameter]
        public SwitchParameter Hidden { get; set; }

        [Parameter]
        public SwitchParameter System { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            ExecutionMethod = RecoveryExecutionMethod.Native;

            string sourceLeaf;
            if (ParameterSetName == "ByUri")
            {
                if (SourceUri == null) { throw new ArgumentNullException("SourceUri"); }
                var segs = SourceUri.Segments;
                sourceLeaf = segs.Length > 0 ? Uri.UnescapeDataString(segs[segs.Length - 1]) : "Winre.wim";
            }
            else
            {
                if (SourceImagePath == null || !SourceImagePath.Exists)
                {
                    throw new FileNotFoundException("Source recovery image was not found.",
                        SourceImagePath != null ? SourceImagePath.FullName : "<null>");
                }
                sourceLeaf = SourceImagePath.Name;
            }

            var destFile = DestinationPathResolver.Resolve(DestinationPath, sourceLeaf);
            DestinationPathResolver.EnsureParentDirectoryExists(destFile);
            WriteVerbose("Destination resolved to '" + destFile.FullName + "'.");

            if (ParameterSetName == "ByPath")
            {
                var skip = !Force.IsPresent && destFile.Exists
                    && destFile.Length == SourceImagePath.Length
                    && destFile.LastWriteTimeUtc == SourceImagePath.LastWriteTimeUtc;
                var target = SourceImagePath.FullName + " -> " + destFile.FullName;
                if (skip)
                {
                    WriteVerbose("Destination matches source (size + timestamp); skipping copy. Use -Force to overwrite.");
                }
                else if (ShouldProcess(target, "Copy recovery image"))
                {
                    TransferLog.Attempt(this, download: false, source: SourceImagePath.FullName, destination: destFile.FullName);
                    File.Copy(SourceImagePath.FullName, destFile.FullName, true);
                    destFile.Refresh();
                }
            }
            else
            {
                if (ShouldProcess(SourceUri.ToString() + " -> " + destFile.FullName, "Download recovery image"))
                {
                    HttpImageDownloader.RunConditional(
                        SourceUri, Headers, destFile, Force.IsPresent, "Set-WindowsRecoveryImage", this, WriteVerbose);
                }
            }

            if (destFile.Exists && (Hidden.IsPresent || this.System.IsPresent))
            {
                VolumeStaging.ApplyAttributes(destFile.FullName, Hidden.IsPresent, this.System.IsPresent);
                destFile.Refresh();
            }

            if (PassThru.IsPresent)
            {
                var info = new WindowsRecoveryImageInfo
                {
                    ImagePath = destFile,
                    ImageKind = "WindowsRE",
                    SizeBytes = destFile.Exists ? destFile.Length : 0,
                    LastWriteTimeUtc = destFile.Exists ? destFile.LastWriteTimeUtc : DateTimeOffset.MinValue
                };
                Stamp(info);
                WriteObject(info);
            }
        }
    }
}
