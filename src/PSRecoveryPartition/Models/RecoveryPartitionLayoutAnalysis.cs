using System.Collections.Generic;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Position of a recovery partition relative to the Windows OS partition on
    /// the same disk.
    /// </summary>
    public enum RecoveryPartitionLayoutPosition
    {
        Unknown,
        Standalone,
        BeforeOs,
        AfterOs,
        SameAsOs
    }

    /// <summary>
    /// Result of inspecting the on-disk layout around a recovery partition or a
    /// proposed recovery partition slot. Captures neighbouring partitions, free
    /// space windows, and a synthesised list of warnings the caller should heed
    /// before resizing or removing the partition.
    /// </summary>
    public sealed class RecoveryPartitionLayoutAnalysis
    {
        public RecoveryPartitionLayoutAnalysis()
        {
            Warnings = new List<string>();
        }

        public int DiskNumber { get; set; }
        public int? PartitionNumber { get; set; }

        public RecoveryPartitionLayoutPosition Position { get; set; }

        public int? OsPartitionNumber { get; set; }
        public long? OsPartitionOffset { get; set; }
        public long? OsPartitionSizeBytes { get; set; }

        public int? PrecedingPartitionNumber { get; set; }
        public string PrecedingPartitionType { get; set; }
        public bool PrecedingPartitionIsOs { get; set; }

        public int? FollowingPartitionNumber { get; set; }
        public string FollowingPartitionType { get; set; }
        public bool FollowingPartitionIsOs { get; set; }

        public long LeadingFreeSpaceBytes { get; set; }
        public long TrailingFreeSpaceBytes { get; set; }

        public bool CanGrowInPlace { get; set; }
        public bool CanShrinkInPlace { get; set; }
        public bool CanRemoveSafely { get; set; }

        public IList<string> Warnings { get; set; }
    }
}
