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
        private static void ExtendFileSystem(int diskNumber, long partitionStartingOffset, long newTotalSectors)
        {
            var volume = ResolveVolumeName(diskNumber, partitionStartingOffset);
            if (volume == null) { return; }
            using (SafeFileHandle handle = DeviceHandleFactory.OpenVolume(volume, readOnly: false))
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
                            "FSCTL_EXTEND_VOLUME failed for " + volume + ".");
                    }
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
        }

        private static void ShrinkFileSystem(int diskNumber, long partitionStartingOffset, long newTotalSectors)
        {
            var volume = ResolveVolumeName(diskNumber, partitionStartingOffset);
            if (volume == null)
            {
                throw new InvalidOperationException(
                    "No Windows volume is currently associated with the partition at offset " +
                    partitionStartingOffset + " on disk " + diskNumber + "; cannot shrink the file system.");
            }
            using (SafeFileHandle handle = DeviceHandleFactory.OpenVolume(volume, readOnly: false))
            {
                SendShrink(handle, volume, SHRINK_VOLUME_REQUEST_TYPES.ShrinkPrepare, newTotalSectors);
                try { SendShrink(handle, volume, SHRINK_VOLUME_REQUEST_TYPES.ShrinkCommit, 0L); }
                catch
                {
                    try { SendShrink(handle, volume, SHRINK_VOLUME_REQUEST_TYPES.ShrinkAbort, 0L); } catch { /* best effort */ }
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

        private static string ResolveVolumeName(int diskNumber, long partitionStartingOffset)
        {
            var vol = Win32VolumeMapper.FindForPartition(
                Win32VolumeMapper.EnumerateAll(), diskNumber, partitionStartingOffset);
            return vol == null ? null : vol.VolumeName;
        }
    }
}
