namespace PSRecoveryPartition
{
    /// <summary>
    /// Well-known GPT partition type GUIDs, by name. <see cref="Unknown"/> means
    /// the partition carries a type GUID that this module does not recognise; the
    /// raw GUID is always available on <c>RecoveryPartitionInfo.GptType</c>.
    /// </summary>
    public enum GptPartitionType
    {
        /// <summary>A type GUID outside the well-known set (or an MBR disk).</summary>
        Unknown = 0,
        /// <summary>Basic data partition (ebd0a0a2-b9e5-4433-87c0-68b6b72699c7). The ordinary OS / data volume.</summary>
        BasicData = 1,
        /// <summary>EFI System Partition (c12a7328-f81f-11d2-ba4b-00a0c93ec93b).</summary>
        EfiSystem = 2,
        /// <summary>Microsoft Reserved partition (e3c9e316-0b5c-4db8-817d-f92df00215ae).</summary>
        MicrosoftReserved = 3,
        /// <summary>Windows Recovery Environment partition (de94bba4-06d1-4d40-a16a-bfd50179d6ac).</summary>
        WindowsRecovery = 4,
    }
}
