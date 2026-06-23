using System;
using System.IO;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Description of a discovered Windows RE / Windows PE image file.
    /// </summary>
    public sealed class WindowsRecoveryImageInfo : RecoveryResultBase
    {
        public FileInfo ImagePath { get; set; }
        public string ImageKind { get; set; }
        public long SizeBytes { get; set; }
        public DateTimeOffset LastWriteTimeUtc { get; set; }
        public string Hash { get; set; }
        public string HashAlgorithm { get; set; }
        public bool IsCurrentlyRegistered { get; set; }
        // Where the image was discovered (Reagent, RecoveryPartition, SystemRoot,
        // UserPath). Lets callers filter or prefer one source over another.
        public string Source { get; set; }
        // Populated when the image lives on a recovery partition that the
        // module enumerated directly via IOCTL + volume staging.
        public int? DiskNumber { get; set; }
        public int? PartitionNumber { get; set; }
        public string VolumePath { get; set; }
    }
}
