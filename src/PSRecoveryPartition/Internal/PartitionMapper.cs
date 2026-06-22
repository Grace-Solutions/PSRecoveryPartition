using System;
using System.Globalization;
using System.Linq;
using PSRecoveryPartition.Native;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Helpers for translating native Win32 partition / volume descriptors into
    /// <see cref="RecoveryPartitionInfo"/> instances.
    /// </summary>
    internal static class PartitionMapper
    {
        public static RecoveryPartitionInfo FromNative(Win32PartitionInfo partition, Win32VolumeInfo volume)
        {
            if (partition == null) { return null; }
            var info = new RecoveryPartitionInfo
            {
                DiscoveredAtUtc   = DateTimeOffset.UtcNow,
                ExecutionMethod   = RecoveryExecutionMethod.Native,
                DiskNumber        = partition.DiskNumber,
                PartitionNumber   = partition.PartitionNumber,
                DiskPath          = @"\\.\PhysicalDrive" + partition.DiskNumber,
                Offset            = partition.StartingOffset,
                SizeBytes         = partition.LengthBytes,
            };

            if (partition.PartitionStyle == PartitionStyle.Gpt)
            {
                info.Guid              = "{" + partition.GptId.ToString("D").ToLowerInvariant() + "}";
                info.PartitionUniqueId = info.Guid;
                info.GptType           = "{" + partition.GptType.ToString("D").ToLowerInvariant() + "}";
                info.NoDefaultDriveLetter = (partition.GptAttributes
                    & NativeConstants.GPT_BASIC_DATA_ATTRIBUTE_NO_AUTOMOUNT) != 0;
                info.IsHidden = (partition.GptAttributes
                    & NativeConstants.GPT_BASIC_DATA_ATTRIBUTE_HIDDEN) != 0;
            }
            else if (partition.PartitionStyle == PartitionStyle.Mbr)
            {
                info.MbrType  = DescribeMbrType(partition.MbrType);
                info.IsHidden = false;
            }

            if (volume != null)
            {
                info.Label       = volume.Label;
                info.FileSystem  = volume.FileSystem;
                info.DriveLetter = volume.DriveLetter;
                if (volume.MountPoints != null)
                {
                    info.AccessPaths = volume.MountPoints.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                }
            }

            info.IsRecoveryPartition = (partition.PartitionStyle == PartitionStyle.Gpt
                                            && IsRecoveryGptType(info.GptType))
                || (partition.PartitionStyle == PartitionStyle.Mbr
                    && IsRecoveryMbrType((long)partition.MbrType))
                || string.Equals(info.Label, RecoveryPartitionConstants.DefaultLabel, StringComparison.OrdinalIgnoreCase);
            return info;
        }

        // Friendly MBR-type names matching the Storage module conventions where
        // possible; unknown bytes fall back to a "0xNN" hex form.
        private static string DescribeMbrType(byte b)
        {
            switch (b)
            {
                case 0x00: return "Unused";
                case 0x06: return "Huge";
                case 0x07: return "IFS";
                case 0x0B:
                case 0x0C: return "FAT32";
                case 0x04:
                case 0x0E: return "FAT16";
                case NativeConstants.PARTITION_RECOVERY_MBR: return "Recovery";
                case 0x42: return "LDM";
                case 0xEE: return "GPT";
                case 0xEF: return "ESP";
                default:   return "0x" + b.ToString("X2", CultureInfo.InvariantCulture);
            }
        }

        public static bool IsRecoveryGptType(string gptType)
        {
            if (string.IsNullOrEmpty(gptType)) { return false; }
            return string.Equals(gptType.Trim('{', '}'),
                RecoveryPartitionConstants.GptTypeRecovery.Trim('{', '}'),
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRecoveryMbrType(string mbrType)
        {
            if (string.IsNullOrEmpty(mbrType)) { return false; }
            if (string.Equals(mbrType, "Recovery", StringComparison.OrdinalIgnoreCase)) { return true; }
            if (string.Equals(mbrType, "WinRE", StringComparison.OrdinalIgnoreCase)) { return true; }
            long parsed;
            if (long.TryParse(mbrType, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out parsed))
            {
                return parsed == RecoveryPartitionConstants.MbrTypeRecovery;
            }
            return false;
        }

        public static bool IsRecoveryMbrType(long mbrTypeNumber)
        {
            return mbrTypeNumber == RecoveryPartitionConstants.MbrTypeRecovery;
        }
    }
}
