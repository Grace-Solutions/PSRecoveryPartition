using System;
using System.Collections;
using System.IO;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Downloads or copies a recovery boot image (WIM) to a local destination.
    /// Supports HTTP/HTTPS sources as well as local or UNC file paths and is
    /// idempotent when the destination matches the source by length.
    /// </summary>
    [Cmdlet(VerbsData.Save, "RecoveryBootImage", SupportsShouldProcess = true,
        DefaultParameterSetName = "ByUri")]
    [OutputType(typeof(WindowsRecoveryImageInfo))]
    public sealed class SaveRecoveryBootImageCmdlet : RecoveryCmdletBase
    {
        [Parameter(ParameterSetName = "ByUri", Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public Uri SourceUri { get; set; }

        [Parameter(ParameterSetName = "ByPath", Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public FileInfo SourcePath { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public FileInfo DestinationPath { get; set; }

        [Parameter]
        public string ImageKind { get; set; } = "WindowsPE";

        // Optional request headers for ByUri downloads. IDictionary so callers
        // can pass a literal hashtable; values are coerced via Convert.ToString
        // since headers go over the wire as strings regardless.
        [Parameter(ParameterSetName = "ByUri")]
        public IDictionary Headers { get; set; }

        // Apply NTFS Hidden / System attributes to the staged file after copy.
        // Off by default on the explicit save cmdlet so callers opt in; the
        // engine staging path inside New-RecoveryPartition applies them by
        // default since recovery payloads are normally hidden+system.
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
            if (ParameterSetName == "ByPath") { sourceLeaf = SourcePath != null ? SourcePath.Name : "bootimage.wim"; }
            else
            {
                var segments = SourceUri.Segments;
                sourceLeaf = segments.Length > 0 ? Uri.UnescapeDataString(segments[segments.Length - 1]) : "bootimage.wim";
            }

            var destFile = DestinationPathResolver.Resolve(DestinationPath, sourceLeaf);
            DestinationPathResolver.EnsureParentDirectoryExists(destFile);

            if (ParameterSetName == "ByPath")
            {
                if (SourcePath == null || !SourcePath.Exists)
                {
                    throw new FileNotFoundException("Source boot image was not found.",
                        SourcePath != null ? SourcePath.FullName : "<null>");
                }
                var skip = !Force.IsPresent && destFile.Exists
                    && destFile.Length == SourcePath.Length
                    && destFile.LastWriteTimeUtc == SourcePath.LastWriteTimeUtc;
                if (!skip && ShouldProcess(SourcePath.FullName + " -> " + destFile.FullName, "Copy recovery boot image"))
                {
                    File.Copy(SourcePath.FullName, destFile.FullName, true);
                    destFile.Refresh();
                }
            }
            else
            {
                if (ShouldProcess(SourceUri.ToString() + " -> " + destFile.FullName, "Download recovery boot image"))
                {
                    HttpImageDownloader
                        .DownloadAsync(SourceUri, destFile.FullName, Headers, this, "Save-RecoveryBootImage")
                        .GetAwaiter().GetResult();
                    destFile.Refresh();
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
                    ImageKind = ImageKind,
                    SizeBytes = destFile.Exists ? destFile.Length : 0,
                    LastWriteTimeUtc = destFile.Exists ? destFile.LastWriteTimeUtc : DateTimeOffset.MinValue
                };
                Stamp(info);
                WriteObject(info);
            }
        }

    }
}
