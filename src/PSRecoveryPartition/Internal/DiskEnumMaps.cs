using System;
using PSRecoveryPartition.Native;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Translates the module's public enums to the values the native layer wants:
    /// GPT type GUIDs and the file-system names fmifs!FormatEx accepts.
    /// </summary>
    internal static class DiskEnumMaps
    {
        private static readonly Guid BasicData = new Guid(NativeConstants.GPT_PARTITION_TYPE_BASIC_DATA);
        private static readonly Guid EfiSystem = new Guid(NativeConstants.GPT_PARTITION_TYPE_EFI_SYSTEM);
        private static readonly Guid Reserved  = new Guid(NativeConstants.GPT_PARTITION_TYPE_MICROSOFT_RESERVED);
        private static readonly Guid Recovery  = new Guid(NativeConstants.GPT_PARTITION_TYPE_MICROSOFT_RECOVERY);

        public static Guid ToGuid(GptPartitionType type)
        {
            switch (type)
            {
                case GptPartitionType.BasicData:         return BasicData;
                case GptPartitionType.EfiSystem:         return EfiSystem;
                case GptPartitionType.MicrosoftReserved: return Reserved;
                case GptPartitionType.WindowsRecovery:   return Recovery;
                default:
                    throw new ArgumentOutOfRangeException("type", type,
                        "No GPT type GUID is defined for " + type + ".");
            }
        }

        public static GptPartitionType FromGuid(Guid guid)
        {
            if (guid == BasicData) { return GptPartitionType.BasicData; }
            if (guid == EfiSystem) { return GptPartitionType.EfiSystem; }
            if (guid == Reserved)  { return GptPartitionType.MicrosoftReserved; }
            if (guid == Recovery)  { return GptPartitionType.WindowsRecovery; }
            return GptPartitionType.Unknown;
        }

        /// <summary>The GPT type a partition of this role receives once it is fully tagged.</summary>
        public static GptPartitionType ToGptType(DiskPartitionKind kind)
        {
            switch (kind)
            {
                case DiskPartitionKind.Efi:      return GptPartitionType.EfiSystem;
                case DiskPartitionKind.Msr:      return GptPartitionType.MicrosoftReserved;
                case DiskPartitionKind.Recovery: return GptPartitionType.WindowsRecovery;
                default:                         return GptPartitionType.BasicData;
            }
        }

        /// <summary>The file-system name fmifs!FormatEx expects.</summary>
        public static string ToNativeName(DiskFileSystem fileSystem)
        {
            switch (fileSystem)
            {
                case DiskFileSystem.Ntfs:  return "NTFS";
                case DiskFileSystem.Fat32: return "FAT32";
                case DiskFileSystem.ExFat: return "exFAT";
                case DiskFileSystem.Fat:   return "FAT";
                case DiskFileSystem.ReFs:  return "ReFS";
                default:
                    throw new ArgumentOutOfRangeException("fileSystem", fileSystem,
                        "Unsupported file system " + fileSystem + ".");
            }
        }

        /// <summary>Default file system for a partition role: FAT32 for an ESP, NTFS otherwise.</summary>
        public static DiskFileSystem DefaultFileSystem(DiskPartitionKind kind)
        {
            return kind == DiskPartitionKind.Efi ? DiskFileSystem.Fat32 : DiskFileSystem.Ntfs;
        }
    }
}
