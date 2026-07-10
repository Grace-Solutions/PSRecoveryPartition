namespace PSRecoveryPartition
{
    /// <summary>
    /// Partition table style written to a disk by <c>Initialize-RecoveryDisk</c>.
    /// </summary>
    public enum DiskPartitionScheme
    {
        /// <summary>GUID Partition Table (UEFI). The default.</summary>
        Gpt = 0,
        /// <summary>Master Boot Record (legacy BIOS). Limited to four primary partitions and no MSR.</summary>
        Mbr = 1,
    }

    /// <summary>
    /// How a <see cref="DiskPartitionSpec"/>'s size value is interpreted.
    /// </summary>
    public enum DiskPartitionSizeMode
    {
        /// <summary>The value is an absolute size in bytes.</summary>
        Size = 0,
        /// <summary>The value is a percentage (1-100) of the free space remaining at that point in the layout.</summary>
        Percentage = 1,
    }

    /// <summary>
    /// Role of a partition, which determines its partition type, attributes, and
    /// whether it receives a file system.
    /// </summary>
    public enum DiskPartitionKind
    {
        /// <summary>Basic data partition (the OS / data volume). Formatted NTFS by default.</summary>
        Basic = 0,
        /// <summary>EFI System Partition. Formatted FAT32. On MBR this becomes an active NTFS system partition.</summary>
        Efi = 1,
        /// <summary>Microsoft Reserved partition. Never formatted. Skipped on MBR (MSR is GPT-only).</summary>
        Msr = 2,
        /// <summary>Windows Recovery partition. Formatted NTFS, then tagged with the recovery type and attributes.</summary>
        Recovery = 3,
    }

    /// <summary>
    /// Named, ready-made disk layouts. Each begins with a 1 GiB EFI System
    /// Partition and a 1 GiB Microsoft Reserved partition (GPT); percentages are
    /// taken against the free space remaining after the preceding partitions.
    /// </summary>
    public enum DiskPartitionLayoutPreset
    {
        /// <summary>EFI 1 GiB, MSR 1 GiB, RECOVERY 20% of remaining, OS 100% of remaining.</summary>
        RecoveryFirst = 0,
        /// <summary>EFI 1 GiB, MSR 1 GiB, OS 80% of remaining, RECOVERY 100% of remaining.</summary>
        RecoveryLast = 1,
        /// <summary>EFI 1 GiB, MSR 1 GiB, OS 100% of remaining. No recovery partition.</summary>
        NoRecovery = 2,
    }
}
