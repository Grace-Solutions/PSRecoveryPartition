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
    }
}
