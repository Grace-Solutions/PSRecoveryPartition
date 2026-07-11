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
        // Native VOLUME_DISK_EXTENTS embeds DISK_EXTENT[] directly after
        // NumberOfDiskExtents. Because DISK_EXTENT contains LARGE_INTEGER
        // fields, the array is 8-byte aligned in the kernel buffer, so the
        // first extent lives at offset 8 (not 4). This explicit padding makes
        // Marshal.SizeOf<VOLUME_DISK_EXTENTS_HEADER>() report 8 and keeps the
        // stride math correct without a hard-coded magic number.
#pragma warning disable 0169
        private int _padding;
#pragma warning restore 0169
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

    // 36 UTF-16 code units (72 bytes), held as raw blittable bytes so the
    // surrounding struct contains no managed object references and may be
    // overlaid against PARTITION_INFORMATION_MBR via LayoutKind.Explicit
    // without tripping the CLR's "object field overlaps non-object field"
    // type-load check.
    [StructLayout(LayoutKind.Sequential, Size = 72)]
    internal struct PARTITION_GPT_NAME
    {
        private ulong _w0, _w1, _w2, _w3, _w4, _w5, _w6, _w7, _w8;

        public unsafe string ToManagedString()
        {
            fixed (ulong* p = &_w0)
            {
                var chars = (char*)p;
                int len = 0;
                while (len < 36 && chars[len] != '\0') { len++; }
                return new string(chars, 0, len);
            }
        }

        public static unsafe PARTITION_GPT_NAME FromString(string value)
        {
            var result = default(PARTITION_GPT_NAME);
            if (string.IsNullOrEmpty(value)) { return result; }
            var src = value.Length > 35 ? value.Substring(0, 35) : value;
            var chars = (char*)&result._w0;
            for (int i = 0; i < src.Length; i++) { chars[i] = src[i]; }
            return result;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PARTITION_INFORMATION_GPT
    {
        public Guid PartitionType;
        public Guid PartitionId;
        public ulong Attributes;
        public PARTITION_GPT_NAME Name;
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
    internal struct CREATE_DISK_MBR
    {
        public uint Signature;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CREATE_DISK_GPT
    {
        public Guid DiskId;
        public uint MaxPartitionCount;
    }

    // CREATE_DISK is PARTITION_STYLE followed by a union of the MBR (4-byte) and
    // GPT (20-byte) variants. The union starts at offset 4; Guid aligns to 4, so
    // the whole struct sizes out to 24 bytes exactly as winioctl.h declares it.
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    internal struct CREATE_DISK
    {
        [FieldOffset(0)] public PartitionStyle PartitionStyle;
        [FieldOffset(4)] public CREATE_DISK_MBR Mbr;
        [FieldOffset(4)] public CREATE_DISK_GPT Gpt;
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

    /// <summary>
    /// FSCTL_SHRINK_VOLUME request kinds, matching ntddvol.h.
    /// </summary>
    internal enum SHRINK_VOLUME_REQUEST_TYPES : int
    {
        ShrinkPrepare = 1,
        ShrinkCommit  = 2,
        ShrinkAbort   = 3,
    }

    /// <summary>
    /// FSCTL_SHRINK_VOLUME input buffer. ShrinkPrepare sets NewNumberOfSectors
    /// to the desired post-shrink volume size; ShrinkCommit and ShrinkAbort
    /// take NewNumberOfSectors=0 (the prepared size is reused).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SHRINK_VOLUME_INFORMATION
    {
        public SHRINK_VOLUME_REQUEST_TYPES ShrinkRequestType;
        public ulong Flags;
        public long  NewNumberOfSectors;
    }
}
