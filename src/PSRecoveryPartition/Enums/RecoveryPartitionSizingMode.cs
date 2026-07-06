namespace PSRecoveryPartition
{
    /// <summary>
    /// How a recovery partition's size was requested: the built-in default, an
    /// explicit byte count (<c>-SizeBytes</c>), or a percentage of the disk
    /// (<c>-SizePercent</c>).
    /// </summary>
    public enum RecoveryPartitionSizingMode
    {
        Default,
        ExplicitBytes,
        Percent
    }
}
