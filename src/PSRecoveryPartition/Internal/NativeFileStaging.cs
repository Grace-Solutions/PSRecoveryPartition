using System;
using System.ComponentModel;
using System.IO;
using PSRecoveryPartition.Native;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Stages files onto a target volume addressed by a Win32 device path
    /// (<c>\\?\GLOBALROOT\Device\HarddiskVolumeN\</c> or
    /// <c>\\?\Volume{guid}\</c>) using native CreateDirectory / CopyFileEx. This
    /// is the "copy without mounting" path: the target partition never receives a
    /// drive letter or junction, so a hidden recovery partition can be written
    /// directly. Managed <see cref="Path"/>/<see cref="File"/> APIs cannot be used
    /// here because .NET Framework rejects the '?' and GlobalRoot path forms.
    /// </summary>
    internal static class NativeFileStaging
    {
        private const int ERROR_ALREADY_EXISTS = 183;
        private const int ERROR_PATH_NOT_FOUND = 3;
        private const uint PROGRESS_CONTINUE = 0;

        /// <summary>
        /// Joins a backslash-terminated (or not) Win32 device root with a relative
        /// path using backslashes, without invoking managed path validation.
        /// </summary>
        public static string Combine(string root, string relative)
        {
            if (string.IsNullOrEmpty(root)) { throw new ArgumentException("root is required.", "root"); }
            var left = root.TrimEnd('\\', '/');
            var right = (relative ?? string.Empty).Replace('/', '\\').Trim('\\');
            return right.Length == 0 ? left + "\\" : left + "\\" + right;
        }

        /// <summary>
        /// Creates <paramref name="win32Dir"/> and every missing parent under the
        /// device root. The root itself (the volume) is assumed to exist.
        /// </summary>
        public static void EnsureDirectory(string win32Dir)
        {
            if (string.IsNullOrEmpty(win32Dir)) { return; }
            var normalized = win32Dir.TrimEnd('\\');

            // Find the device-root prefix so we never try to "create" the volume
            // itself. For \\?\GLOBALROOT\Device\HarddiskVolumeN\ and
            // \\?\Volume{guid}\ the first real subdirectory is the create target.
            var rootLen = DeviceRootLength(normalized);
            if (rootLen <= 0 || rootLen >= normalized.Length) { return; }

            // Walk each segment after the root, creating as we go.
            var idx = rootLen;
            while (idx <= normalized.Length)
            {
                var next = normalized.IndexOf('\\', idx);
                if (next < 0) { next = normalized.Length; }
                var partial = normalized.Substring(0, next);
                CreateOne(partial);
                if (next == normalized.Length) { break; }
                idx = next + 1;
            }
        }

        private static void CreateOne(string dir)
        {
            if (NativeMethods.CreateDirectoryW(dir, IntPtr.Zero)) { return; }
            var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            if (err == ERROR_ALREADY_EXISTS) { return; }
            // The last path component of a device root (e.g. the "\\?\GLOBALROOT\
            // Device\HarddiskVolumeN" prefix) can report PATH_NOT_FOUND when
            // treated as a create target; the segment walk above avoids that, so a
            // real PATH_NOT_FOUND here means a genuinely bad target.
            throw new Win32Exception(err, "CreateDirectory failed for '" + dir + "' (Win32 " + err + ").");
        }

        /// <summary>
        /// Copies <paramref name="sourceFile"/> to <paramref name="destWin32File"/>,
        /// reporting 0-100 percent through <paramref name="onProgress"/> when
        /// supplied. Overwrites any existing destination.
        /// </summary>
        public static void CopyFile(string sourceFile, string destWin32File, Action<int> onProgress)
        {
            if (string.IsNullOrEmpty(sourceFile)) { throw new ArgumentException("sourceFile is required.", "sourceFile"); }
            if (string.IsNullOrEmpty(destWin32File)) { throw new ArgumentException("destWin32File is required.", "destWin32File"); }

            var cancel = false;
            NativeMethods.CopyProgressRoutine routine = null;
            if (onProgress != null)
            {
                var last = -1;
                routine = (total, transferred, sSize, sTransferred, streamNo, reason, hSrc, hDst, data) =>
                {
                    if (total > 0)
                    {
                        var pct = (int)(transferred * 100 / total);
                        if (pct != last) { last = pct; try { onProgress(pct); } catch { /* ignore reporter errors */ } }
                    }
                    return PROGRESS_CONTINUE;
                };
            }

            if (!NativeMethods.CopyFileExW(sourceFile, destWin32File, routine, IntPtr.Zero, ref cancel, 0))
            {
                var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                throw new Win32Exception(err, "CopyFileEx failed ('" + sourceFile + "' -> '" + destWin32File + "', Win32 " + err + ").");
            }
        }

        /// <summary>
        /// Length of the device-root prefix that must already exist (the volume),
        /// so directory creation starts at the first real subdirectory. Handles
        /// the GlobalRoot device form and the \\?\Volume{guid}\ form.
        /// </summary>
        private static int DeviceRootLength(string path)
        {
            // \\?\GLOBALROOT\Device\HarddiskVolumeN\...  -> root ends after HarddiskVolumeN
            const string gr = @"\\?\GLOBALROOT\";
            if (path.StartsWith(gr, StringComparison.OrdinalIgnoreCase))
            {
                // Consume "Device\HarddiskVolumeN" (three backslash-separated tokens
                // after the prefix: Device, HarddiskVolumeN).
                var after = gr.Length;
                var tokensSeen = 0;
                for (var i = after; i < path.Length; i++)
                {
                    if (path[i] == '\\')
                    {
                        tokensSeen++;
                        if (tokensSeen == 2) { return i + 1; }
                    }
                }
                return path.Length; // whole thing is the root
            }

            // \\?\Volume{guid}\...
            const string vol = @"\\?\";
            if (path.StartsWith(vol, StringComparison.OrdinalIgnoreCase))
            {
                var firstSep = path.IndexOf('\\', vol.Length);
                return firstSep < 0 ? path.Length : firstSep + 1;
            }

            // An ordinary rooted path (mounted target). Start after the drive/UNC root.
            try { return Path.GetPathRoot(path)?.Length ?? 0; }
            catch (ArgumentException) { return 0; }
        }
    }
}
