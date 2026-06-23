using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// FSCTL helpers used by the resize path: FSCTL_EXTEND_VOLUME for grow
    /// and the prepare / commit / abort sequence of FSCTL_SHRINK_VOLUME for
    /// shrink. Split out from the resize orchestration so that file system
    /// concerns stay separate from partition layout concerns.
    /// </summary>
    internal static partial class Win32PartitionWriter
    {
        // After IOCTL_DISK_GROW_PARTITION or a recent un-hide of a recovery
        // partition the volume manager briefly cycles the volume device,
        // during which FSCTLs come back with ERROR_INVALID_FUNCTION or
        // ERROR_NOT_READY because no file system is attached for an instant.
        // Re-resolve the volume and retry rather than failing immediately.
        private const int FsCtlMaxAttempts = 20;
        private const int FsCtlRetryDelayMs = 250;

        private static void ExtendFileSystem(
            int diskNumber, int partitionNumber, long partitionStartingOffset, long newTotalSectors)
        {
            RunFsCtlWithRetry(
                diskNumber, partitionNumber, partitionStartingOffset,
                "FSCTL_EXTEND_VOLUME",
                (handle, display) =>
                {
                    var size = sizeof(long);
                    var buffer = Marshal.AllocHGlobal(size);
                    try
                    {
                        Marshal.WriteInt64(buffer, newTotalSectors);
                        int returned;
                        var ok = NativeMethods.DeviceIoControl(
                            handle, NativeConstants.FSCTL_EXTEND_VOLUME,
                            buffer, size, IntPtr.Zero, 0, out returned, IntPtr.Zero);
                        return ok ? 0 : Marshal.GetLastWin32Error();
                    }
                    finally { Marshal.FreeHGlobal(buffer); }
                });
        }

        private static void ShrinkFileSystem(
            int diskNumber, int partitionNumber, long partitionStartingOffset, long newTotalSectors)
        {
            RunFsCtlWithRetry(
                diskNumber, partitionNumber, partitionStartingOffset,
                "FSCTL_SHRINK_VOLUME",
                (handle, display) =>
                {
                    int err = SendShrinkRaw(handle, SHRINK_VOLUME_REQUEST_TYPES.ShrinkPrepare, newTotalSectors);
                    if (err != 0) { return err; }
                    err = SendShrinkRaw(handle, SHRINK_VOLUME_REQUEST_TYPES.ShrinkCommit, 0L);
                    if (err != 0)
                    {
                        SendShrinkRaw(handle, SHRINK_VOLUME_REQUEST_TYPES.ShrinkAbort, 0L);
                    }
                    return err;
                });
        }

        /// <summary>
        /// Opens the volume, invokes <paramref name="action"/>, and retries on
        /// transient errors (ERROR_INVALID_FUNCTION / ERROR_NOT_READY /
        /// ERROR_INVALID_HANDLE) by re-resolving the volume and reopening.
        /// Throws a Win32Exception with the last error after the retry budget
        /// is exhausted, or immediately on a non-transient error.
        /// </summary>
        private static void RunFsCtlWithRetry(
            int diskNumber, int partitionNumber, long partitionStartingOffset,
            string label, Func<SafeFileHandle, string, int> action)
        {
            int lastError = 0;
            string lastDisplay = null;
            for (int attempt = 0; attempt < FsCtlMaxAttempts; attempt++)
            {
                SafeFileHandle handle = null;
                string display;
                string tempLetter = null;
                try
                {
                    handle = OpenFileSystemHandle(
                        diskNumber, partitionNumber, partitionStartingOffset,
                        readOnly: false, displayName: out display,
                        tempDriveLetterToRelease: out tempLetter);
                    lastDisplay = display;
                }
                catch (Win32Exception ex)
                {
                    lastError = ex.NativeErrorCode;
                    if (!IsTransientFsCtlError(lastError)) { throw; }
                    Thread.Sleep(FsCtlRetryDelayMs);
                    continue;
                }

                try
                {
                    int err = action(handle, display);
                    if (err == 0) { return; }
                    lastError = err;
                    if (!IsTransientFsCtlError(err))
                    {
                        throw new Win32Exception(err, label + " failed for " + display + " (Win32 error " + err + ").");
                    }
                }
                finally
                {
                    if (handle != null) { handle.Dispose(); }
                    ReleaseTempDriveLetter(tempLetter);
                }

                Thread.Sleep(FsCtlRetryDelayMs);
            }
            throw new Win32Exception(
                lastError,
                label + " failed for " + (lastDisplay ?? "<unresolved>") +
                " after " + FsCtlMaxAttempts + " attempts (Win32 error " + lastError + ").");
        }

        private static bool IsTransientFsCtlError(int err)
        {
            return err == 1   // ERROR_INVALID_FUNCTION  - FS not yet attached
                || err == 6   // ERROR_INVALID_HANDLE    - volume just cycled
                || err == 21  // ERROR_NOT_READY         - volume not online yet
                || err == 32; // ERROR_SHARING_VIOLATION - mountmgr holds it
        }

        private static int SendShrinkRaw(
            SafeFileHandle handle, SHRINK_VOLUME_REQUEST_TYPES kind, long newSectors)
        {
            var request = new SHRINK_VOLUME_INFORMATION
            {
                ShrinkRequestType  = kind,
                Flags              = 0,
                NewNumberOfSectors = newSectors,
            };
            var size = Marshal.SizeOf<SHRINK_VOLUME_INFORMATION>();
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(request, buffer, false);
                int returned;
                var ok = NativeMethods.DeviceIoControl(
                    handle, NativeConstants.FSCTL_SHRINK_VOLUME,
                    buffer, size, IntPtr.Zero, 0, out returned, IntPtr.Zero);
                return ok ? 0 : Marshal.GetLastWin32Error();
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        /// <summary>
        /// Opens a handle suitable for volume-scoped FSCTLs against the given
        /// partition. When the host has automount disabled (mountvol /N) the
        /// volume manager registers the volume GUID but never attaches NTFS,
        /// so opening the GUID device yields a handle that responds with
        /// ERROR_INVALID_FUNCTION to file-system FSCTLs. To work around that,
        /// the helper transparently assigns a temporary drive letter when no
        /// mount point exists, opens <c>\\.\X:</c> (which forces NTFS to
        /// attach), and records the temporary letter for cleanup after the
        /// FSCTL completes. Hidden recovery partitions hit this path on
        /// every grow/shrink and rely on it.
        /// </summary>
        private static SafeFileHandle OpenFileSystemHandle(
            int diskNumber, int partitionNumber, long partitionStartingOffset,
            bool readOnly, out string displayName,
            out string tempDriveLetterToRelease)
        {
            tempDriveLetterToRelease = null;
            var volume = ResolveVolumeName(diskNumber, partitionStartingOffset);
            if (volume != null)
            {
                var volumeRoot = volume.EndsWith("\\", StringComparison.Ordinal) ? volume : volume + "\\";
                var existing = FindExistingDriveLetterRoot(volumeRoot);
                if (existing != null)
                {
                    displayName = existing.TrimEnd('\\');
                    return DeviceHandleFactory.OpenVolume(displayName, readOnly);
                }
                var assigned = TryAssignTempDriveLetter(volumeRoot);
                if (assigned != null)
                {
                    tempDriveLetterToRelease = assigned;
                    displayName = assigned.TrimEnd('\\');
                    return DeviceHandleFactory.OpenVolume(displayName, readOnly);
                }
                // Fall back to opening the volume GUID directly: when
                // automount is enabled NTFS is already attached and FSCTLs
                // succeed via this path.
                displayName = volume;
                return DeviceHandleFactory.OpenVolume(volume, readOnly);
            }
            displayName = @"\\.\Harddisk" + diskNumber + "Partition" + partitionNumber;
            return DeviceHandleFactory.OpenHarddiskPartition(diskNumber, partitionNumber, readOnly);
        }

        private static string ResolveVolumeName(int diskNumber, long partitionStartingOffset)
        {
            var vol = Win32VolumeMapper.FindForPartition(
                Win32VolumeMapper.EnumerateAll(), diskNumber, partitionStartingOffset);
            return vol == null ? null : vol.VolumeName;
        }

        private static string FindExistingDriveLetterRoot(string volumeRoot)
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
                if (p.Length == 3 && p[1] == ':' && (p[2] == '\\' || p[2] == '/'))
                {
                    return @"\\.\" + p.Substring(0, 2) + "\\";
                }
            }
            return null;
        }

        private static string TryAssignTempDriveLetter(string volumeRoot)
        {
            var inUse = NativeMethods.GetLogicalDrives();
            for (var i = 25; i >= 3; i--)
            {
                if ((inUse & (1u << i)) != 0) { continue; }
                var letter = (char)('A' + i);
                var mountPoint = letter + ":\\";
                if (!NativeMethods.SetVolumeMountPointW(mountPoint, volumeRoot)) { continue; }
                // SetVolumeMountPointW only registers the symlink with the
                // mount manager. With automount disabled the file system is
                // not attached until somebody actually touches the root, so
                // wait until GetVolumeInformationW succeeds (which forces the
                // NTFS recogniser to walk the volume parameter list) before
                // returning. Without this probe the subsequent open of
                // \\.\X: returns a raw volume handle and the FSCTL fails with
                // ERROR_INVALID_FUNCTION.
                if (!WaitForFileSystemAttach(mountPoint))
                {
                    try { NativeMethods.DeleteVolumeMountPointW(mountPoint); } catch { /* best effort */ }
                    continue;
                }
                return @"\\.\" + letter + ":\\";
            }
            return null;
        }

        private static bool WaitForFileSystemAttach(string driveRoot)
        {
            var nameBuf = new System.Text.StringBuilder(261);
            var fsBuf   = new System.Text.StringBuilder(261);
            for (var attempt = 0; attempt < FsCtlMaxAttempts; attempt++)
            {
                uint serial, maxComp, flags;
                if (NativeMethods.GetVolumeInformationW(
                        driveRoot, nameBuf, nameBuf.Capacity,
                        out serial, out maxComp, out flags,
                        fsBuf, fsBuf.Capacity)
                    && fsBuf.Length > 0)
                {
                    return true;
                }
                Thread.Sleep(FsCtlRetryDelayMs);
            }
            return false;
        }

        private static void ReleaseTempDriveLetter(string tempPath)
        {
            if (string.IsNullOrEmpty(tempPath)) { return; }
            // tempPath looks like "\\.\X:\". Recover "X:\" for DeleteVolumeMountPointW.
            var idx = tempPath.LastIndexOf('\\', tempPath.Length - 2);
            if (idx < 0) { return; }
            var mountPoint = tempPath.Substring(idx + 1);
            if (!mountPoint.EndsWith("\\", StringComparison.Ordinal)) { mountPoint += "\\"; }
            try { NativeMethods.DeleteVolumeMountPointW(mountPoint); } catch { /* best effort */ }
        }
    }
}
