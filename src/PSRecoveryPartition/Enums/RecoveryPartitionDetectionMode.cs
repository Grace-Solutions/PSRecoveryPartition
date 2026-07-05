namespace PSRecoveryPartition
{
    /// <summary>
    /// Selects which physical disk(s) <c>Get-RecoveryPartition</c> scans for
    /// recovery partitions. Defaults to <see cref="CurrentOSDisk"/> so recovery /
    /// BCD operations are not performed across every disk in a dual-disk or
    /// dual-boot system in an assumptive manner. Additional scenarios can be added
    /// as new members without breaking existing callers.
    /// </summary>
    public enum RecoveryPartitionDetectionMode
    {
        /// <summary>
        /// Only the physical disk that hosts the running Windows installation
        /// (the disk backing the system drive). This is the default and normally
        /// yields the single recovery partition paired with the current OS.
        /// </summary>
        CurrentOSDisk = 0,

        /// <summary>Every physical disk on the host.</summary>
        AllDisks = 1,

        /// <summary>
        /// Every physical disk except the one hosting the running OS. Useful for
        /// targeting a second OS/data disk without touching the current OS disk.
        /// </summary>
        SecondaryDisksOnly = 2,
    }
}
