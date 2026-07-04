using System;
using System.IO;

namespace PSRecoveryPartition
{
    /// <summary>
    /// How a custom recovery boot entry loads its image.
    /// </summary>
    internal enum RecoveryBootMode
    {
        /// <summary>RAM boot: the WIM is mounted as a ramdisk and needs a boot.sdi.</summary>
        Ramdisk = 0,
        /// <summary>Flat / non-RAM boot: the WIM is expanded onto the partition and booted in place.</summary>
        Flat = 1,
    }

    /// <summary>
    /// Fully-resolved description of a custom BCD boot entry to create. Built by
    /// <c>New-WindowsRecoveryBootEntry</c> and consumed by <see cref="BcdEditEngine"/>.
    /// </summary>
    internal sealed class BcdBootEntryRequest
    {
        public string Name;
        public RecoveryBootMode Mode;

        // Drive-letter token (e.g. "X:") valid only for the duration of the
        // bcdedit calls. bcdedit rewrites "[X:]" / "partition=X:" into a
        // persistent NT device id at parse time, so the letter need not survive.
        public string VolumeToken;

        // Ramdisk mode: volume-relative paths to the staged WIM and boot.sdi.
        public string ImageRelativePath;
        public string SdiRelativePath;

        public string SystemRoot = @"\Windows";
        public string LoaderPath;   // e.g. \windows\system32\boot\winload.efi

        public TimeSpan Timeout;
        public RecoveryBootEntryVisibility Visibility;
        public bool SetDefault;
        public bool AddLast;

        // Surfaced on the returned info object for the caller's convenience.
        public FileInfo StagedImage;
    }
}
