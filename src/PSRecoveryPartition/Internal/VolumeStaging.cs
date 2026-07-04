using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using PSRecoveryPartition.Native;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Runs a file-system action against a recovery partition without permanently
    /// mounting it. Prefers the kernel <c>\\?\Volume{guid}\</c> path so no drive
    /// letter is consumed; transiently attaches a directory junction (no drive
    /// letter) and tears it down on exit when NTFS is not yet bound to the volume
    /// (for example, on hosts with automount disabled).
    /// </summary>
    internal static class VolumeStaging
    {
        /// <summary>
        /// Resolves <paramref name="diskNumber"/> / <paramref name="partitionNumber"/>
        /// to the kernel volume name (<c>\\?\Volume{guid}\</c>). Returns null when
        /// the volume manager has not yet registered a volume for the partition.
        /// </summary>
        public static string ResolveVolumePath(int diskNumber, int partitionNumber)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var part = disk.Partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber);
            if (part == null) { return null; }
            var vol = Win32VolumeMapper.FindForPartition(
                Win32VolumeMapper.EnumerateAll(), diskNumber, part.StartingOffset);
            if (vol == null || string.IsNullOrEmpty(vol.VolumeName)) { return null; }
            return EnsureTrailingSlash(vol.VolumeName);
        }

        /// <summary>
        /// Invokes <paramref name="action"/> with a backslash-terminated volume
        /// root that is valid for <em>managed</em> file I/O. Assigns a transient
        /// directory junction (no drive letter) over the <c>\\?\Volume{guid}\</c>
        /// path and runs the action through the junction's temp-folder path,
        /// removing the junction on exit.
        /// </summary>
        /// <remarks>
        /// The raw <c>\\?\Volume{guid}\</c> name is valid for native CreateFile
        /// but managed file APIs on .NET Framework reject it: the '?' trips the
        /// legacy path validator ("Illegal characters in path"). The junction's
        /// root is an ordinary temp directory path, which every managed API
        /// accepts. SetVolumeMountPointW adds the junction as an extra mount
        /// point regardless of whether a file system is already attached, so a
        /// single code path covers both automounted and automount-disabled hosts.
        /// </remarks>
        public static void WithVolumeRoot(int diskNumber, int partitionNumber, Action<string> action)
        {
            if (action == null) { throw new ArgumentNullException("action"); }
            var volumePath = ResolveVolumePath(diskNumber, partitionNumber);
            if (volumePath == null)
            {
                throw new InvalidOperationException(
                    "No Windows volume is currently associated with disk " + diskNumber +
                    " partition " + partitionNumber + ".");
            }

            WithTransientJunction(volumePath, action);
        }

        private static bool HasAttachedFileSystem(string volumeRoot)
        {
            var fs = new System.Text.StringBuilder(261);
            var label = new System.Text.StringBuilder(261);
            uint serial, maxComp, flags;
            return NativeMethods.GetVolumeInformationW(
                volumeRoot, label, label.Capacity,
                out serial, out maxComp, out flags,
                fs, fs.Capacity) && fs.Length > 0;
        }

        private static void WithTransientJunction(string volumeRoot, Action<string> action)
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "PSRecoveryPartition-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            var mounted = false;
            try
            {
                if (!NativeMethods.SetVolumeMountPointW(EnsureTrailingSlash(tempRoot), volumeRoot))
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        "SetVolumeMountPointW failed for transient junction " + tempRoot + ".");
                }
                mounted = true;

                // Wait briefly for NTFS to attach so subsequent I/O sees the FS.
                WaitForFileSystem(EnsureTrailingSlash(tempRoot), TimeSpan.FromSeconds(10));

                action(EnsureTrailingSlash(tempRoot));
            }
            finally
            {
                if (mounted)
                {
                    try { NativeMethods.DeleteVolumeMountPointW(EnsureTrailingSlash(tempRoot)); } catch { /* best effort */ }
                }
                try { Directory.Delete(tempRoot, true); } catch { /* best effort */ }
            }
        }

        private static void WaitForFileSystem(string root, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (HasAttachedFileSystem(root)) { return; }
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Invokes <paramref name="action"/> with a transiently-assigned DOS
        /// drive letter (e.g. <c>"X:"</c>) for the partition. Required when a
        /// downstream tool insists on a drive-letter style root (the canonical
        /// case is <c>bcdedit /set ... ramdisk=[X:]\path</c> -- the bracketed
        /// letter is converted to a persistent NT volume identifier at parse
        /// time, so the letter only needs to exist while bcdedit runs). The
        /// letter is removed before this method returns.
        /// </summary>
        public static void WithDriveLetter(int diskNumber, int partitionNumber, Action<string> action)
        {
            if (action == null) { throw new ArgumentNullException("action"); }
            var volumePath = ResolveVolumePath(diskNumber, partitionNumber);
            if (volumePath == null)
            {
                throw new InvalidOperationException(
                    "No Windows volume is currently associated with disk " + diskNumber +
                    " partition " + partitionNumber + ".");
            }

            var letter = PickFreeDriveLetter();
            if (letter == null)
            {
                throw new InvalidOperationException(
                    "No free DOS drive letter is available for transient mount of " + volumePath + ".");
            }
            var mountPoint = letter + "\\";
            var mounted = false;
            try
            {
                if (!NativeMethods.SetVolumeMountPointW(mountPoint, volumePath))
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        "SetVolumeMountPointW failed for transient drive letter " + mountPoint + ".");
                }
                mounted = true;
                WaitForFileSystem(mountPoint, TimeSpan.FromSeconds(10));
                action(letter);
            }
            finally
            {
                if (mounted)
                {
                    try { NativeMethods.DeleteVolumeMountPointW(mountPoint); } catch { /* best effort */ }
                }
            }
        }

        private static string PickFreeDriveLetter()
        {
            // Bit 0 = A:, bit 25 = Z:. Skip A/B (floppy reserved) and walk down
            // from Z so we collide as little as possible with user assignments.
            uint mask = NativeMethods.GetLogicalDrives();
            for (int i = 25; i >= 2; i--)
            {
                if ((mask & (1u << i)) == 0)
                {
                    return ((char)('A' + i)) + ":";
                }
            }
            return null;
        }

        /// <summary>
        /// Applies <c>Hidden</c> and/or <c>System</c> attributes to a staged
        /// file. Best effort: failures are surfaced as <see cref="Win32Exception"/>
        /// so callers can decide whether to propagate.
        /// </summary>
        public static void ApplyAttributes(string path, bool hidden, bool system, bool readOnly = false)
        {
            if (string.IsNullOrEmpty(path)) { return; }
            uint current = NativeMethods.GetFileAttributesW(path);
            if (current == NativeConstants.INVALID_FILE_ATTRIBUTES)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "GetFileAttributesW failed for " + path + ".");
            }
            if (hidden)   { current |= NativeConstants.FILE_ATTRIBUTE_HIDDEN; }
            if (system)   { current |= NativeConstants.FILE_ATTRIBUTE_SYSTEM; }
            if (readOnly) { current |= NativeConstants.FILE_ATTRIBUTE_READONLY; }
            if (!NativeMethods.SetFileAttributesW(path, current))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "SetFileAttributesW failed for " + path + ".");
            }
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path)) { return path; }
            return path.EndsWith("\\", StringComparison.Ordinal) ? path : path + "\\";
        }
    }
}
