using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Opens kernel device paths (physical disks and volumes) and returns the
    /// resulting <see cref="SafeFileHandle"/>. Centralising CreateFile here
    /// keeps the path conventions (\\.\PhysicalDriveN and \\?\Volume{guid})
    /// in one place and ensures Win32 errors surface as Win32Exceptions with
    /// the original error code preserved.
    /// </summary>
    internal static class DeviceHandleFactory
    {
        /// <summary>
        /// Opens \\.\PhysicalDriveN with the supplied access flags. Pass
        /// <c>readOnly: true</c> for read paths (drive layout / geometry /
        /// length) and <c>false</c> for write paths (set layout / grow).
        /// </summary>
        public static SafeFileHandle OpenPhysicalDisk(int diskNumber, bool readOnly)
        {
            if (diskNumber < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "diskNumber", diskNumber, "Physical disk number must be non-negative.");
            }
            var path = string.Format(
                CultureInfo.InvariantCulture, @"\\.\PhysicalDrive{0}", diskNumber);
            return Open(path, readOnly);
        }

        /// <summary>
        /// Opens a volume by its kernel name (e.g. <c>\\?\Volume{guid}</c>).
        /// The trailing backslash is stripped because CreateFile rejects it
        /// for volume-handle opens that will receive IOCTLs.
        /// </summary>
        public static SafeFileHandle OpenVolume(string volumeName, bool readOnly)
        {
            if (string.IsNullOrEmpty(volumeName))
            {
                throw new ArgumentException("Volume name is required.", "volumeName");
            }
            var trimmed = volumeName.EndsWith("\\", StringComparison.Ordinal)
                ? volumeName.Substring(0, volumeName.Length - 1)
                : volumeName;
            return Open(trimmed, readOnly);
        }

        /// <summary>
        /// Opens the partition device at <c>\\.\Harddisk{disk}Partition{part}</c>
        /// directly. This bypasses the Volume Manager / mountmgr layer and is
        /// the only reliable way to address hidden recovery partitions whose
        /// GPT type prevents a <c>\\?\Volume{guid}</c> name from ever being
        /// surfaced by FindFirstVolumeW. The kernel partition device still has
        /// the file system mounted on it, so volume-scoped FSCTLs continue to
        /// work against this handle.
        /// </summary>
        public static SafeFileHandle OpenHarddiskPartition(int diskNumber, int partitionNumber, bool readOnly)
        {
            if (diskNumber < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "diskNumber", diskNumber, "Physical disk number must be non-negative.");
            }
            if (partitionNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "partitionNumber", partitionNumber, "Partition number must be positive.");
            }
            var path = string.Format(
                CultureInfo.InvariantCulture, @"\\.\Harddisk{0}Partition{1}", diskNumber, partitionNumber);
            return Open(path, readOnly);
        }

        private static SafeFileHandle Open(string path, bool readOnly)
        {
            var access = readOnly
                ? NativeConstants.GENERIC_READ
                : (NativeConstants.GENERIC_READ | NativeConstants.GENERIC_WRITE);

            var handle = NativeMethods.CreateFileW(
                path,
                access,
                NativeConstants.FILE_SHARE_READ | NativeConstants.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeConstants.OPEN_EXISTING,
                NativeConstants.FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);

            if (handle == null || handle.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();
                if (handle != null) { handle.Dispose(); }
                throw new Win32Exception(
                    error,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "CreateFile failed for '{0}' with Win32 error {1}.",
                        path,
                        error));
            }
            return handle;
        }
    }
}
