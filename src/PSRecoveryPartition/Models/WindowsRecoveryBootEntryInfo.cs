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
        // The source image that was supplied to the cmdlet (for example a UNC or
        // local WIM). This is only the *source* - for a staged ramdisk entry the
        // image is copied onto the recovery partition and the entry boots from that
        // local copy (see StagedImagePath), not from here.
        public FileInfo BootImagePath { get; set; }
        // Volume-relative path where the image was staged on the target partition
        // and that the boot entry actually loads, e.g. \Generic_x64\Generic_x64.wim.
        // Null for flat-expanded entries and for enumerated entries.
        public string StagedImagePath { get; set; }
        // Volume-relative path of the staged boot.sdi for a ramdisk entry.
        public string SdiPath { get; set; }
        // Identifier of the ramdisk device-options object that carries the boot.sdi
        // reference (ramdisk entries only).
        public string DeviceOptionsIdentifier { get; set; }
        // Recovery partition the image was staged onto, when applicable.
        public int? DiskNumber { get; set; }
        public int? PartitionNumber { get; set; }
        public TimeSpan BootTimeout { get; set; }
        public RecoveryBootEntryVisibility Visibility { get; set; }
        public bool IsDefault { get; set; }
        public bool IsRecoveryEntry { get; set; }
    }
}
