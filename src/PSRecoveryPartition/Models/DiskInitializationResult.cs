using System.Collections.Generic;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Result of <c>Initialize-RecoveryDisk</c>: the scheme written and the
    /// partitions that now exist on the disk, in on-disk order.
    /// </summary>
    public sealed class DiskInitializationResult : RecoveryResultBase
    {
        public DiskInitializationResult()
        {
            Partitions = new List<RecoveryPartitionInfo>();
        }

        public int DiskNumber { get; set; }
        public DiskPartitionScheme PartitionScheme { get; set; }
        public long DiskSizeBytes { get; set; }
        public bool Changed { get; set; }
        public IList<RecoveryPartitionInfo> Partitions { get; set; }
    }
}
