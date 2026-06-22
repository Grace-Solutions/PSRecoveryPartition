using System;
using System.Collections.Generic;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Managed projection of a single PARTITION_INFORMATION_EX entry returned
    /// from IOCTL_DISK_GET_DRIVE_LAYOUT_EX. The MBR and GPT-specific fields are
    /// both present; the <see cref="PartitionStyle"/> discriminator indicates
    /// which set is meaningful.
    /// </summary>
    internal sealed class Win32PartitionInfo
    {
        public int DiskNumber { get; set; }
        public int PartitionNumber { get; set; }
        public long StartingOffset { get; set; }
        public long LengthBytes { get; set; }
        public PartitionStyle PartitionStyle { get; set; }
        public bool RewritePartition { get; set; }
        public bool IsServicePartition { get; set; }

        // MBR fields. Only meaningful when PartitionStyle == Mbr.
        public byte MbrType { get; set; }
        public bool BootIndicator { get; set; }
        public bool RecognizedPartition { get; set; }
        public uint HiddenSectors { get; set; }

        // GPT fields. Only meaningful when PartitionStyle == Gpt.
        public Guid GptType { get; set; }
        public Guid GptId { get; set; }
        public ulong GptAttributes { get; set; }
        public string GptName { get; set; }
    }

    /// <summary>
    /// Managed projection of a whole disk: size, partition style, and the
    /// ordered list of partitions parsed from IOCTL_DISK_GET_DRIVE_LAYOUT_EX.
    /// </summary>
    internal sealed class Win32DiskInfo
    {
        public int DiskNumber { get; set; }
        public long SizeBytes { get; set; }
        public PartitionStyle PartitionStyle { get; set; }
        // MBR-specific layout header fields.
        public uint MbrSignature { get; set; }
        public uint MbrChecksum { get; set; }
        // GPT-specific layout header fields.
        public Guid GptDiskId { get; set; }
        public long GptStartingUsableOffset { get; set; }
        public long GptUsableLength { get; set; }
        public uint GptMaxPartitionCount { get; set; }
        // Partitions in starting-offset order, with zero-length placeholders
        // removed (the kernel returns 128 GPT slots; we filter to the active
        // ones with LengthBytes > 0).
        public IList<Win32PartitionInfo> Partitions { get; set; } = new List<Win32PartitionInfo>();

        public string DevicePath { get { return @"\\.\PhysicalDrive" + DiskNumber; } }
    }

    /// <summary>
    /// One disk extent backing a volume (from IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS).
    /// Single-extent volumes are the only shape the module supports for recovery
    /// partitions; multi-extent volumes (spanned / striped) are surfaced as-is
    /// for diagnostic purposes but ignored by the recovery matcher.
    /// </summary>
    internal sealed class Win32VolumeExtent
    {
        public int DiskNumber { get; set; }
        public long StartingOffset { get; set; }
        public long Length { get; set; }
    }

    /// <summary>
    /// Managed projection of a Windows volume: kernel volume name, mount
    /// points, file-system metadata, and the disk extents backing the volume.
    /// </summary>
    internal sealed class Win32VolumeInfo
    {
        public string VolumeName { get; set; } // \\?\Volume{guid}\
        public string Label { get; set; }
        public string FileSystem { get; set; }
        public uint SerialNumber { get; set; }
        public IList<string> MountPoints { get; set; } = new List<string>();
        public IList<Win32VolumeExtent> Extents { get; set; } = new List<Win32VolumeExtent>();

        /// <summary>
        /// First mount point that looks like a drive letter ("X:\"), or null.
        /// </summary>
        public string DriveLetter
        {
            get
            {
                if (MountPoints == null) { return null; }
                foreach (var mp in MountPoints)
                {
                    if (!string.IsNullOrEmpty(mp) && mp.Length >= 2 && mp[1] == ':')
                    {
                        return mp.Substring(0, 2);
                    }
                }
                return null;
            }
        }
    }
}
