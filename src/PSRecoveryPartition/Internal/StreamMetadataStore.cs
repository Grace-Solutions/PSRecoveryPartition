using System;
using System.IO;
using PSRecoveryPartition.Native;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Stores a small piece of metadata (the download ETag) in an NTFS alternate
    /// data stream on the downloaded file, so a later conditional request can send
    /// <c>If-None-Match</c>. The stream is opened with native <c>CreateFileW</c>
    /// (via <see cref="NativeMethods"/>) because managed file APIs on .NET
    /// Framework reject the <c>file:stream</c> path form. Reads/writes are strictly
    /// best-effort: a non-NTFS destination simply yields no stored value.
    /// </summary>
    internal static class StreamMetadataStore
    {
        private const string EtagStreamSuffix = ":PSRecoveryPartition.etag";

        public static string ReadEtag(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) { return null; }
            try
            {
                using (var handle = NativeMethods.CreateFileW(
                    filePath + EtagStreamSuffix,
                    NativeConstants.GENERIC_READ,
                    NativeConstants.FILE_SHARE_READ,
                    IntPtr.Zero,
                    NativeConstants.OPEN_EXISTING,
                    NativeConstants.FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero))
                {
                    if (handle == null || handle.IsInvalid) { return null; }
                    using (var fs = new FileStream(handle, FileAccess.Read))
                    using (var reader = new StreamReader(fs))
                    {
                        var value = reader.ReadToEnd();
                        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                    }
                }
            }
            catch { return null; }
        }

        public static void WriteEtag(string filePath, string etag)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(etag)) { return; }
            try
            {
                using (var handle = NativeMethods.CreateFileW(
                    filePath + EtagStreamSuffix,
                    NativeConstants.GENERIC_WRITE,
                    0,
                    IntPtr.Zero,
                    NativeConstants.CREATE_ALWAYS,
                    NativeConstants.FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero))
                {
                    if (handle == null || handle.IsInvalid) { return; }
                    using (var fs = new FileStream(handle, FileAccess.Write))
                    using (var writer = new StreamWriter(fs))
                    {
                        writer.Write(etag);
                    }
                }
            }
            catch { /* best effort */ }
        }
    }
}
