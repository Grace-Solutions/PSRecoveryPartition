using System;
using System.IO;
using System.Management.Automation;
using System.Net.Http;
using System.Threading.Tasks;

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
        public DirectoryInfo DestinationDirectory { get; set; }

        [Parameter]
        public string DestinationFileName { get; set; }

        [Parameter]
        public string ImageKind { get; set; } = "WindowsPE";

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            ExecutionMethod = RecoveryExecutionMethod.Native;
            if (!DestinationDirectory.Exists) { DestinationDirectory.Create(); }

            string destName = DestinationFileName;
            if (string.IsNullOrEmpty(destName))
            {
                if (ParameterSetName == "ByPath") { destName = SourcePath.Name; }
                else
                {
                    var segments = SourceUri.Segments;
                    destName = segments.Length > 0 ? Uri.UnescapeDataString(segments[segments.Length - 1]) : "bootimage.wim";
                }
            }
            var destPath = Path.Combine(DestinationDirectory.FullName, destName);
            var destFile = new FileInfo(destPath);

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
                if (!skip && ShouldProcess(SourcePath.FullName + " -> " + destPath, "Copy recovery boot image"))
                {
                    File.Copy(SourcePath.FullName, destPath, true);
                    destFile.Refresh();
                }
            }
            else
            {
                if (ShouldProcess(SourceUri.ToString() + " -> " + destPath, "Download recovery boot image"))
                {
                    DownloadAsync(SourceUri, destPath).GetAwaiter().GetResult();
                    destFile.Refresh();
                }
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

        private async Task DownloadAsync(Uri uri, string destPath)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(30);
                using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var total = response.Content.Headers.ContentLength ?? -1L;
                    var progress = new ProgressRecord(1, "Save-RecoveryBootImage", "Downloading " + uri);
                    using (var src = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                    {
                        var buffer = new byte[81920];
                        long read = 0;
                        int n;
                        while ((n = await src.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                        {
                            await dst.WriteAsync(buffer, 0, n).ConfigureAwait(false);
                            read += n;
                            if (total > 0)
                            {
                                progress.PercentComplete = (int)((read * 100) / total);
                                WriteProgress(progress);
                            }
                        }
                        progress.RecordType = ProgressRecordType.Completed;
                        WriteProgress(progress);
                    }
                }
            }
        }
    }
}
