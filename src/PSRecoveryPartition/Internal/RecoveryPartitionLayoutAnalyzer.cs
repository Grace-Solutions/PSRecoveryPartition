using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using PSRecoveryPartition.Native;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Inspects on-disk partition geometry around a recovery partition (existing
    /// or proposed) and produces a <see cref="RecoveryPartitionLayoutAnalysis"/>.
    /// The analyzer is intentionally read-only; it never mutates layout.
    /// </summary>
    internal static class RecoveryPartitionLayoutAnalyzer
    {
        public static RecoveryPartitionLayoutAnalysis Analyze(
            PSCmdlet cmdlet, int diskNumber, int? partitionNumber, long proposedSizeBytes)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var volumes = Win32VolumeMapper.EnumerateAll();
            return Analyze(disk, volumes, partitionNumber, proposedSizeBytes);
        }

        /// <summary>
        /// Pure overload that takes already-read native descriptors. Used by
        /// the cmdlet overload above and intended for unit testing.
        /// </summary>
        public static RecoveryPartitionLayoutAnalysis Analyze(
            Win32DiskInfo disk,
            IList<Win32VolumeInfo> volumes,
            int? partitionNumber,
            long proposedSizeBytes)
        {
            if (disk == null) { throw new ArgumentNullException("disk"); }

            var analysis = new RecoveryPartitionLayoutAnalysis
            {
                DiskNumber = disk.DiskNumber,
                PartitionNumber = partitionNumber
            };

            var partitions = disk.Partitions
                .Where(p => p.LengthBytes > 0)
                .OrderBy(p => p.StartingOffset)
                .ToList();

            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            var os = partitions.FirstOrDefault(p => IsOsPartition(p, volumes, systemDrive));
            if (os != null)
            {
                analysis.OsPartitionNumber   = os.PartitionNumber;
                analysis.OsPartitionOffset   = os.StartingOffset;
                analysis.OsPartitionSizeBytes = os.LengthBytes;
            }

            Win32PartitionInfo self = null;
            if (partitionNumber.HasValue)
            {
                self = partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber.Value);
            }

            long selfOffset = self != null ? self.StartingOffset : 0L;
            long selfSize   = self != null ? self.LengthBytes    : proposedSizeBytes;
            long selfEnd    = selfOffset + selfSize;

            if (self == null && os == null)
            {
                analysis.Position = RecoveryPartitionLayoutPosition.Standalone;
            }
            else if (self != null && os != null)
            {
                analysis.Position = selfOffset == analysis.OsPartitionOffset
                    ? RecoveryPartitionLayoutPosition.SameAsOs
                    : (selfOffset < analysis.OsPartitionOffset
                        ? RecoveryPartitionLayoutPosition.BeforeOs
                        : RecoveryPartitionLayoutPosition.AfterOs);
            }
            else if (self == null)
            {
                analysis.Position = RecoveryPartitionLayoutPosition.Unknown;
            }
            else
            {
                analysis.Position = RecoveryPartitionLayoutPosition.Standalone;
            }

            if (self != null)
            {
                var preceding = partitions
                    .Where(p => p.StartingOffset + p.LengthBytes <= selfOffset)
                    .OrderByDescending(p => p.StartingOffset)
                    .FirstOrDefault();
                if (preceding != null)
                {
                    analysis.PrecedingPartitionNumber = preceding.PartitionNumber;
                    analysis.PrecedingPartitionType   = DescribeType(preceding);
                    analysis.PrecedingPartitionIsOs   = IsOsPartition(preceding, volumes, systemDrive);
                    analysis.LeadingFreeSpaceBytes    = selfOffset - (preceding.StartingOffset + preceding.LengthBytes);
                }
                else
                {
                    analysis.LeadingFreeSpaceBytes = selfOffset;
                }

                var following = partitions
                    .Where(p => p.StartingOffset >= selfEnd)
                    .OrderBy(p => p.StartingOffset)
                    .FirstOrDefault();
                if (following != null)
                {
                    analysis.FollowingPartitionNumber = following.PartitionNumber;
                    analysis.FollowingPartitionType   = DescribeType(following);
                    analysis.FollowingPartitionIsOs   = IsOsPartition(following, volumes, systemDrive);
                    analysis.TrailingFreeSpaceBytes   = following.StartingOffset - selfEnd;
                }
                else
                {
                    analysis.TrailingFreeSpaceBytes = Math.Max(0L, disk.SizeBytes - selfEnd);
                }
            }

            var growBy = Math.Max(0L, proposedSizeBytes - selfSize);
            analysis.CanGrowInPlace   = self == null || analysis.TrailingFreeSpaceBytes >= growBy;
            analysis.CanShrinkInPlace = self == null || proposedSizeBytes <= selfSize;
            analysis.CanRemoveSafely  = !analysis.FollowingPartitionIsOs;

            if (analysis.FollowingPartitionIsOs)
            {
                analysis.Warnings.Add(
                    "Recovery partition is immediately followed by the OS partition; growing or removing it cannot be done in place without first relocating the OS.");
            }
            if (self != null && proposedSizeBytes > selfSize && !analysis.CanGrowInPlace)
            {
                analysis.Warnings.Add(
                    "Requested size (" + proposedSizeBytes + " bytes) exceeds the available trailing free space (" + analysis.TrailingFreeSpaceBytes + " bytes); the resize would fail without shrinking a neighbour first.");
            }
            if (analysis.Position == RecoveryPartitionLayoutPosition.BeforeOs && self != null && proposedSizeBytes != selfSize)
            {
                analysis.Warnings.Add(
                    "Recovery partition is located before the OS partition; any size change will shift the OS partition offset and is not supported in place.");
            }
            return analysis;
        }

        private static bool IsOsPartition(
            Win32PartitionInfo partition, IList<Win32VolumeInfo> volumes, string systemDrive)
        {
            if (partition == null || string.IsNullOrEmpty(systemDrive)) { return false; }
            // GPT boot partitions are Basic Data; MBR boot partitions carry
            // the boot indicator. Combine both with a drive-letter match so we
            // catch the common single-OS host without depending on the
            // Storage module's computed IsBoot property.
            if (partition.PartitionStyle == PartitionStyle.Mbr && partition.BootIndicator) { return true; }
            var volume = volumes == null ? null
                : Win32VolumeMapper.FindForPartition(volumes, partition.DiskNumber, partition.StartingOffset);
            if (volume == null || string.IsNullOrEmpty(volume.DriveLetter)) { return false; }
            return systemDrive.StartsWith(volume.DriveLetter, StringComparison.OrdinalIgnoreCase);
        }

        private static string DescribeType(Win32PartitionInfo p)
        {
            if (p.PartitionStyle == PartitionStyle.Gpt)
            {
                var g = p.GptType.ToString("D").ToLowerInvariant();
                if (g == NativeConstants.GPT_PARTITION_TYPE_EFI_SYSTEM)         { return "EFI System"; }
                if (g == NativeConstants.GPT_PARTITION_TYPE_MICROSOFT_RESERVED) { return "Microsoft Reserved"; }
                if (g == NativeConstants.GPT_PARTITION_TYPE_BASIC_DATA)         { return "Basic Data"; }
                if (g == NativeConstants.GPT_PARTITION_TYPE_MICROSOFT_RECOVERY) { return "Recovery"; }
                return g;
            }
            if (p.PartitionStyle == PartitionStyle.Mbr)
            {
                return p.MbrType == NativeConstants.PARTITION_RECOVERY_MBR
                    ? "Recovery"
                    : "0x" + p.MbrType.ToString("X2", System.Globalization.CultureInfo.InvariantCulture);
            }
            return "Unknown";
        }
    }
}
