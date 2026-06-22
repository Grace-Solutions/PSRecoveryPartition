using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Mutates partition layout via IOCTL_DISK_SET_DRIVE_LAYOUT_EX. Every
    /// write reads the current layout first, mutates it in-place, and resubmits
    /// the entire layout (the kernel contract). IOCTL_DISK_UPDATE_PROPERTIES
    /// is called after each mutation to refresh the volume manager's view.
    /// </summary>
    internal static partial class Win32PartitionWriter
    {

        public static Win32PartitionInfo Create(
            int diskNumber,
            long requestedSizeBytes,
            Guid gptType,
            byte mbrType,
            ulong gptAttributes,
            string gptName)
        {
            if (requestedSizeBytes <= 0)
            {
                throw new ArgumentOutOfRangeException("requestedSizeBytes",
                    requestedSizeBytes, "Partition size must be greater than zero.");
            }

            var disk = Win32DiskInfoReader.Read(diskNumber);
            long startingOffset = ChooseStartingOffset(disk, requestedSizeBytes);
            long partitionLength = AlignDown(requestedSizeBytes, AlignmentBytes);

            var entries = BuildExistingEntries(disk);
            var newEntry = new PARTITION_INFORMATION_EX
            {
                PartitionStyle   = disk.PartitionStyle,
                StartingOffset   = startingOffset,
                PartitionLength  = partitionLength,
                PartitionNumber  = AllocatePartitionNumber(entries),
                RewritePartition = true,
            };
            if (disk.PartitionStyle == PartitionStyle.Gpt)
            {
                newEntry.Gpt = new PARTITION_INFORMATION_GPT
                {
                    PartitionType = gptType,
                    PartitionId   = Guid.NewGuid(),
                    Attributes    = gptAttributes,
                    Name          = string.IsNullOrEmpty(gptName) ? string.Empty : gptName,
                };
            }
            else
            {
                newEntry.Mbr = new PARTITION_INFORMATION_MBR
                {
                    PartitionType       = mbrType,
                    BootIndicator       = false,
                    RecognizedPartition = true,
                    HiddenSectors       = (uint)(startingOffset / 512L),
                };
            }
            entries.Add(newEntry);

            WriteLayout(diskNumber, disk, entries);
            UpdateProperties(diskNumber);

            var refreshed = Win32DiskInfoReader.Read(diskNumber);
            return refreshed.Partitions.FirstOrDefault(p => p.StartingOffset == startingOffset);
        }

        public static void Remove(int diskNumber, int partitionNumber)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var entries = BuildExistingEntries(disk);
            var target = entries.FindIndex(e => e.PartitionNumber == partitionNumber);
            if (target < 0)
            {
                throw new InvalidOperationException(
                    "Partition " + partitionNumber + " was not found on disk " + diskNumber + ".");
            }
            var entry = entries[target];
            entry.PartitionLength  = 0;
            entry.RewritePartition = true;
            if (disk.PartitionStyle == PartitionStyle.Gpt)
            {
                entry.Gpt = new PARTITION_INFORMATION_GPT
                {
                    PartitionType = Guid.Empty,
                    PartitionId   = Guid.Empty,
                    Attributes    = 0,
                    Name          = string.Empty,
                };
            }
            else
            {
                entry.Mbr = new PARTITION_INFORMATION_MBR { PartitionType = NativeConstants.PARTITION_ENTRY_UNUSED };
            }
            entries[target] = entry;

            WriteLayout(diskNumber, disk, entries);
            UpdateProperties(diskNumber);
        }

        /// <summary>
        /// Updates the GPT attribute bitmask (Hidden / NoAutomount / etc.) for
        /// an existing GPT partition. No-op on MBR disks.
        /// </summary>
        public static void SetGptAttributes(
            int diskNumber, int partitionNumber, ulong attributes, Guid? newGptType = null, string newGptName = null)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            if (disk.PartitionStyle != PartitionStyle.Gpt)
            {
                throw new InvalidOperationException(
                    "SetGptAttributes is only valid on GPT disks (disk " + diskNumber + " is " + disk.PartitionStyle + ").");
            }
            var entries = BuildExistingEntries(disk);
            var idx = entries.FindIndex(e => e.PartitionNumber == partitionNumber);
            if (idx < 0)
            {
                throw new InvalidOperationException(
                    "Partition " + partitionNumber + " was not found on disk " + diskNumber + ".");
            }
            var entry = entries[idx];
            var gpt = entry.Gpt;
            gpt.Attributes = attributes;
            if (newGptType.HasValue) { gpt.PartitionType = newGptType.Value; }
            if (newGptName != null)  { gpt.Name = newGptName; }
            entry.Gpt = gpt;
            entry.RewritePartition = true;
            entries[idx] = entry;

            WriteLayout(diskNumber, disk, entries);
            UpdateProperties(diskNumber);
        }

        /// <summary>
        /// Sets the MBR partition type byte (the canonical path for 0x27).
        /// Throws on a GPT disk.
        /// </summary>
        public static void SetMbrType(int diskNumber, int partitionNumber, byte mbrType)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            if (disk.PartitionStyle != PartitionStyle.Mbr)
            {
                throw new InvalidOperationException(
                    "SetMbrType is only valid on MBR disks (disk " + diskNumber + " is " + disk.PartitionStyle + ").");
            }
            var entries = BuildExistingEntries(disk);
            var idx = entries.FindIndex(e => e.PartitionNumber == partitionNumber);
            if (idx < 0)
            {
                throw new InvalidOperationException(
                    "Partition " + partitionNumber + " was not found on disk " + diskNumber + ".");
            }
            var entry = entries[idx];
            var mbr = entry.Mbr;
            mbr.PartitionType = mbrType;
            entry.Mbr = mbr;
            entry.RewritePartition = true;
            entries[idx] = entry;

            WriteLayout(diskNumber, disk, entries);
            UpdateProperties(diskNumber);
        }
    }
}
