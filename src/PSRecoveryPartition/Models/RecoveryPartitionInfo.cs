using System;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Description of a single recovery partition discovered or modified by the
    /// module. Wraps the underlying Storage / CIM partition data.
    /// </summary>
    public sealed class RecoveryPartitionInfo : RecoveryResultBase
    {
        public int DiskNumber { get; set; }
        public int PartitionNumber { get; set; }
        public string DiskPath { get; set; }
        public string PartitionUniqueId { get; set; }
        public string Guid { get; set; }
        public string GptType { get; set; }
        public string MbrType { get; set; }
        public long Offset { get; set; }
        public long SizeBytes { get; set; }
        public string Label { get; set; }
        public string FileSystem { get; set; }
        public string DriveLetter { get; set; }
        public string[] AccessPaths { get; set; }
        // \\?\Volume{guid}\ form (stable across reboots, no drive letter required).
        public string VolumePath { get; set; }
        // \Device\HarddiskN\PartitionM (NT object name).
        public string DevicePath { get; set; }
        // \\?\GLOBALROOT\Device\HarddiskN\PartitionM (Win32 namespace entry into the NT object).
        public string GlobalRootPath { get; set; }
        public bool IsRecoveryPartition { get; set; }
        public bool IsHidden { get; set; }
        public bool NoDefaultDriveLetter { get; set; }
        public DateTimeOffset DiscoveredAtUtc { get; set; }
    }
}
