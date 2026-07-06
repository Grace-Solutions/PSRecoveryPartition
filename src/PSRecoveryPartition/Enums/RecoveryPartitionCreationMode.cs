namespace PSRecoveryPartition
{
    /// <summary>
    /// Strategy used to place a new recovery partition on the target disk. A
    /// recovery partition is always created <b>after</b> the existing partitions
    /// (at the tail of the disk); it is never placed before the OS partition,
    /// because that would require shifting the OS partition's starting offset,
    /// which cannot be done in place. These modes only differ in how trailing free
    /// space is obtained.
    /// </summary>
    public enum RecoveryPartitionCreationMode
    {
        /// <summary>
        /// Append the partition into existing free space at the end of the disk.
        /// No existing partition is moved or resized. Fails when there is not
        /// enough trailing free space. This is the safe default — it never touches
        /// existing data.
        /// </summary>
        UseTrailingFreeSpace = 0,

        /// <summary>
        /// If trailing free space is insufficient, shrink the last partition on the
        /// disk (typically the OS partition) by the shortfall to free space at the
        /// end, then append the recovery partition after it. The shrink is a normal
        /// NTFS shrink — it fails rather than destroying data if the tail of the
        /// volume is occupied.
        /// </summary>
        ShrinkToFit = 1,

        /// <summary>
        /// Only create the partition when the disk has no existing partitions;
        /// refuse otherwise. The most conservative choice for provisioning fresh
        /// disks.
        /// </summary>
        RequireEmptyDisk = 2,
    }
}
