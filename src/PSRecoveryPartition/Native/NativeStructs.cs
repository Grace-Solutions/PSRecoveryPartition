using System;
using System.Runtime.InteropServices;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Native struct layouts for the disk/volume IOCTLs. PARTITION_INFORMATION_EX
    /// and DRIVE_LAYOUT_INFORMATION_EX use C unions in winioctl.h; here we model
    /// them with FieldOffset overlays sized to the larger of the two variants so
    /// the layout matches the kernel-returned buffer byte-for-byte.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct DISK_GEOMETRY
    {
        public long Cylinders;
        public int MediaType;
        public int TracksPerCylinder;
        public int SectorsPerTrack;
        public int BytesPerSector;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DISK_GEOMETRY_EX
    {
        public DISK_GEOMETRY Geometry;
        public long DiskSize;
        // Followed by DISK_PARTITION_INFO / DISK_DETECTION_INFO blobs we do not
        // need; callers allocate a generously sized buffer and read only the
        // fixed head.
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GET_LENGTH_INFORMATION
    {
        public long Length;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DISK_EXTENT
    {
        public int DiskNumber;
        public long StartingOffset;
        public long ExtentLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VOLUME_DISK_EXTENTS_HEADER
    {
        public int NumberOfDiskExtents;
        // Followed by DISK_EXTENT[NumberOfDiskExtents]. We marshal extents
        // manually to support multi-extent volumes (spanned / striped sets).
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PARTITION_INFORMATION_MBR
    {
        public byte PartitionType;
        [MarshalAs(UnmanagedType.U1)] public bool BootIndicator;
        [MarshalAs(UnmanagedType.U1)] public bool RecognizedPartition;
        public uint HiddenSectors;
        public Guid PartitionId; // Win10+, zero on older OS.
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PARTITION_INFORMATION_GPT
    {
        public Guid PartitionType;
        public Guid PartitionId;
        public ulong Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 36)]
        public string Name;
    }

    // PARTITION_INFORMATION_EX uses a C union of MBR/GPT variants. We overlay
    // both at the same FieldOffset; the discriminator is PartitionStyle. The
    // GPT variant is the larger (112 bytes total for the union payload), so
    // the struct sizes out to the same width the kernel produces.
    [StructLayout(LayoutKind.Explicit)]
    internal struct PARTITION_INFORMATION_EX
    {
        [FieldOffset(0)]  public PartitionStyle PartitionStyle;
        [FieldOffset(8)]  public long StartingOffset;
        [FieldOffset(16)] public long PartitionLength;
        [FieldOffset(24)] public int PartitionNumber;
        [FieldOffset(28)] [MarshalAs(UnmanagedType.U1)] public bool RewritePartition;
        [FieldOffset(29)] [MarshalAs(UnmanagedType.U1)] public bool IsServicePartition;
        [FieldOffset(32)] public PARTITION_INFORMATION_MBR Mbr;
        [FieldOffset(32)] public PARTITION_INFORMATION_GPT Gpt;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DRIVE_LAYOUT_INFORMATION_MBR
    {
        public uint Signature;
        public uint CheckSum;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DRIVE_LAYOUT_INFORMATION_GPT
    {
        public Guid DiskId;
        public long StartingUsableOffset;
        public long UsableLength;
        public uint MaxPartitionCount;
    }

    // Variable-length header for IOCTL_DISK_GET_DRIVE_LAYOUT_EX. The header is
    // 48 bytes (8 for style + count, 40 for the union padded out to GPT size),
    // followed by PartitionCount PARTITION_INFORMATION_EX entries of 144 bytes
    // each. Callers manually walk the buffer rather than marshalling the
    // variable-length tail.
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    internal struct DRIVE_LAYOUT_INFORMATION_EX_HEADER
    {
        [FieldOffset(0)] public PartitionStyle PartitionStyle;
        [FieldOffset(4)] public int PartitionCount;
        [FieldOffset(8)] public DRIVE_LAYOUT_INFORMATION_MBR Mbr;
        [FieldOffset(8)] public DRIVE_LAYOUT_INFORMATION_GPT Gpt;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DISK_GROW_PARTITION
    {
        public int PartitionNumber;
        public long BytesToGrow;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SET_PARTITION_INFORMATION_EX_GPT
    {
        public PartitionStyle PartitionStyle;
        public PARTITION_INFORMATION_GPT Gpt;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SET_PARTITION_INFORMATION_EX_MBR
    {
        public PartitionStyle PartitionStyle;
        public byte PartitionType;
    }
}
