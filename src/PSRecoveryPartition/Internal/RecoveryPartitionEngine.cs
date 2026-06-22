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

        public IList<RecoveryPartitionInfo> Get(int? diskNumber = null, bool recoveryOnly = true)
        {
            var disks = diskNumber.HasValue
                ? new List<Win32DiskInfo> { Win32DiskInfoReader.Read(diskNumber.Value) }
                : Win32DiskInfoReader.EnumerateAll();
            var volumes = Win32VolumeMapper.EnumerateAll();

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

        public RecoveryPartitionInfo Create(int diskNumber, long sizeBytes, string label, string fileSystem, FileInfo windowsREImagePath)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var gptType = new Guid(RecoveryPartitionConstants.GptTypeRecovery.Trim('{', '}'));
            var finalAttributes = NativeConstants.GPT_ATTRIBUTE_PLATFORM_REQUIRED
                                  | NativeConstants.GPT_BASIC_DATA_ATTRIBUTE_NO_DRIVE_LETTER
                                  | NativeConstants.GPT_BASIC_DATA_ATTRIBUTE_HIDDEN;
            var resolvedLabel = string.IsNullOrEmpty(label) ? RecoveryPartitionConstants.DefaultLabel : label;
            var resolvedFs    = string.IsNullOrEmpty(fileSystem) ? "NTFS" : fileSystem;

            // Step 1: create with PLATFORM_REQUIRED only so Windows surfaces a
            // mountable volume root we can format. The hidden / no-auto-mount
            // attributes are applied after format and image copy.
            var created = Win32PartitionWriter.Create(
                diskNumber, sizeBytes, gptType,
                NativeConstants.PARTITION_RECOVERY_MBR,
                NativeConstants.GPT_ATTRIBUTE_PLATFORM_REQUIRED,
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

            // Step 4: optional WindowsRE image copy through a temporary mount.
            if (windowsREImagePath != null && windowsREImagePath.Exists)
            {
                CopyImageOnto(volume.VolumeName, windowsREImagePath);
            }

            // Step 5: stamp the final recovery attribute set (or MBR 0x27).
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

        public RecoveryPartitionInfo Resize(int diskNumber, int partitionNumber, long newSizeBytes)
        {
            throw new NotImplementedException(
                "Resize-RecoveryPartition is pending the Phase 17d IOCTL_DISK_GROW_PARTITION + " +
                "FSCTL_EXTEND_VOLUME / FSCTL_SHRINK_VOLUME implementation.");
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

        private void CopyImageOnto(string volumeName, FileInfo image)
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "PSRecoveryPartition-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            var mounted = false;
            try
            {
                if (!NativeMethods.SetVolumeMountPointW(EnsureTrailingSlash(tempRoot), EnsureTrailingSlash(volumeName)))
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        "SetVolumeMountPointW failed for temp mount " + tempRoot + ".");
                }
                mounted = true;
                var dest = Path.Combine(tempRoot, "Recovery", "WindowsRE");
                Directory.CreateDirectory(dest);
                File.Copy(image.FullName, Path.Combine(dest, image.Name), true);
            }
            finally
            {
                if (mounted)
                {
                    try { NativeMethods.DeleteVolumeMountPointW(EnsureTrailingSlash(tempRoot)); } catch { /* best effort */ }
                }
                try { Directory.Delete(tempRoot, true); } catch { /* best effort */ }
            }
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
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var vol = Win32VolumeMapper.FindForPartition(
                        Win32VolumeMapper.EnumerateAll(), diskNumber, startingOffset);
                    if (vol != null) { return vol; }
                }
                catch (Win32Exception) { /* volume manager momentarily unavailable */ }
                Thread.Sleep(250);
            }
            return null;
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
