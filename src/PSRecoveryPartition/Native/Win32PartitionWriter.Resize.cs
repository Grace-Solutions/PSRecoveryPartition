using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Resize half of <see cref="Win32PartitionWriter"/>. Grow uses
    /// IOCTL_DISK_GROW_PARTITION followed by FSCTL_EXTEND_VOLUME so NTFS
    /// catches up to the larger backing partition. Shrink runs the standard
    /// FSCTL_SHRINK_VOLUME prepare/commit dance, then rewrites the layout
    /// with a reduced PartitionLength via IOCTL_DISK_SET_DRIVE_LAYOUT_EX.
    /// </summary>
    internal static partial class Win32PartitionWriter
    {
        public static Win32PartitionInfo Resize(
            int diskNumber, int partitionNumber, long newSizeBytes)
        {
            if (newSizeBytes <= 0)
            {
                throw new ArgumentOutOfRangeException("newSizeBytes",
                    newSizeBytes, "Partition size must be greater than zero.");
            }
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var part = disk.Partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber);
            if (part == null)
            {
                throw new InvalidOperationException(
                    "Partition " + partitionNumber + " was not found on disk " + diskNumber + ".");
            }
            var aligned = AlignDown(newSizeBytes, AlignmentBytes);
            if (aligned == part.LengthBytes)
            {
                return part;
            }
            if (aligned > part.LengthBytes)
            {
                Grow(diskNumber, partitionNumber, aligned - part.LengthBytes);
            }
            else
            {
                Shrink(diskNumber, partitionNumber, aligned);
            }
            var refreshed = Win32DiskInfoReader.Read(diskNumber);
            return refreshed.Partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber);
        }

        public static void Grow(int diskNumber, int partitionNumber, long bytesToGrow)
        {
            if (bytesToGrow <= 0) { return; }

            var growRequest = new DISK_GROW_PARTITION
            {
                PartitionNumber = partitionNumber,
                BytesToGrow     = bytesToGrow,
            };
            var size = Marshal.SizeOf<DISK_GROW_PARTITION>();
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(growRequest, buffer, false);
                using (SafeFileHandle handle = DeviceHandleFactory.OpenPhysicalDisk(diskNumber, readOnly: false))
                {
                    int returned;
                    var ok = NativeMethods.DeviceIoControl(
                        handle, NativeConstants.IOCTL_DISK_GROW_PARTITION,
                        buffer, size, IntPtr.Zero, 0, out returned, IntPtr.Zero);
                    if (!ok)
                    {
                        throw new Win32Exception(
                            Marshal.GetLastWin32Error(),
                            "IOCTL_DISK_GROW_PARTITION failed for disk " + diskNumber +
                            " partition " + partitionNumber + ".");
                    }
                }
            }
            finally { Marshal.FreeHGlobal(buffer); }
            UpdateProperties(diskNumber);

            // Tell NTFS to expand into the new partition tail.
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var part = disk.Partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber);
            if (part == null) { return; }
            var bps = Win32DiskInfoReader.ReadBytesPerSector(diskNumber);
            ExtendFileSystem(diskNumber, partitionNumber, part.StartingOffset, part.LengthBytes / Math.Max(1, bps));
        }

        public static void Shrink(int diskNumber, int partitionNumber, long newSizeBytes)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var part = disk.Partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber);
            if (part == null)
            {
                throw new InvalidOperationException(
                    "Partition " + partitionNumber + " was not found on disk " + diskNumber + ".");
            }
            if (newSizeBytes >= part.LengthBytes) { return; }

            var bps = Win32DiskInfoReader.ReadBytesPerSector(diskNumber);
            var newSectors = newSizeBytes / Math.Max(1, bps);
            ShrinkFileSystem(diskNumber, partitionNumber, part.StartingOffset, newSectors);

            // Rewrite the layout to reduce PartitionLength to the new size.
            var entries = BuildExistingEntries(disk);
            var idx = entries.FindIndex(e => e.PartitionNumber == partitionNumber);
            if (idx < 0) { return; }
            var entry = entries[idx];
            entry.PartitionLength  = newSizeBytes;
            entry.RewritePartition = true;
            entries[idx] = entry;

            WriteLayout(diskNumber, disk, entries);
            UpdateProperties(diskNumber);
        }

    }
}
