using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PSRecoveryPartition.Native
{
    internal static partial class Win32PartitionWriter
    {
        private const long AlignmentBytes = 1024L * 1024L; // 1 MiB
        private const int  EntrySize      = 144;
        private const int  HeaderSize     = 48;

        /// <summary>
        /// Selects an aligned starting offset for a new partition by appending
        /// after the last in-use entry. Throws when no space is available.
        /// </summary>
        private static long ChooseStartingOffset(Win32DiskInfo disk, long requestedSizeBytes)
        {
            long minOffset = disk.PartitionStyle == PartitionStyle.Gpt
                ? Math.Max(disk.GptStartingUsableOffset, AlignmentBytes)
                : AlignmentBytes;
            long maxOffset = disk.PartitionStyle == PartitionStyle.Gpt
                ? disk.GptStartingUsableOffset + disk.GptUsableLength
                : disk.SizeBytes;

            long tail = minOffset;
            foreach (var p in disk.Partitions.OrderBy(p => p.StartingOffset))
            {
                if (p.LengthBytes <= 0) { continue; }
                tail = Math.Max(tail, p.StartingOffset + p.LengthBytes);
            }
            long aligned = AlignUp(tail, AlignmentBytes);
            long requiredEnd = aligned + AlignDown(requestedSizeBytes, AlignmentBytes);
            if (requiredEnd > maxOffset)
            {
                throw new InvalidOperationException(
                    "Insufficient trailing free space on disk " + disk.DiskNumber +
                    " for a " + requestedSizeBytes + "-byte partition; need " +
                    (requiredEnd - aligned) + " bytes, have " + Math.Max(0, maxOffset - aligned) + " bytes.");
            }
            return aligned;
        }

        private static int AllocatePartitionNumber(IList<PARTITION_INFORMATION_EX> entries)
        {
            int max = 0;
            foreach (var e in entries)
            {
                if (e.PartitionNumber > max) { max = e.PartitionNumber; }
            }
            return max + 1;
        }

        private static List<PARTITION_INFORMATION_EX> BuildExistingEntries(Win32DiskInfo disk)
        {
            var list = new List<PARTITION_INFORMATION_EX>();
            foreach (var p in disk.Partitions)
            {
                var entry = new PARTITION_INFORMATION_EX
                {
                    PartitionStyle    = p.PartitionStyle,
                    StartingOffset    = p.StartingOffset,
                    PartitionLength   = p.LengthBytes,
                    PartitionNumber   = p.PartitionNumber,
                    RewritePartition  = false,
                    IsServicePartition = p.IsServicePartition,
                };
                if (p.PartitionStyle == PartitionStyle.Gpt)
                {
                    entry.Gpt = new PARTITION_INFORMATION_GPT
                    {
                        PartitionType = p.GptType,
                        PartitionId   = p.GptId,
                        Attributes    = p.GptAttributes,
                        Name          = p.GptName ?? string.Empty,
                    };
                }
                else if (p.PartitionStyle == PartitionStyle.Mbr)
                {
                    entry.Mbr = new PARTITION_INFORMATION_MBR
                    {
                        PartitionType       = p.MbrType,
                        BootIndicator       = p.BootIndicator,
                        RecognizedPartition = p.RecognizedPartition,
                        HiddenSectors       = p.HiddenSectors,
                    };
                }
                list.Add(entry);
            }
            return list;
        }

        private static void WriteLayout(int diskNumber, Win32DiskInfo disk, IList<PARTITION_INFORMATION_EX> entries)
        {
            var bufferSize = HeaderSize + EntrySize * entries.Count;
            var buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                // Zero-initialise so the GPT/MBR union padding is well-defined.
                for (int i = 0; i < bufferSize; i++) { Marshal.WriteByte(buffer, i, 0); }

                var header = new DRIVE_LAYOUT_INFORMATION_EX_HEADER
                {
                    PartitionStyle = disk.PartitionStyle,
                    PartitionCount = entries.Count,
                };
                if (disk.PartitionStyle == PartitionStyle.Mbr)
                {
                    header.Mbr = new DRIVE_LAYOUT_INFORMATION_MBR
                    {
                        Signature = disk.MbrSignature == 0 ? GenerateMbrSignature() : disk.MbrSignature,
                        CheckSum  = disk.MbrChecksum,
                    };
                }
                else if (disk.PartitionStyle == PartitionStyle.Gpt)
                {
                    header.Gpt = new DRIVE_LAYOUT_INFORMATION_GPT
                    {
                        DiskId               = disk.GptDiskId == Guid.Empty ? Guid.NewGuid() : disk.GptDiskId,
                        StartingUsableOffset = disk.GptStartingUsableOffset,
                        UsableLength         = disk.GptUsableLength,
                        MaxPartitionCount    = disk.GptMaxPartitionCount == 0 ? 128u : disk.GptMaxPartitionCount,
                    };
                }
                Marshal.StructureToPtr(header, buffer, false);

                for (int i = 0; i < entries.Count; i++)
                {
                    var entryPtr = IntPtr.Add(buffer, HeaderSize + i * EntrySize);
                    Marshal.StructureToPtr(entries[i], entryPtr, false);
                }

                using (SafeFileHandle handle = DeviceHandleFactory.OpenPhysicalDisk(diskNumber, readOnly: false))
                {
                    int returned;
                    var ok = NativeMethods.DeviceIoControl(
                        handle, NativeConstants.IOCTL_DISK_SET_DRIVE_LAYOUT_EX,
                        buffer, bufferSize, IntPtr.Zero, 0, out returned, IntPtr.Zero);
                    if (!ok)
                    {
                        throw new Win32Exception(
                            Marshal.GetLastWin32Error(),
                            "IOCTL_DISK_SET_DRIVE_LAYOUT_EX failed for disk " + diskNumber + ".");
                    }
                }
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        private static void UpdateProperties(int diskNumber)
        {
            using (SafeFileHandle handle = DeviceHandleFactory.OpenPhysicalDisk(diskNumber, readOnly: false))
            {
                int returned;
                NativeMethods.DeviceIoControl(
                    handle, NativeConstants.IOCTL_DISK_UPDATE_PROPERTIES,
                    IntPtr.Zero, 0, IntPtr.Zero, 0, out returned, IntPtr.Zero);
                // Failure here is non-fatal: the volume manager will eventually
                // notice on its own. We surface a Win32Exception only on the
                // SET path so callers see write failures clearly.
            }
        }

        private static uint GenerateMbrSignature()
        {
            // Random non-zero signature; the BIOS/MBR specification disallows 0.
            var bytes = Guid.NewGuid().ToByteArray();
            uint sig = BitConverter.ToUInt32(bytes, 0);
            return sig == 0 ? 0xA5A5A5A5u : sig;
        }

        private static long AlignUp(long value, long alignment)
        {
            var rem = value % alignment;
            return rem == 0 ? value : value + (alignment - rem);
        }

        private static long AlignDown(long value, long alignment)
        {
            return value - (value % alignment);
        }
    }
}
