namespace PSRecoveryPartition
{
    /// <summary>
    /// File system a partition is formatted with. PowerShell converts the member
    /// name from a string automatically, so <c>'NTFS'</c>, <c>'exFAT'</c>, and
    /// <c>'ReFS'</c> all bind case-insensitively.
    /// </summary>
    public enum DiskFileSystem
    {
        /// <summary>NTFS. The default for every partition except an EFI System Partition.</summary>
        Ntfs = 0,
        /// <summary>FAT32. The default (and the only valid choice) for an EFI System Partition.</summary>
        Fat32 = 1,
        /// <summary>exFAT.</summary>
        ExFat = 2,
        /// <summary>FAT (FAT16).</summary>
        Fat = 3,
        /// <summary>ReFS.</summary>
        ReFs = 4,
    }
}
