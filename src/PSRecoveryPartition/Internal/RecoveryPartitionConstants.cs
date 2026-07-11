namespace PSRecoveryPartition
{
    /// <summary>
    /// Well-known constants for Windows recovery partitions.
    /// </summary>
    internal static class RecoveryPartitionConstants
    {
        // GPT partition type GUID for a Windows Recovery Environment partition.
        public const string GptTypeRecovery = "{de94bba4-06d1-4d40-a16a-bfd50179d6ac}";

        // MBR partition type byte (0x27) for Windows Recovery Environment.
        public const int MbrTypeRecovery = 0x27;

        // GPT attributes for a recovery partition live on the public
        // GptPartitionAttributes flags enum (Recovery, RecoveryHidden), which is
        // the single source of truth for both New-RecoveryPartition and
        // Initialize-RecoveryDisk.

        public const string DefaultLabel = "RECOVERY";
        public const long DefaultSizeBytes = 1024L * 1024L * 1024L; // 1 GiB
        public const int DefaultSizePercentMax = 10;
    }
}
