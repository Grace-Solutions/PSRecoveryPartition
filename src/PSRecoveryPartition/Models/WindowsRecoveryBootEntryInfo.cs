using System;
using System.IO;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Description of a single recovery boot entry as discovered in or applied
    /// to the Windows Boot Configuration Data store.
    /// </summary>
    public sealed class WindowsRecoveryBootEntryInfo : RecoveryResultBase
    {
        public string Identifier { get; set; }
        public string Name { get; set; }
        public FileInfo BootImagePath { get; set; }
        public TimeSpan BootTimeout { get; set; }
        public RecoveryBootEntryVisibility Visibility { get; set; }
        public bool IsDefault { get; set; }
        public bool IsRecoveryEntry { get; set; }
        public DateTimeOffset DiscoveredAtUtc { get; set; }
    }
}
