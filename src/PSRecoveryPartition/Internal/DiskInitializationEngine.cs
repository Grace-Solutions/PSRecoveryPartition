using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using PSRecoveryPartition.Native;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Repartitions and formats a whole disk from a resolved layout, entirely via
    /// native IOCTLs and <c>fmifs!FormatEx</c> -- no diskpart or format.com.
    ///
    /// <para>The partitions are created with ordinary basic-data / IFS types so the
    /// volume manager surfaces a formattable volume for each. Only after every
    /// volume is formatted are the EFI and Recovery partitions re-tagged with their
    /// real type GUIDs (and the recovery attribute mask), which is the same
    /// ordering diskpart-driven deployment scripts rely on: a partition already
    /// carrying the recovery type or the no-drive-letter attribute will not mount,
    /// and therefore cannot be formatted.</para>
    /// </summary>
    internal sealed class DiskInitializationEngine
    {
        private static readonly Guid GptTypeBasicData = new Guid(NativeConstants.GPT_PARTITION_TYPE_BASIC_DATA);
        private static readonly Guid GptTypeEfiSystem = new Guid(NativeConstants.GPT_PARTITION_TYPE_EFI_SYSTEM);
        private static readonly Guid GptTypeMsr       = new Guid(NativeConstants.GPT_PARTITION_TYPE_MICROSOFT_RESERVED);
        private static readonly Guid GptTypeRecovery  = new Guid(NativeConstants.GPT_PARTITION_TYPE_MICROSOFT_RECOVERY);

        // PLATFORM_REQUIRED | NO_DRIVE_LETTER -- the canonical Microsoft recovery mask.
        private const ulong RecoveryGptAttributes =
            NativeConstants.GPT_ATTRIBUTE_PLATFORM_REQUIRED |
            NativeConstants.GPT_BASIC_DATA_ATTRIBUTE_NO_DRIVE_LETTER;

        private readonly PSCmdlet _owner;

        public DiskInitializationEngine(PSCmdlet owner) { _owner = owner; }

        public DiskInitializationResult Initialize(
            int diskNumber, DiskPartitionScheme scheme, IList<DiskPartitionSpec> specs)
        {
            GuardNotOsDisk(diskNumber);

            var style = scheme == DiskPartitionScheme.Gpt ? PartitionStyle.Gpt : PartitionStyle.Mbr;

            Progress("Preparing disk", "Dismounting existing volumes", 5);
            Verbose("Dismounting volumes backed by disk " + diskNumber + ".");
            Win32PartitionWriter.DismountVolumesOnDisk(diskNumber, Verbose);

            Progress("Preparing disk", "Clearing the existing partition table", 15);
            Verbose("Clearing the partition table on disk " + diskNumber + ".");
            Win32PartitionWriter.DeleteDriveLayout(diskNumber);

            Progress("Preparing disk", "Writing a fresh " + scheme + " partition table", 25);
            Verbose("Writing a fresh " + scheme + " partition table on disk " + diskNumber + ".");
            Win32PartitionWriter.CreateDisk(diskNumber, style);

            var disk = Win32DiskInfoReader.Read(diskNumber);
            var planned = DiskLayoutPlanner.Resolve(disk, scheme, specs);
            foreach (var p in planned)
            {
                Verbose("Planned partition " + p.PartitionNumber + ": " + p.Spec.Label +
                    " offset=" + p.StartingOffset + " length=" + p.LengthBytes + " (" + p.Spec.Kind + ").");
            }

            Progress("Creating partitions", "Writing the drive layout", 40);
            Win32PartitionWriter.ApplyFullLayout(diskNumber, BuildEntries(style, planned));

            var step = 0;
            foreach (var p in planned)
            {
                step++;
                if (p.Spec.Kind == DiskPartitionKind.Msr)
                {
                    Verbose("Skipping format of '" + p.Spec.Label + "' (Microsoft Reserved partitions carry no file system).");
                    continue;
                }
                Progress("Formatting", "Formatting " + p.Spec.Label, 40 + (40 * step / Math.Max(1, planned.Count)));
                FormatPartition(diskNumber, p);
            }

            Progress("Finalizing", "Applying partition types and attributes", 90);
            foreach (var p in planned) { ApplyFinalType(diskNumber, style, p); }

            Progress("Finalizing", "Refreshing disk", 100, completed: true);
            return BuildResult(diskNumber, scheme);
        }

        // --- Phase 1: entries that will mount and format ---
        private static IList<PARTITION_INFORMATION_EX> BuildEntries(PartitionStyle style, IList<PlannedPartition> planned)
        {
            var entries = new List<PARTITION_INFORMATION_EX>(planned.Count);
            foreach (var p in planned)
            {
                var entry = new PARTITION_INFORMATION_EX
                {
                    PartitionStyle = style,
                    StartingOffset = p.StartingOffset,
                    PartitionLength = p.LengthBytes,
                    PartitionNumber = p.PartitionNumber,
                    RewritePartition = true,
                };

                if (style == PartitionStyle.Gpt)
                {
                    entry.Gpt = new PARTITION_INFORMATION_GPT
                    {
                        // MSR is never formatted, so it can carry its real type up
                        // front. Everything else starts as basic data so it mounts.
                        PartitionType = p.Spec.Kind == DiskPartitionKind.Msr ? GptTypeMsr : GptTypeBasicData,
                        PartitionId = Guid.NewGuid(),
                        Attributes = 0,
                        Name = PARTITION_GPT_NAME.FromString(p.Spec.Label),
                    };
                }
                else
                {
                    entry.Mbr = new PARTITION_INFORMATION_MBR
                    {
                        PartitionType = NativeConstants.PARTITION_IFS,
                        // On MBR the "EFI" slot becomes the active system partition.
                        BootIndicator = p.Spec.Kind == DiskPartitionKind.Efi,
                        RecognizedPartition = true,
                        HiddenSectors = (uint)(p.StartingOffset / 512L),
                    };
                }
                entries.Add(entry);
            }
            return entries;
        }

        // --- Phase 2: format via fmifs ---
        private void FormatPartition(int diskNumber, PlannedPartition p)
        {
            var volumeRoot = WaitForVolume(diskNumber, p.StartingOffset, TimeSpan.FromSeconds(30));
            if (volumeRoot == null)
            {
                throw new InvalidOperationException(
                    "No volume appeared for partition " + p.PartitionNumber + " ('" + p.Spec.Label +
                    "') on disk " + diskNumber + "; cannot format it.");
            }

            var fs = ResolveFileSystem(p.Spec);
            Verbose("Formatting '" + p.Spec.Label + "' (" + volumeRoot + ") as " + fs + ".");
            Win32VolumeFormatter.Format(volumeRoot, fs, p.Spec.Label, quickFormat: true);
        }

        private static string ResolveFileSystem(DiskPartitionSpec spec)
        {
            if (!string.IsNullOrEmpty(spec.FileSystem)) { return spec.FileSystem; }
            return spec.Kind == DiskPartitionKind.Efi ? "FAT32" : "NTFS";
        }

        private static string WaitForVolume(int diskNumber, long startingOffset, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var vol = Win32VolumeMapper.FindForPartition(
                        Win32VolumeMapper.EnumerateAll(), diskNumber, startingOffset);
                    if (vol != null && !string.IsNullOrEmpty(vol.VolumeName))
                    {
                        return vol.VolumeName.EndsWith("\\", StringComparison.Ordinal)
                            ? vol.VolumeName : vol.VolumeName + "\\";
                    }
                }
                catch { /* volume manager still settling */ }
                Thread.Sleep(250);
            }
            return null;
        }

        // --- Phase 3: real partition types + attributes, after formatting ---
        private void ApplyFinalType(int diskNumber, PartitionStyle style, PlannedPartition p)
        {
            var spec = p.Spec;
            if (style == PartitionStyle.Gpt)
            {
                Guid? finalType = null;
                ulong attributes = 0;

                switch (spec.Kind)
                {
                    case DiskPartitionKind.Efi:
                        finalType = GptTypeEfiSystem;
                        break;
                    case DiskPartitionKind.Recovery:
                        finalType = GptTypeRecovery;
                        attributes = RecoveryGptAttributes;
                        break;
                    case DiskPartitionKind.Msr:
                    case DiskPartitionKind.Basic:
                        break;
                }
                if (spec.HasExplicitGptAttributes) { attributes = spec.GptAttributes; }

                if (finalType.HasValue || attributes != 0)
                {
                    Verbose("Tagging '" + spec.Label + "' with type " +
                        (finalType.HasValue ? finalType.Value.ToString("B") : "(unchanged)") +
                        " attributes 0x" + attributes.ToString("X16") + ".");
                    Win32PartitionWriter.SetGptAttributes(
                        diskNumber, p.PartitionNumber, attributes, finalType, spec.Label);
                }
            }
            else if (spec.Kind == DiskPartitionKind.Recovery)
            {
                Verbose("Setting MBR type 0x27 on '" + spec.Label + "'.");
                Win32PartitionWriter.SetMbrType(diskNumber, p.PartitionNumber, NativeConstants.PARTITION_RECOVERY_MBR);
            }
        }

        // --- Guards ---
        private static void GuardNotOsDisk(int diskNumber)
        {
            string systemRoot;
            try { systemRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)); }
            catch { return; }
            if (string.IsNullOrEmpty(systemRoot)) { return; }

            IList<Win32VolumeInfo> volumes;
            try { volumes = Win32VolumeMapper.EnumerateAll(); }
            catch { return; }

            foreach (var vol in volumes)
            {
                if (vol.MountPoints == null || vol.Extents == null) { continue; }
                var hostsOs = vol.MountPoints.Any(mp =>
                    !string.IsNullOrEmpty(mp) &&
                    string.Equals(mp.TrimEnd('\\'), systemRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
                if (!hostsOs) { continue; }
                if (vol.Extents.Any(e => e.DiskNumber == diskNumber))
                {
                    throw new InvalidOperationException(
                        "Disk " + diskNumber + " hosts the running operating system (" + systemRoot +
                        "). Refusing to repartition it. Run this from Windows PE, or target a different disk.");
                }
            }
        }

        // --- Result ---
        private static DiskInitializationResult BuildResult(int diskNumber, DiskPartitionScheme scheme)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            var volumes = SafeEnumerateVolumes();
            var result = new DiskInitializationResult
            {
                DiskNumber = diskNumber,
                PartitionScheme = scheme,
                DiskSizeBytes = disk.SizeBytes,
                Changed = true,
                ExecutionMethod = RecoveryExecutionMethod.Native,
            };
            foreach (var part in disk.Partitions.OrderBy(p => p.StartingOffset))
            {
                var vol = Win32VolumeMapper.FindForPartition(volumes, diskNumber, part.StartingOffset);
                var info = PartitionMapper.FromNative(part, vol);
                if (info != null) { result.Partitions.Add(info); }
            }
            return result;
        }

        private static IList<Win32VolumeInfo> SafeEnumerateVolumes()
        {
            try { return Win32VolumeMapper.EnumerateAll(); }
            catch { return new List<Win32VolumeInfo>(); }
        }

        // --- Reporting ---
        private void Verbose(string message)
        {
            if (_owner != null) { _owner.WriteVerbose(message); }
        }

        private void Progress(string activity, string status, int percent, bool completed = false)
        {
            if (_owner == null) { return; }
            var record = new ProgressRecord(2, activity, status)
            {
                PercentComplete = Math.Min(100, Math.Max(0, percent)),
            };
            if (completed) { record.RecordType = ProgressRecordType.Completed; }
            _owner.WriteProgress(record);
        }
    }
}
