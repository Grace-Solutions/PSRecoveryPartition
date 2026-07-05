using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Outcome of a conditional download. When <see cref="Downloaded"/> is false
    /// the remote resource was unchanged (HTTP 304) and the existing local file
    /// was left untouched. <see cref="LastModified"/> / <see cref="ETag"/> carry
    /// the validators the server reported so callers can stamp the file for the
    /// next conditional request.
    /// </summary>
    internal sealed class DownloadOutcome
    {
        public bool Downloaded;
        public long BytesWritten;
        public DateTimeOffset? LastModified;
        public string ETag;
    }

    /// <summary>
    /// Shared HTTP/HTTPS download surface for recovery image cmdlets. Streams the
    /// response to disk with progress reporting and supports a caller-supplied
    /// header dictionary so HTTP authentication scenarios (bearer tokens, basic
    /// auth, custom signing headers) are covered without a bespoke download path
    /// per cmdlet. Supports a conditional <c>If-Modified-Since</c> request so an
    /// unchanged remote image is not re-downloaded.
    /// </summary>
    internal static class HttpImageDownloader
    {
        public static async Task<DownloadOutcome> DownloadAsync(
            Uri uri,
            string destPath,
            IDictionary headers,
            PSCmdlet owner,
            string activity,
            DateTimeOffset? notModifiedSince = null,
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
                    if (notModifiedSince.HasValue)
                    {
                        // Server-side freshness check: a 304 means the local copy is
                        // current, so nothing is re-downloaded.
                        request.Headers.IfModifiedSince = notModifiedSince.Value;
                    }

                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                    {
                        var etag = response.Headers.ETag != null ? response.Headers.ETag.Tag : null;
                        var lastModified = response.Content != null ? response.Content.Headers.LastModified : null;

                        if (response.StatusCode == HttpStatusCode.NotModified)
                        {
                            return new DownloadOutcome { Downloaded = false, LastModified = lastModified, ETag = etag };
                        }

                        response.EnsureSuccessStatusCode();
                        var total = response.Content.Headers.ContentLength ?? -1L;
                        var progress = new ProgressRecord(1, activity ?? "Download", "Downloading " + uri);
                        long read = 0;
                        using (var src = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using (var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                        {
                            var buffer = new byte[81920];
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
                        return new DownloadOutcome { Downloaded = true, BytesWritten = read, LastModified = lastModified, ETag = etag };
                    }
                }
            }
        }

        /// <summary>
        /// Synchronous wrapper used by the image cmdlets: performs a conditional
        /// download (unless <paramref name="force"/>), stamps the file with the
        /// server's Last-Modified so the next run can skip an unchanged image, and
        /// reports the decision through <paramref name="writeVerbose"/>.
        /// </summary>
        public static void RunConditional(
            Uri uri,
            IDictionary headers,
            FileInfo destFile,
            bool force,
            string activity,
            PSCmdlet owner,
            Action<string> writeVerbose)
        {
            DateTimeOffset? since = (!force && destFile.Exists)
                ? destFile.LastWriteTimeUtc
                : (DateTimeOffset?)null;
            if (writeVerbose != null)
            {
                writeVerbose(since.HasValue
                    ? "Conditional download (If-Modified-Since " + since.Value.ToString("u") + "); use -Force to download unconditionally."
                    : (force ? "Forced download (skipping freshness check)." : "Downloading (no existing local copy)."));
            }

            var outcome = DownloadAsync(uri, destFile.FullName, headers, owner, activity, since).GetAwaiter().GetResult();

            if (!outcome.Downloaded)
            {
                if (writeVerbose != null)
                {
                    writeVerbose("Remote image unchanged (HTTP 304); kept existing '" + destFile.FullName + "'.");
                }
            }
            else
            {
                if (outcome.LastModified.HasValue)
                {
                    File.SetLastWriteTimeUtc(destFile.FullName, outcome.LastModified.Value.UtcDateTime);
                }
                if (writeVerbose != null)
                {
                    writeVerbose("Downloaded " + outcome.BytesWritten + " bytes"
                        + (string.IsNullOrEmpty(outcome.ETag) ? string.Empty : " (ETag " + outcome.ETag + ")") + ".");
                }
            }
            destFile.Refresh();
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
