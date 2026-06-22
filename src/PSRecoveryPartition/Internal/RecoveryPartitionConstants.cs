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

        // GPT attributes for a recovery partition: required, no drive letter,
        // hidden, no automount. Combined value 0x8000000000000001 historically;
        // we expose individual flags here for the Storage cmdlet path.
        public const long GptAttributesRecovery = unchecked((long)0x8000000000000001UL);

        public const string DefaultLabel = "Recovery";
        public const long DefaultSizeBytes = 1024L * 1024L * 1024L; // 1 GiB
        public const int DefaultSizePercentMax = 10;
    }
}
