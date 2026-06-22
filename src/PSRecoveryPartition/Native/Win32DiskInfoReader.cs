using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Reads disk geometry and partition layout via direct IOCTLs on
    /// \\.\PhysicalDriveN handles. No PowerShell Storage module dependency.
    /// </summary>
    internal static class Win32DiskInfoReader
    {
        // Layout header is 48 bytes; each PARTITION_INFORMATION_EX entry is
        // 144 bytes. Start at 16 KiB (enough for ~110 partitions, well above
        // the GPT cap of 128). We retry with a doubled buffer on the rare
        // ERROR_INSUFFICIENT_BUFFER response.
        private const int InitialLayoutBufferSize = 16 * 1024;
        private const int PartitionEntrySize = 144;
        private const int LayoutHeaderSize = 48;

        public static Win32DiskInfo Read(int diskNumber)
        {
            using (var handle = DeviceHandleFactory.OpenPhysicalDisk(diskNumber, readOnly: true))
            {
                var info = new Win32DiskInfo { DiskNumber = diskNumber };
                info.SizeBytes = ReadLength(handle);
                ReadLayout(handle, diskNumber, info);
                return info;
            }
        }

        /// <summary>
        /// Probes \\.\PhysicalDriveN for N=0..MaxScan, returning every disk
        /// that opens successfully. Disks that fail with FILE_NOT_FOUND end
        /// the scan; ACCESS_DENIED and other errors are skipped (the caller
        /// may not be elevated, or the disk may be in an inconsistent state).
        /// </summary>
        public static IList<Win32DiskInfo> EnumerateAll(int maxScan = 63)
        {
            var disks = new List<Win32DiskInfo>();
            int consecutiveMisses = 0;
            for (int n = 0; n <= maxScan && consecutiveMisses < 4; n++)
            {
                try
                {
                    disks.Add(Read(n));
                    consecutiveMisses = 0;
                }
                catch (Win32Exception ex)
                {
                    // 2 = FILE_NOT_FOUND, 3 = PATH_NOT_FOUND.
                    if (ex.NativeErrorCode == 2 || ex.NativeErrorCode == 3)
                    {
                        consecutiveMisses++;
                    }
                    // ACCESS_DENIED / other -> skip this slot but keep scanning.
                }
            }
            return disks;
        }

        public static long ReadLengthOnly(int diskNumber)
        {
            using (var handle = DeviceHandleFactory.OpenPhysicalDisk(diskNumber, readOnly: true))
            {
                return ReadLength(handle);
            }
        }

        /// <summary>
        /// Reads the disk's bytes-per-sector value via IOCTL_DISK_GET_DRIVE_GEOMETRY.
        /// Used by the resize path to convert byte counts to sector counts for
        /// FSCTL_EXTEND_VOLUME / FSCTL_SHRINK_VOLUME.
        /// </summary>
        public static int ReadBytesPerSector(int diskNumber)
        {
            using (var handle = DeviceHandleFactory.OpenPhysicalDisk(diskNumber, readOnly: true))
            {
                var size = Marshal.SizeOf<DISK_GEOMETRY>();
                var buffer = Marshal.AllocHGlobal(size);
                try
                {
                    int returned;
                    var ok = NativeMethods.DeviceIoControl(
                        handle, NativeConstants.IOCTL_DISK_GET_DRIVE_GEOMETRY,
                        IntPtr.Zero, 0, buffer, size, out returned, IntPtr.Zero);
                    if (!ok)
                    {
                        throw new Win32Exception(
                            Marshal.GetLastWin32Error(),
                            "IOCTL_DISK_GET_DRIVE_GEOMETRY failed for disk " + diskNumber + ".");
                    }
                    var geom = Marshal.PtrToStructure<DISK_GEOMETRY>(buffer);
                    return geom.BytesPerSector > 0 ? geom.BytesPerSector : 512;
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
        }

        private static long ReadLength(SafeFileHandle handle)
        {
            var length = default(GET_LENGTH_INFORMATION);
            var size = Marshal.SizeOf<GET_LENGTH_INFORMATION>();
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                int returned;
                var ok = NativeMethods.DeviceIoControl(
                    handle, NativeConstants.IOCTL_DISK_GET_LENGTH_INFO,
                    IntPtr.Zero, 0, buffer, size, out returned, IntPtr.Zero);
                if (!ok)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        "IOCTL_DISK_GET_LENGTH_INFO failed.");
                }
                length = Marshal.PtrToStructure<GET_LENGTH_INFORMATION>(buffer);
            }
            finally { Marshal.FreeHGlobal(buffer); }
            return length.Length;
        }

        private static void ReadLayout(SafeFileHandle handle, int diskNumber, Win32DiskInfo info)
        {
            var bufferSize = InitialLayoutBufferSize;
            while (true)
            {
                var buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    int returned;
                    var ok = NativeMethods.DeviceIoControl(
                        handle, NativeConstants.IOCTL_DISK_GET_DRIVE_LAYOUT_EX,
                        IntPtr.Zero, 0, buffer, bufferSize, out returned, IntPtr.Zero);
                    if (!ok)
                    {
                        var error = Marshal.GetLastWin32Error();
                        if (error == NativeConstants.ERROR_INSUFFICIENT_BUFFER
                            || error == NativeConstants.ERROR_MORE_DATA)
                        {
                            bufferSize *= 2;
                            continue;
                        }
                        throw new Win32Exception(
                            error, "IOCTL_DISK_GET_DRIVE_LAYOUT_EX failed.");
                    }

                    var header = Marshal.PtrToStructure<DRIVE_LAYOUT_INFORMATION_EX_HEADER>(buffer);
                    info.PartitionStyle = header.PartitionStyle;
                    if (header.PartitionStyle == PartitionStyle.Mbr)
                    {
                        info.MbrSignature = header.Mbr.Signature;
                        info.MbrChecksum  = header.Mbr.CheckSum;
                    }
                    else if (header.PartitionStyle == PartitionStyle.Gpt)
                    {
                        info.GptDiskId               = header.Gpt.DiskId;
                        info.GptStartingUsableOffset = header.Gpt.StartingUsableOffset;
                        info.GptUsableLength         = header.Gpt.UsableLength;
                        info.GptMaxPartitionCount    = header.Gpt.MaxPartitionCount;
                    }

                    for (int i = 0; i < header.PartitionCount; i++)
                    {
                        var entryPtr = IntPtr.Add(buffer, LayoutHeaderSize + i * PartitionEntrySize);
                        var ex = Marshal.PtrToStructure<PARTITION_INFORMATION_EX>(entryPtr);
                        if (ex.PartitionLength <= 0) { continue; }
                        info.Partitions.Add(Project(diskNumber, ex));
                    }
                    return;
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
        }

        private static Win32PartitionInfo Project(int diskNumber, PARTITION_INFORMATION_EX ex)
        {
            var p = new Win32PartitionInfo
            {
                DiskNumber          = diskNumber,
                PartitionNumber     = ex.PartitionNumber,
                StartingOffset      = ex.StartingOffset,
                LengthBytes         = ex.PartitionLength,
                PartitionStyle      = ex.PartitionStyle,
                RewritePartition    = ex.RewritePartition,
                IsServicePartition  = ex.IsServicePartition,
            };
            if (ex.PartitionStyle == PartitionStyle.Mbr)
            {
                p.MbrType             = ex.Mbr.PartitionType;
                p.BootIndicator       = ex.Mbr.BootIndicator;
                p.RecognizedPartition = ex.Mbr.RecognizedPartition;
                p.HiddenSectors       = ex.Mbr.HiddenSectors;
            }
            else if (ex.PartitionStyle == PartitionStyle.Gpt)
            {
                p.GptType       = ex.Gpt.PartitionType;
                p.GptId         = ex.Gpt.PartitionId;
                p.GptAttributes = ex.Gpt.Attributes;
                p.GptName       = ex.Gpt.Name;
            }
            return p;
        }
    }
}
