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
        // The BCD object's group as reported by bcdedit, e.g. "Windows Boot
        // Loader", "Device options", "Firmware Application (101fffff)". Only
        // "Windows Boot Loader" objects are real bootable entries; supporting
        // objects such as the ramdisk "Device options" (which carries
        // ramdisksdipath / boot.sdi) are surfaced only under -IncludeAll and are
        // never classified as recovery entries.
        public string ObjectType { get; set; }
        public FileInfo BootImagePath { get; set; }
        public TimeSpan BootTimeout { get; set; }
        public RecoveryBootEntryVisibility Visibility { get; set; }
        public bool IsDefault { get; set; }
        public bool IsRecoveryEntry { get; set; }
    }
}
