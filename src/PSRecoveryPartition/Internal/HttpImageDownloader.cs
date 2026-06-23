using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Net.Http;
using System.Threading.Tasks;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Shared HTTP/HTTPS download surface for recovery image cmdlets. Streams the
    /// response to disk with progress reporting and supports a caller-supplied
    /// header dictionary so HTTP authentication scenarios (bearer tokens, basic
    /// auth, custom signing headers) are covered without a bespoke download path
    /// per cmdlet.
    /// </summary>
    internal static class HttpImageDownloader
    {
        public static async Task DownloadAsync(
            Uri uri,
            string destPath,
            IDictionary headers,
            PSCmdlet owner,
            string activity,
            TimeSpan? timeout = null)
        {
            if (uri == null) { throw new ArgumentNullException("uri"); }
            if (string.IsNullOrEmpty(destPath)) { throw new ArgumentNullException("destPath"); }

            using (var client = new HttpClient())
            {
                client.Timeout = timeout ?? TimeSpan.FromMinutes(30);
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    ApplyHeaders(request, headers);
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var total = response.Content.Headers.ContentLength ?? -1L;
                        var progress = new ProgressRecord(1, activity ?? "Download", "Downloading " + uri);
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
                                if (total > 0 && owner != null)
                                {
                                    progress.PercentComplete = (int)((read * 100) / total);
                                    owner.WriteProgress(progress);
                                }
                            }
                            if (owner != null)
                            {
                                progress.RecordType = ProgressRecordType.Completed;
                                owner.WriteProgress(progress);
                            }
                        }
                    }
                }
            }
        }

        private static void ApplyHeaders(HttpRequestMessage request, IDictionary headers)
        {
            if (headers == null) { return; }
            foreach (DictionaryEntry entry in headers)
            {
                if (entry.Key == null) { continue; }
                var name = Convert.ToString(entry.Key);
                if (string.IsNullOrEmpty(name)) { continue; }
                var value = Convert.ToString(entry.Value);
                // Content-class headers (Content-Type, Content-Length, ...) are
                // rejected by HttpRequestHeaders; route those to the content if
                // the caller ever sends a body. GET has no body so we drop them.
                if (!request.Headers.TryAddWithoutValidation(name, value))
                {
                    // Best effort: header was rejected by the request collection
                    // (likely a Content-* header on a body-less GET). Skip rather
                    // than throw so caller intent is preserved.
                }
            }
        }
    }
}
