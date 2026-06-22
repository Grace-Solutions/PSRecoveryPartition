using System;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Result of mount / dismount operations. Carries the partition being
    /// affected together with the access path that was added or removed.
    /// </summary>
    public sealed class RecoveryPartitionMountResult : RecoveryResultBase
    {
        public RecoveryPartitionInfo Partition { get; set; }
        public string AccessPath { get; set; }
        public bool Mounted { get; set; }
        public bool Changed { get; set; }
        public DateTimeOffset TimestampUtc { get; set; }
    }
}
