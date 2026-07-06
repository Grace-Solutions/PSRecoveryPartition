using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Threading;
using PSRecoveryPartition.Native;

namespace PSRecoveryPartition
{
    /// <summary>
    /// High level operations on recovery partitions, implemented on top of the
    /// native Win32 IOCTL / fmifs interop surface. Returns rich
    /// <see cref="RecoveryPartitionInfo"/> objects so cmdlets stay thin.
    /// </summary>
    internal sealed class RecoveryPartitionEngine
    {
        private readonly PSCmdlet _owner;

        public RecoveryPartitionEngine(PSCmdlet owner)
        {
            _owner = owner;
        }

        public IList<RecoveryPartitionInfo> Get(
            int? diskNumber = null,
            bool recoveryOnly = true,
            RecoveryPartitionDetectionMode detectionMode = RecoveryPartitionDetectionMode.AllDisks)
        {
            var volumes = Win32VolumeMapper.EnumerateAll();

            IList<Win32DiskInfo> disks;
            if (diskNumber.HasValue)
            {
                // An explicit disk always wins over the detection mode.
                disks = new List<Win32DiskInfo> { Win32DiskInfoReader.Read(diskNumber.Value) };
            }
            else
            {
                disks = FilterByDetectionMode(Win32DiskInfoReader.EnumerateAll(), volumes, detectionMode);
            }

            var result = new List<RecoveryPartitionInfo>();
            foreach (var disk in disks)
            {
                foreach (var part in disk.Partitions)
                {
                    var vol = Win32VolumeMapper.FindForPartition(volumes, part.DiskNumber, part.StartingOffset);
                    var info = PartitionMapper.FromNative(part, vol);
                    if (!recoveryOnly || info.IsRecoveryPartition) { result.Add(info); }
                }
            }
            return result;
        }

        private static IList<Win32DiskInfo> FilterByDetectionMode(
            IList<Win32DiskInfo> disks, IList<Win32VolumeInfo> volumes, RecoveryPartitionDetectionMode mode)
        {
            if (mode == RecoveryPartitionDetectionMode.AllDisks) { return disks; }

            var osDisk = ResolveCurrentOsDiskNumber(volumes);
            if (osDisk == null)
            {
                throw new InvalidOperationException(
                    "Could not determine the current OS disk for -DetectionMode " + mode +
                    ". Specify -DiskNumber explicitly, or use -DetectionMode AllDisks.");
            }
            return mode == RecoveryPartitionDetectionMode.CurrentOSDisk
                ? disks.Where(d => d.DiskNumber == osDisk.Value).ToList()
                : disks.Where(d => d.DiskNumber != osDisk.Value).ToList();
        }

        /// <summary>
        /// Disk number backing the running Windows installation, resolved by
        /// mapping the system drive root (e.g. <c>C:\</c>) to the volume that
        /// carries it and reading that volume's first disk extent. Returns null
        /// when the mapping cannot be established.
        /// </summary>
        private static int? ResolveCurrentOsDiskNumber(IList<Win32VolumeInfo> volumes)
        {
            var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var sysRoot = string.IsNullOrEmpty(windir) ? null : Path.GetPathRoot(windir);
            if (string.IsNullOrEmpty(sysRoot)) { return null; }
            var target = sysRoot.TrimEnd('\\');

            foreach (var v in volumes)
            {
                if (v.Extents == null || v.Extents.Count == 0 || v.MountPoints == null) { continue; }
                foreach (var mp in v.MountPoints)
                {
                    if (!string.IsNullOrEmpty(mp) &&
                        string.Equals(mp.TrimEnd('\\'), target, StringComparison.OrdinalIgnoreCase))
                    {
                        return v.Extents[0].DiskNumber;
                    }
                }
            }
            return null;
        }

        public RecoveryPartitionInfo Create(int diskNumber, long sizeBytes, string label, string fileSystem, RecoveryPartitionCreationMode creationMode)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var gptType = new Guid(RecoveryPartitionConstants.GptTypeRecovery.Trim('{', '}'));
            var finalAttributes = NativeConstants.GPT_ATTRIBUTE_PLATFORM_REQUIRED
                                  | NativeConstants.GPT_BASIC_DATA_ATTRIBUTE_NO_DRIVE_LETTER
                                  | NativeConstants.GPT_BASIC_DATA_ATTRIBUTE_HIDDEN;
            var resolvedLabel = string.IsNullOrEmpty(label) ? RecoveryPartitionConstants.DefaultLabel : label;
            var resolvedFs    = string.IsNullOrEmpty(fileSystem) ? "NTFS" : fileSystem;

            // Step 0: apply the placement strategy. The partition is always created
            // after the existing partitions; this only decides how trailing free
            // space is obtained (append into it, shrink the last partition to make
            // it, or require an empty disk).
            ApplyCreationMode(disk, sizeBytes, creationMode);

            // Step 1: create as a plain Basic Data partition with no special
            // attributes so the Windows volume manager surfaces a mountable
            // volume root we can format. The recovery GPT type GUID and the
            // full attribute mask (PLATFORM_REQUIRED | HIDDEN | NO_DRIVE_LETTER)
            // are stamped in step 5 once the file system exists. This mirrors
            // the standard diskpart WinRE recipe (create -> format -> set id ->
            // gpt attributes) and avoids volume manager skipping the partition
            // because of a non-basic type.
            var basicDataType = new Guid(NativeConstants.GPT_PARTITION_TYPE_BASIC_DATA);
            var initialGptType = disk.PartitionStyle == PartitionStyle.Gpt ? basicDataType : Guid.Empty;
            var created = Win32PartitionWriter.Create(
                diskNumber, sizeBytes, initialGptType,
                NativeConstants.PARTITION_IFS,
                0UL,
                resolvedLabel);
            if (created == null)
            {
                throw new InvalidOperationException(
                    "Created partition on disk " + diskNumber + " but the layout re-read returned no matching entry.");
            }

            // Step 2: wait for the volume manager to register a volume for the
            // new partition before we hand it to fmifs.
            var volume = WaitForVolume(diskNumber, created.StartingOffset, TimeSpan.FromSeconds(30));
            if (volume == null)
            {
                throw new InvalidOperationException(
                    "Created partition " + created.PartitionNumber + " on disk " + diskNumber +
                    " but no Windows volume surfaced within the polling window; cannot format.");
            }

            // Step 3: format via fmifs!FormatEx against the kernel volume root.
            Win32VolumeFormatter.Format(
                EnsureTrailingSlash(volume.VolumeName), resolvedFs, resolvedLabel, quickFormat: true);

            // Image/boot payloads are staged separately (Set-WindowsRecoveryImage,
            // New-WindowsRecoveryBootEntry) so this cmdlet only creates and formats
            // the partition.

            // Step 5: strip any drive-letter mount the volume manager auto-
            // assigned while the partition was a plain Basic Data entry. The
            // final recovery attribute set carries NO_DRIVE_LETTER, but that
            // only governs future auto-mount decisions; an already-assigned
            // letter persists in the mountmgr database until we delete it.
            StripDriveLetters(diskNumber, created.StartingOffset);

            // Step 6: stamp the final recovery attribute set (or MBR 0x27).
            if (disk.PartitionStyle == PartitionStyle.Gpt)
            {
                Win32PartitionWriter.SetGptAttributes(
                    diskNumber, created.PartitionNumber, finalAttributes,
                    newGptType: gptType, newGptName: resolvedLabel);
            }
            else if (disk.PartitionStyle == PartitionStyle.Mbr)
            {
                TrySetMbrRecoveryType(diskNumber, created.PartitionNumber);
            }

            return ReadInfo(diskNumber, created.PartitionNumber) ?? PartitionMapper.FromNative(created, volume);
        }

        private static void StripDriveLetters(int diskNumber, long startingOffset)
        {
            try
            {
                var vol = Win32VolumeMapper.FindForPartition(
                    Win32VolumeMapper.EnumerateAll(), diskNumber, startingOffset);
                if (vol == null || vol.MountPoints == null) { return; }
                foreach (var mp in vol.MountPoints)
                {
                    if (string.IsNullOrEmpty(mp)) { continue; }
                    if (mp.Length == 3 && mp[1] == ':' && (mp[2] == '\\' || mp[2] == '/'))
                    {
                        try { NativeMethods.DeleteVolumeMountPointW(mp); } catch { /* best effort */ }
                    }
                }
            }
            catch { /* best effort - the GPT NO_DRIVE_LETTER attr is still applied below */ }
        }

        public RecoveryPartitionInfo Resize(int diskNumber, int partitionNumber, long newSizeBytes)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var part = disk.Partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber);
            if (part == null)
            {
                throw new InvalidOperationException(
                    "Partition " + partitionNumber + " was not found on disk " + diskNumber + ".");
            }

            // For GPT recovery partitions the volume manager does not surface a
            // \\?\Volume{guid} device, so FSCTL_EXTEND_VOLUME / FSCTL_SHRINK_VOLUME
            // cannot reach the file system. Temporarily flip the partition back
            // to Basic Data with cleared attributes, wait for the volume to
            // appear, perform the resize, then restore the recovery identity.
            // This mirrors the diskpart flow for hidden recovery partitions.
            var basicDataType = new Guid(NativeConstants.GPT_PARTITION_TYPE_BASIC_DATA);
            var needsUnhide = disk.PartitionStyle == PartitionStyle.Gpt
                              && part.GptType != Guid.Empty
                              && part.GptType != basicDataType;
            var originalGptType = part.GptType;
            var originalAttributes = part.GptAttributes;
            var originalName = part.GptName;

            if (needsUnhide)
            {
                Win32PartitionWriter.SetGptAttributes(
                    diskNumber, partitionNumber, 0UL,
                    newGptType: basicDataType, newGptName: originalName);
                // Wait not just for the volume GUID to register, but for a
                // file system (NTFS) to actually attach above it -- otherwise
                // FSCTL_EXTEND_VOLUME / FSCTL_SHRINK_VOLUME come back with
                // ERROR_INVALID_FUNCTION because no FS driver is sitting on
                // the volume device yet.
                var surfaced = WaitForVolume(
                    diskNumber, part.StartingOffset, TimeSpan.FromSeconds(30),
                    requireMountedFileSystem: true);
                if ((surfaced == null || string.IsNullOrEmpty(surfaced.FileSystem)) && _owner != null)
                {
                    _owner.WriteWarning(
                        "Disk " + diskNumber + " partition " + partitionNumber +
                        ": no Windows volume surfaced after un-hiding for resize; FSCTL may fail.");
                }
            }

            try
            {
                Win32PartitionWriter.Resize(diskNumber, partitionNumber, newSizeBytes);
            }
            finally
            {
                if (needsUnhide)
                {
                    // Restore the original recovery identity even if the resize
                    // threw, so the partition does not remain exposed as Basic
                    // Data on disk.
                    Win32PartitionWriter.SetGptAttributes(
                        diskNumber, partitionNumber, originalAttributes,
                        newGptType: originalGptType, newGptName: originalName);
                    StripDriveLetters(diskNumber, part.StartingOffset);
                }
            }

            var refreshed = Win32DiskInfoReader.Read(diskNumber);
            var resized = refreshed.Partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber);
            if (resized == null) { return ReadInfo(diskNumber, partitionNumber); }
            var vol = Win32VolumeMapper.FindForPartition(
                Win32VolumeMapper.EnumerateAll(), diskNumber, resized.StartingOffset);
            return PartitionMapper.FromNative(resized, vol);
        }

        public void Remove(int diskNumber, int partitionNumber)
        {
            // Pre-flight: best-effort detach of any mount points so the volume
            // manager releases the partition before we rewrite the layout.
            try
            {
                var disk = Win32DiskInfoReader.Read(diskNumber);
                var part = disk.Partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber);
                if (part != null)
                {
                    var vol = Win32VolumeMapper.FindForPartition(
                        Win32VolumeMapper.EnumerateAll(), diskNumber, part.StartingOffset);
                    if (vol != null && vol.MountPoints != null)
                    {
                        foreach (var mp in vol.MountPoints)
                        {
                            if (string.IsNullOrEmpty(mp)) { continue; }
                            try { NativeMethods.DeleteVolumeMountPointW(mp); } catch { /* best effort */ }
                        }
                    }
                }
            }
            catch { /* best effort */ }

            Win32PartitionWriter.Remove(diskNumber, partitionNumber);
        }

        public RecoveryPartitionInfo SetMetadata(int diskNumber, int partitionNumber, string label, bool? noDefaultDriveLetter, bool? isHidden)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var part = disk.Partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber);
            if (part == null)
            {
                throw new InvalidOperationException(
                    "Partition " + partitionNumber + " was not found on disk " + diskNumber + ".");
            }

            if (disk.PartitionStyle == PartitionStyle.Gpt && (noDefaultDriveLetter.HasValue || isHidden.HasValue))
            {
                var attrs = part.GptAttributes;
                if (noDefaultDriveLetter.HasValue)
                {
                    attrs = noDefaultDriveLetter.Value
                        ? attrs | NativeConstants.GPT_BASIC_DATA_ATTRIBUTE_NO_DRIVE_LETTER
                        : attrs & ~NativeConstants.GPT_BASIC_DATA_ATTRIBUTE_NO_DRIVE_LETTER;
                }
                if (isHidden.HasValue)
                {
                    attrs = isHidden.Value
                        ? attrs | NativeConstants.GPT_BASIC_DATA_ATTRIBUTE_HIDDEN
                        : attrs & ~NativeConstants.GPT_BASIC_DATA_ATTRIBUTE_HIDDEN;
                }
                Win32PartitionWriter.SetGptAttributes(diskNumber, partitionNumber, attrs);
            }
            else if (disk.PartitionStyle == PartitionStyle.Mbr
                && (noDefaultDriveLetter.HasValue || isHidden.HasValue)
                && _owner != null)
            {
                _owner.WriteWarning(
                    "MBR disk " + diskNumber + ": IsHidden / NoDefaultDriveLetter are GPT-only attributes; " +
                    "ignored on this disk. Use the MBR partition type byte (0x27) to mark a recovery partition.");
            }

            if (!string.IsNullOrEmpty(label))
            {
                var vol = Win32VolumeMapper.FindForPartition(
                    Win32VolumeMapper.EnumerateAll(), diskNumber, part.StartingOffset);
                if (vol != null && !string.IsNullOrEmpty(vol.VolumeName))
                {
                    if (!NativeMethods.SetVolumeLabelW(EnsureTrailingSlash(vol.VolumeName), label))
                    {
                        throw new Win32Exception(
                            Marshal.GetLastWin32Error(),
                            "SetVolumeLabelW failed for " + vol.VolumeName + ".");
                    }
                }
            }

            return ReadInfo(diskNumber, partitionNumber) ?? PartitionMapper.FromNative(part, null);
        }

        public RecoveryPartitionMountResult AddAccessPath(int diskNumber, int partitionNumber, string accessPath)
        {
            var vol = ResolveVolume(diskNumber, partitionNumber);
            EnsureDirectoryForMountPoint(accessPath);
            if (!NativeMethods.SetVolumeMountPointW(EnsureTrailingSlash(accessPath), EnsureTrailingSlash(vol.VolumeName)))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "SetVolumeMountPointW failed for " + accessPath + " -> " + vol.VolumeName + ".");
            }
            return new RecoveryPartitionMountResult
            {
                AccessPath = accessPath,
                Mounted = true,
                Changed = true,
                TimestampUtc = DateTimeOffset.UtcNow,
                ExecutionMethod = RecoveryExecutionMethod.Native,
                Partition = Get(diskNumber, recoveryOnly: false).FirstOrDefault(p => p.PartitionNumber == partitionNumber)
            };
        }

        public RecoveryPartitionMountResult RemoveAccessPath(int diskNumber, int partitionNumber, string accessPath)
        {
            if (!NativeMethods.DeleteVolumeMountPointW(EnsureTrailingSlash(accessPath)))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "DeleteVolumeMountPointW failed for " + accessPath + ".");
            }
            return new RecoveryPartitionMountResult
            {
                AccessPath = accessPath,
                Mounted = false,
                Changed = true,
                TimestampUtc = DateTimeOffset.UtcNow,
                ExecutionMethod = RecoveryExecutionMethod.Native,
                Partition = Get(diskNumber, recoveryOnly: false).FirstOrDefault(p => p.PartitionNumber == partitionNumber)
            };
        }

        private const long PlacementAlignmentBytes = 1024L * 1024L; // 1 MiB

        /// <summary>
        /// Applies the requested placement strategy before the partition is
        /// created. A recovery partition is always appended after the existing
        /// partitions (never before the OS, which would require moving the OS
        /// partition), so this only ensures there is enough trailing free space:
        /// verify it, refuse a non-empty disk, or shrink the last partition to
        /// create room.
        /// </summary>
        private static void ApplyCreationMode(Win32DiskInfo disk, long requestedSizeBytes, RecoveryPartitionCreationMode mode)
        {
            var activeParts = disk.Partitions
                .Where(p => p.LengthBytes > 0)
                .OrderBy(p => p.StartingOffset)
                .ToList();

            if (mode == RecoveryPartitionCreationMode.RequireEmptyDisk)
            {
                if (activeParts.Count > 0)
                {
                    throw new InvalidOperationException(
                        "Disk " + disk.DiskNumber + " already has " + activeParts.Count +
                        " partition(s); -CreationMode RequireEmptyDisk will not create a recovery partition on a non-empty disk.");
                }
                return;
            }

            long minOffset = disk.PartitionStyle == PartitionStyle.Gpt
                ? Math.Max(disk.GptStartingUsableOffset, PlacementAlignmentBytes)
                : PlacementAlignmentBytes;
            long maxUsable = disk.PartitionStyle == PartitionStyle.Gpt
                ? disk.GptStartingUsableOffset + disk.GptUsableLength
                : disk.SizeBytes;

            long tail = minOffset;
            foreach (var p in activeParts) { tail = Math.Max(tail, p.StartingOffset + p.LengthBytes); }
            long alignedTail = AlignUp1MiB(tail);
            long trailingFree = Math.Max(0, maxUsable - alignedTail);

            long needed = AlignDown1MiB(requestedSizeBytes);
            if (needed <= 0) { needed = PlacementAlignmentBytes; }

            if (trailingFree >= needed) { return; } // enough room already

            if (mode == RecoveryPartitionCreationMode.UseTrailingFreeSpace)
            {
                throw new InvalidOperationException(
                    "Disk " + disk.DiskNumber + " has " + trailingFree + " bytes of trailing free space but " +
                    needed + " bytes are required. Free space at the end of the disk (for example shrink the OS " +
                    "partition), or pass -CreationMode ShrinkToFit to shrink the last partition automatically.");
            }

            // ShrinkToFit: shrink the partition at the tail of the disk by the
            // shortfall so the freed space is contiguous with any existing
            // trailing free space, then the create step appends into it.
            if (activeParts.Count == 0)
            {
                throw new InvalidOperationException(
                    "Requested recovery partition size (" + requestedSizeBytes + " bytes) exceeds the usable space on disk " +
                    disk.DiskNumber + ".");
            }

            var last = activeParts.OrderBy(p => p.StartingOffset + p.LengthBytes).Last();
            long deficit = needed - trailingFree;
            long newLastSize = AlignDown1MiB(last.LengthBytes - deficit);
            if (newLastSize <= PlacementAlignmentBytes)
            {
                throw new InvalidOperationException(
                    "Cannot free " + deficit + " bytes by shrinking partition " + last.PartitionNumber +
                    " on disk " + disk.DiskNumber + "; it is too small. Free space manually or request a smaller size.");
            }

            Win32PartitionWriter.Shrink(disk.DiskNumber, last.PartitionNumber, newLastSize);
        }

        private static long AlignUp1MiB(long value)
        {
            var rem = value % PlacementAlignmentBytes;
            return rem == 0 ? value : value + (PlacementAlignmentBytes - rem);
        }

        private static long AlignDown1MiB(long value)
        {
            return value - (value % PlacementAlignmentBytes);
        }

        private void TrySetMbrRecoveryType(int diskNumber, int partitionNumber)
        {
            try
            {
                Win32PartitionWriter.SetMbrType(diskNumber, partitionNumber, NativeConstants.PARTITION_RECOVERY_MBR);
            }
            catch (Win32Exception ex) when (_owner != null)
            {
                _owner.WriteWarning(
                    "Native IOCTL_DISK_SET_DRIVE_LAYOUT_EX rejected the MBR 0x27 type byte (" + ex.Message +
                    "); falling back to diskpart 'SET ID=27 OVERRIDE'.");
                var result = DiskpartMbrTypeSetter.Apply(_owner, diskNumber, partitionNumber, NativeConstants.PARTITION_RECOVERY_MBR);
                if (result == null || result.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "diskpart fallback failed to set MBR 0x27 on disk " + diskNumber +
                        " partition " + partitionNumber + ".");
                }
            }
        }

        private Win32VolumeInfo ResolveVolume(int diskNumber, int partitionNumber)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var part = disk.Partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber);
            if (part == null)
            {
                throw new InvalidOperationException(
                    "Partition " + partitionNumber + " was not found on disk " + diskNumber + ".");
            }
            var vol = Win32VolumeMapper.FindForPartition(
                Win32VolumeMapper.EnumerateAll(), diskNumber, part.StartingOffset);
            if (vol == null)
            {
                throw new InvalidOperationException(
                    "No Windows volume is currently associated with disk " + diskNumber +
                    " partition " + partitionNumber + ".");
            }
            return vol;
        }

        private static Win32VolumeInfo WaitForVolume(int diskNumber, long startingOffset, TimeSpan timeout)
        {
            return WaitForVolume(diskNumber, startingOffset, timeout, requireMountedFileSystem: false);
        }

        private static Win32VolumeInfo WaitForVolume(
            int diskNumber, long startingOffset, TimeSpan timeout, bool requireMountedFileSystem)
        {
            var deadline = DateTime.UtcNow + timeout;
            Win32VolumeInfo last = null;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var vol = Win32VolumeMapper.FindForPartition(
                        Win32VolumeMapper.EnumerateAll(), diskNumber, startingOffset);
                    if (vol != null)
                    {
                        last = vol;
                        if (!requireMountedFileSystem || !string.IsNullOrEmpty(vol.FileSystem))
                        {
                            return vol;
                        }
                    }
                }
                catch (Win32Exception) { /* volume manager momentarily unavailable */ }
                Thread.Sleep(250);
            }
            return last;
        }

        private RecoveryPartitionInfo ReadInfo(int diskNumber, int partitionNumber)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var part = disk.Partitions.FirstOrDefault(p => p.PartitionNumber == partitionNumber);
            if (part == null) { return null; }
            var vol = Win32VolumeMapper.FindForPartition(
                Win32VolumeMapper.EnumerateAll(), diskNumber, part.StartingOffset);
            return PartitionMapper.FromNative(part, vol);
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path)) { return path; }
            return path.EndsWith("\\", StringComparison.Ordinal) ? path : path + "\\";
        }

        private static void EnsureDirectoryForMountPoint(string accessPath)
        {
            // Drive-letter access paths ("X:\") are not real directories; only
            // junction-style mount points need a host directory to exist.
            if (string.IsNullOrEmpty(accessPath)) { return; }
            if (accessPath.Length <= 3 && accessPath.Length >= 2 && accessPath[1] == ':') { return; }
            try { Directory.CreateDirectory(accessPath); } catch { /* best effort */ }
        }
    }
}
