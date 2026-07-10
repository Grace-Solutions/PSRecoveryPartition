using System;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Win32 / kernel32 constants used by the native interop layer.
    /// IOCTL codes are precomputed via the CTL_CODE macro:
    /// (DeviceType &lt;&lt; 16) | (Access &lt;&lt; 14) | (Function &lt;&lt; 2) | Method.
    /// </summary>
    internal static class NativeConstants
    {
        // CreateFile access / share / disposition.
        public const uint GENERIC_READ      = 0x80000000;
        public const uint GENERIC_WRITE     = 0x40000000;
        public const uint FILE_SHARE_READ   = 0x00000001;
        public const uint FILE_SHARE_WRITE  = 0x00000002;
        public const uint CREATE_ALWAYS     = 2;
        public const uint OPEN_EXISTING     = 3;
        public const uint FILE_ATTRIBUTE_NORMAL   = 0x00000080;
        public const uint FILE_ATTRIBUTE_HIDDEN   = 0x00000002;
        public const uint FILE_ATTRIBUTE_SYSTEM   = 0x00000004;
        public const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
        public const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        // Win32 error codes used for sized-buffer retry loops.
        public const int ERROR_SUCCESS           = 0;
        public const int ERROR_MORE_DATA         = 234;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public const int ERROR_NO_MORE_FILES     = 18;

        // Disk IOCTLs (IOCTL_DISK_BASE = 0x07).
        public const uint IOCTL_DISK_GET_DRIVE_GEOMETRY    = 0x00070000;
        public const uint IOCTL_DISK_GET_DRIVE_GEOMETRY_EX = 0x000700A0;
        public const uint IOCTL_DISK_GET_LENGTH_INFO       = 0x0007405C;
        public const uint IOCTL_DISK_GET_PARTITION_INFO_EX = 0x00070048;
        public const uint IOCTL_DISK_SET_PARTITION_INFO_EX = 0x0007C04C;
        public const uint IOCTL_DISK_GET_DRIVE_LAYOUT_EX   = 0x00070050;
        public const uint IOCTL_DISK_SET_DRIVE_LAYOUT_EX   = 0x0007C054;
        public const uint IOCTL_DISK_UPDATE_PROPERTIES     = 0x00070140;
        public const uint IOCTL_DISK_GROW_PARTITION        = 0x0007C0D0;
        // CTL_CODE(0x07, 0x0016, METHOD_BUFFERED, FILE_READ_ACCESS|FILE_WRITE_ACCESS)
        public const uint IOCTL_DISK_CREATE_DISK           = 0x0007C058;
        // CTL_CODE(0x07, 0x0040, METHOD_BUFFERED, FILE_READ_ACCESS|FILE_WRITE_ACCESS)
        public const uint IOCTL_DISK_DELETE_DRIVE_LAYOUT   = 0x0007C100;

        // Volume IOCTLs (IOCTL_VOLUME_BASE = 0x56).
        public const uint IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS = 0x00560000;

        // File-system FSCTLs (FILE_DEVICE_FILE_SYSTEM = 0x09).
        // CTL_CODE(9, 60, METHOD_BUFFERED, FILE_ANY_ACCESS)  -> 0x000900F0
        // CTL_CODE(9, 108, METHOD_BUFFERED, FILE_ANY_ACCESS) -> 0x000901B0
        public const uint FSCTL_EXTEND_VOLUME = 0x000900F0;
        public const uint FSCTL_SHRINK_VOLUME = 0x000901B0;
        public const uint FSCTL_LOCK_VOLUME   = 0x00090018;
        public const uint FSCTL_UNLOCK_VOLUME = 0x0009001C;
        public const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;

        // MBR partition type bytes.
        public const byte PARTITION_ENTRY_UNUSED = 0x00;
        public const byte PARTITION_HUGE         = 0x06;
        public const byte PARTITION_IFS          = 0x07; // NTFS/exFAT
        public const byte PARTITION_RECOVERY_MBR = 0x27; // Windows recovery / OEM service

        // GPT partition attributes (DEFINED_GPT_ATTRIBUTES). The recovery
        // partition has all three set in addition to PLATFORM_REQUIRED, so the
        // composite value 0x8000000000000001UL is the canonical Microsoft
        // recovery attribute mask.
        public const ulong GPT_ATTRIBUTE_PLATFORM_REQUIRED         = 0x0000000000000001UL;
        public const ulong GPT_BASIC_DATA_ATTRIBUTE_NO_DRIVE_LETTER = 0x8000000000000000UL;
        public const ulong GPT_BASIC_DATA_ATTRIBUTE_HIDDEN          = 0x4000000000000000UL;
        public const ulong GPT_BASIC_DATA_ATTRIBUTE_SHADOW_COPY     = 0x2000000000000000UL;
        public const ulong GPT_BASIC_DATA_ATTRIBUTE_READ_ONLY       = 0x1000000000000000UL;
        public const ulong GPT_BASIC_DATA_ATTRIBUTE_NO_AUTOMOUNT    = 0x0000000080000000UL;

        // Canonical GPT type GUIDs (string form for cross-checks with the
        // existing RecoveryPartitionConstants table).
        public const string GPT_PARTITION_TYPE_BASIC_DATA          = "ebd0a0a2-b9e5-4433-87c0-68b6b72699c7";
        public const string GPT_PARTITION_TYPE_MICROSOFT_RECOVERY  = "de94bba4-06d1-4d40-a16a-bfd50179d6ac";
        public const string GPT_PARTITION_TYPE_MICROSOFT_RESERVED  = "e3c9e316-0b5c-4db8-817d-f92df00215ae";
        public const string GPT_PARTITION_TYPE_EFI_SYSTEM          = "c12a7328-f81f-11d2-ba4b-00a0c93ec93b";
    }

    /// <summary>
    /// Partition-table style flavour returned by IOCTL_DISK_GET_DRIVE_LAYOUT_EX.
    /// Matches the Windows PARTITION_STYLE enum exactly.
    /// </summary>
    internal enum PartitionStyle : int
    {
        Mbr = 0,
        Gpt = 1,
        Raw = 2,
    }
}
