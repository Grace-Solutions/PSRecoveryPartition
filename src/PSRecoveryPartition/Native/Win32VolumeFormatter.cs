using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Synchronous wrapper around fmifs!FormatEx. The native API is callback
    /// driven: we block on a ManualResetEvent that the callback signals on
    /// FMIFS_DONE, and surface the final success flag back to the caller.
    /// fmifs only accepts DOS-style drive roots (e.g. "X:\\"); when the caller
    /// supplies a \\?\Volume{guid}\ device path the helper transparently
    /// mounts the volume on a free drive letter for the duration of the call
    /// and removes that mount point afterwards.
    /// </summary>
    internal static class Win32VolumeFormatter
    {
        // FMIFS_DONE notification code; FormatEx invokes the callback with
        // command=11 when the operation completes. argument is a BOOL* whose
        // value indicates success.
        private const int FMIFS_DONE = 11;

        private const int FormatMediaTypeFixed = 12;

        /// <summary>
        /// Formats <paramref name="target"/> with the given file system and
        /// label. <paramref name="target"/> may be either a DOS drive root
        /// (e.g. "X:\\") or a \\?\Volume{guid}\ device path; the latter is
        /// temporarily exposed under a free drive letter so fmifs can attach.
        /// </summary>
        public static void Format(
            string target,
            string fileSystem = "NTFS",
            string label = null,
            bool quickFormat = true,
            int clusterSize = 0,
            TimeSpan? timeout = null)
        {
            if (string.IsNullOrEmpty(target)) { throw new ArgumentNullException("target"); }

            if (IsVolumeGuidPath(target))
            {
                FormatViaTempLetter(target, fileSystem, label, quickFormat, clusterSize, timeout);
                return;
            }

            FormatDosRoot(target, fileSystem, label, quickFormat, clusterSize, timeout);
        }

        private static void FormatDosRoot(
            string driveRoot,
            string fileSystem,
            string label,
            bool quickFormat,
            int clusterSize,
            TimeSpan? timeout)
        {
            var done = new ManualResetEventSlim(false);
            var success = false;

            NativeMethods.FormatExCallback cb = (command, subAction, argument) =>
            {
                if (command == FMIFS_DONE)
                {
                    try
                    {
                        success = argument != IntPtr.Zero
                            && Marshal.ReadByte(argument) != 0;
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

        private static void FormatViaTempLetter(
            string volumeName,
            string fileSystem,
            string label,
            bool quickFormat,
            int clusterSize,
            TimeSpan? timeout)
        {
            var volumeRoot = volumeName.EndsWith("\\", StringComparison.Ordinal)
                ? volumeName : volumeName + "\\";

            // If mountmgr already auto-assigned a drive letter to this volume,
            // reuse it instead of trying to add another. Volumes can technically
            // hold multiple mount points, but SetVolumeMountPointW returns
            // ERROR_INVALID_PARAMETER when the target already has a DOS letter.
            var existingLetter = FindExistingDriveLetter(volumeRoot);
            if (existingLetter != null)
            {
                FormatDosRoot(existingLetter, fileSystem, label, quickFormat, clusterSize, timeout);
                return;
            }

            var candidates = PickFreeDriveLetters();
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "fmifs!FormatEx requires a DOS drive root but no free drive letter is available " +
                    "to temporarily expose " + volumeName + ".");
            }

            string mountPoint = null;
            var lastError = 0;
            foreach (var letter in candidates)
            {
                var candidate = letter + ":\\";
                if (NativeMethods.SetVolumeMountPointW(candidate, volumeRoot))
                {
                    mountPoint = candidate;
                    break;
                }
                lastError = Marshal.GetLastWin32Error();
            }
            if (mountPoint == null)
            {
                throw new Win32Exception(
                    lastError,
                    "SetVolumeMountPointW failed for every candidate letter (last Win32 error " + lastError +
                    ") when trying to temporarily expose " + volumeRoot + " for fmifs!FormatEx.");
            }
            try
            {
                FormatDosRoot(mountPoint, fileSystem, label, quickFormat, clusterSize, timeout);
            }
            finally
            {
                try { NativeMethods.DeleteVolumeMountPointW(mountPoint); } catch { /* best effort */ }
            }
        }

        private static string FindExistingDriveLetter(string volumeRoot)
        {
            var buffer = new char[1024];
            int needed;
            if (!NativeMethods.GetVolumePathNamesForVolumeNameW(volumeRoot, buffer, buffer.Length, out needed))
            {
                var err = Marshal.GetLastWin32Error();
                if (err != NativeConstants.ERROR_MORE_DATA) { return null; }
                buffer = new char[needed];
                if (!NativeMethods.GetVolumePathNamesForVolumeNameW(volumeRoot, buffer, buffer.Length, out needed))
                {
                    return null;
                }
            }
            var paths = new string(buffer, 0, Math.Max(0, needed))
                .Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in paths)
            {
                // Drive-letter mount points are exactly "X:\" (length 3, colon at index 1).
                if (p.Length == 3 && p[1] == ':' && (p[2] == '\\' || p[2] == '/')) { return p; }
            }
            return null;
        }

        private static bool IsVolumeGuidPath(string path)
        {
            return !string.IsNullOrEmpty(path)
                && path.StartsWith(@"\\?\Volume{", StringComparison.OrdinalIgnoreCase);
        }

        private static System.Collections.Generic.List<char> PickFreeDriveLetters()
        {
            // Prefer high letters (Z..D) to avoid colliding with the typical
            // floppy/system letters and to stay out of the way of any user
            // sessions that may be assigning low letters dynamically.
            var result = new System.Collections.Generic.List<char>();
            var inUse = NativeMethods.GetLogicalDrives();
            for (var i = 25; i >= 3; i--)
            {
                if ((inUse & (1u << i)) == 0) { result.Add((char)('A' + i)); }
            }
            return result;
        }
    }
}
