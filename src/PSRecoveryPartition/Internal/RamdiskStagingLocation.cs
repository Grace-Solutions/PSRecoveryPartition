namespace PSRecoveryPartition
{
    /// <summary>
    /// Where a ramdisk recovery boot entry's payload lives, resolved from the BCD
    /// store. Produced by <see cref="BcdEditEngine.ResolveRamdiskStaging"/> and
    /// consumed by the boot-image replace and file-cleanup paths.
    /// </summary>
    internal sealed class RamdiskStagingLocation
    {
        /// <summary>True when the entry is a ramdisk (staged-WIM) boot entry.</summary>
        public bool IsRamdisk;

        /// <summary>Raw BCD volume token, e.g. <c>\Device\HarddiskVolume4</c> or <c>C:</c>.</summary>
        public string VolumeToken;

        /// <summary>Volume-relative path to the staged WIM, e.g. <c>\Recovery\WindowsRE\image.wim</c>.</summary>
        public string ImageRelativePath;

        /// <summary>Volume-relative path to the staged boot.sdi (null when unknown).</summary>
        public string SdiRelativePath;

        /// <summary>Identifier of the ramdisk device-options object (null for the shared {ramdiskoptions}).</summary>
        public string DeviceOptionsId;

        /// <summary>True when <see cref="DiskNumber"/>/<see cref="PartitionNumber"/> were resolved.</summary>
        public bool LocationResolved;

        public int DiskNumber = -1;
        public int PartitionNumber = -1;
    }
}
