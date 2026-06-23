using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
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
        private static void ExtendFileSystem(
            int diskNumber, int partitionNumber, long partitionStartingOffset, long newTotalSectors)
        {
            string display;
            using (SafeFileHandle handle = OpenFileSystemHandle(
                diskNumber, partitionNumber, partitionStartingOffset, readOnly: false, displayName: out display))
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
                    if (!ok)
                    {
                        throw new Win32Exception(
                            Marshal.GetLastWin32Error(),
                            "FSCTL_EXTEND_VOLUME failed for " + display + ".");
                    }
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
        }

        private static void ShrinkFileSystem(
            int diskNumber, int partitionNumber, long partitionStartingOffset, long newTotalSectors)
        {
            string display;
            using (SafeFileHandle handle = OpenFileSystemHandle(
                diskNumber, partitionNumber, partitionStartingOffset, readOnly: false, displayName: out display))
            {
                SendShrink(handle, display, SHRINK_VOLUME_REQUEST_TYPES.ShrinkPrepare, newTotalSectors);
                try { SendShrink(handle, display, SHRINK_VOLUME_REQUEST_TYPES.ShrinkCommit, 0L); }
                catch
                {
                    try { SendShrink(handle, display, SHRINK_VOLUME_REQUEST_TYPES.ShrinkAbort, 0L); } catch { /* best effort */ }
                    throw;
                }
            }
        }

        private static void SendShrink(
            SafeFileHandle handle, string volume,
            SHRINK_VOLUME_REQUEST_TYPES kind, long newSectors)
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
                if (!ok)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        "FSCTL_SHRINK_VOLUME(" + kind + ") failed for " + volume + ".");
                }
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        /// <summary>
        /// Opens a handle suitable for volume-scoped FSCTLs against the given
        /// partition. Prefers the mountmgr-surfaced <c>\\?\Volume{guid}</c>
        /// path when available, then falls back to the kernel partition device
        /// <c>\\.\Harddisk{N}Partition{M}</c>. Hidden recovery partitions are
        /// never surfaced as volume GUIDs, but the partition device always
        /// exists once the layout write has been committed.
        /// </summary>
        private static SafeFileHandle OpenFileSystemHandle(
            int diskNumber, int partitionNumber, long partitionStartingOffset,
            bool readOnly, out string displayName)
        {
            var volume = ResolveVolumeName(diskNumber, partitionStartingOffset);
            if (volume != null)
            {
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
    }
}
