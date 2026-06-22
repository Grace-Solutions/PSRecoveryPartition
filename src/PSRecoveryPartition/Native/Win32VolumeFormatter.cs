using System;
using System.Threading;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Synchronous wrapper around fmifs!FormatEx. The native API is callback
    /// driven: we block on a ManualResetEvent that the callback signals on
    /// FMIFS_DONE, and surface the final success flag back to the caller.
    /// </summary>
    internal static class Win32VolumeFormatter
    {
        // FMIFS_DONE notification code; FormatEx invokes the callback with
        // command=11 when the operation completes. argument is a BOOL* whose
        // value indicates success.
        private const int FMIFS_DONE = 11;

        private const int FormatMediaTypeFixed = 12;

        /// <summary>
        /// Formats <paramref name="driveRoot"/> with the given file system and
        /// label. <paramref name="driveRoot"/> must include the trailing
        /// backslash (e.g. "X:\\" or "\\?\Volume{guid}\\").
        /// </summary>
        public static void Format(
            string driveRoot,
            string fileSystem = "NTFS",
            string label = null,
            bool quickFormat = true,
            int clusterSize = 0,
            TimeSpan? timeout = null)
        {
            if (string.IsNullOrEmpty(driveRoot)) { throw new ArgumentNullException("driveRoot"); }

            var done = new ManualResetEventSlim(false);
            var success = false;

            NativeMethods.FormatExCallback cb = (command, subAction, argument) =>
            {
                if (command == FMIFS_DONE)
                {
                    try
                    {
                        success = argument != IntPtr.Zero
                            && System.Runtime.InteropServices.Marshal.ReadByte(argument) != 0;
                    }
                    finally { done.Set(); }
                }
                return true;
            };

            NativeMethods.FormatEx(
                driveRoot,
                FormatMediaTypeFixed,
                string.IsNullOrEmpty(fileSystem) ? "NTFS" : fileSystem,
                label ?? string.Empty,
                quickFormat,
                clusterSize,
                cb);

            var waitFor = timeout ?? TimeSpan.FromMinutes(10);
            if (!done.Wait(waitFor))
            {
                throw new TimeoutException(
                    "fmifs!FormatEx did not complete within " + waitFor + " for " + driveRoot + ".");
            }
            if (!success)
            {
                throw new InvalidOperationException(
                    "fmifs!FormatEx reported failure for " + driveRoot + ".");
            }
            GC.KeepAlive(cb);
        }
    }
}
