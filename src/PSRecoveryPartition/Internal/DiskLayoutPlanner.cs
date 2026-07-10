using System;
using System.Collections.Generic;
using PSRecoveryPartition.Native;

namespace PSRecoveryPartition
{
    /// <summary>
    /// A single partition resolved onto concrete disk coordinates.
    /// </summary>
    internal sealed class PlannedPartition
    {
        public DiskPartitionSpec Spec;
        public long StartingOffset;
        public long LengthBytes;
        public int PartitionNumber;
    }

    /// <summary>
    /// Expands layout presets into <see cref="DiskPartitionSpec"/> lists and
    /// resolves a spec list onto a specific disk: each entry is laid down in list
    /// order, 1 MiB aligned, taking either an absolute size or a percentage of the
    /// free space remaining at that point.
    /// </summary>
    internal static class DiskLayoutPlanner
    {
        private const long AlignmentBytes = 1024L * 1024L;   // 1 MiB
        private const long MinPartitionBytes = 1024L * 1024L;

        public static IList<DiskPartitionSpec> FromPreset(DiskPartitionLayoutPreset preset)
        {
            switch (preset)
            {
                case DiskPartitionLayoutPreset.RecoveryFirst:
                    return new List<DiskPartitionSpec>
                    {
                        DiskPartitionSpec.Efi(),
                        DiskPartitionSpec.Msr(),
                        new DiskPartitionSpec("RECOVERY", DiskPartitionSizeMode.Percentage, 20, DiskPartitionKind.Recovery),
                        new DiskPartitionSpec("OS", DiskPartitionSizeMode.Percentage, 100, DiskPartitionKind.Basic),
                    };

                case DiskPartitionLayoutPreset.RecoveryLast:
                    return new List<DiskPartitionSpec>
                    {
                        DiskPartitionSpec.Efi(),
                        DiskPartitionSpec.Msr(),
                        new DiskPartitionSpec("OS", DiskPartitionSizeMode.Percentage, 80, DiskPartitionKind.Basic),
                        new DiskPartitionSpec("RECOVERY", DiskPartitionSizeMode.Percentage, 100, DiskPartitionKind.Recovery),
                    };

                case DiskPartitionLayoutPreset.NoRecovery:
                    return new List<DiskPartitionSpec>
                    {
                        DiskPartitionSpec.Efi(),
                        DiskPartitionSpec.Msr(),
                        new DiskPartitionSpec("OS", DiskPartitionSizeMode.Percentage, 100, DiskPartitionKind.Basic),
                    };

                default:
                    throw new ArgumentOutOfRangeException("preset", preset, "Unknown layout preset.");
            }
        }

        /// <summary>
        /// Drops entries that cannot exist under the requested scheme (MSR is
        /// GPT-only) and enforces the scheme's structural limits.
        /// </summary>
        public static IList<DiskPartitionSpec> AdaptToScheme(
            IList<DiskPartitionSpec> specs, DiskPartitionScheme scheme, Action<string> warn)
        {
            var result = new List<DiskPartitionSpec>();
            foreach (var s in specs)
            {
                if (scheme == DiskPartitionScheme.Mbr && s.Kind == DiskPartitionKind.Msr)
                {
                    if (warn != null) { warn("Skipping '" + s.Label + "': a Microsoft Reserved partition is GPT-only and has no MBR equivalent."); }
                    continue;
                }
                result.Add(s);
            }
            if (result.Count == 0)
            {
                throw new InvalidOperationException("The resolved layout contains no partitions.");
            }
            if (scheme == DiskPartitionScheme.Mbr && result.Count > 4)
            {
                throw new InvalidOperationException(
                    "An MBR disk supports at most four primary partitions; the layout resolves to " + result.Count + ".");
            }
            return result;
        }

        /// <summary>
        /// Resolves offsets and lengths for <paramref name="specs"/> against
        /// <paramref name="disk"/>. Percentages are taken against the free space
        /// remaining at that point, so a trailing 100% entry consumes the rest of
        /// the disk. All boundaries are aligned down to 1 MiB.
        /// </summary>
        public static IList<PlannedPartition> Resolve(
            Win32DiskInfo disk, DiskPartitionScheme scheme, IList<DiskPartitionSpec> specs)
        {
            long usableStart;
            long usableEnd;
            if (scheme == DiskPartitionScheme.Gpt)
            {
                // After IOCTL_DISK_CREATE_DISK the GPT header reports the region
                // between the primary and backup partition tables.
                usableStart = Math.Max(disk.GptStartingUsableOffset, AlignmentBytes);
                usableEnd = disk.GptStartingUsableOffset + disk.GptUsableLength;
            }
            else
            {
                usableStart = AlignmentBytes;
                usableEnd = disk.SizeBytes;
            }

            var cursor = AlignUp(usableStart, AlignmentBytes);
            var planned = new List<PlannedPartition>(specs.Count);

            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                var remaining = usableEnd - cursor;
                if (remaining < MinPartitionBytes)
                {
                    throw new InvalidOperationException(
                        "Disk " + disk.DiskNumber + " ran out of space at '" + spec.Label + "' (entry " + (i + 1) +
                        " of " + specs.Count + "); " + Math.Max(0, remaining) + " bytes remain.");
                }

                long length = spec.SizeMode == DiskPartitionSizeMode.Percentage
                    ? (spec.Value >= 100 ? remaining : MulDiv(remaining, spec.Value, 100))
                    : spec.Value;

                length = AlignDown(length, AlignmentBytes);

                // The final entry of a layout that asks for everything should not
                // lose its tail to rounding.
                if (length > remaining) { length = AlignDown(remaining, AlignmentBytes); }

                if (length < MinPartitionBytes)
                {
                    throw new InvalidOperationException(
                        "Partition '" + spec.Label + "' resolves to " + length +
                        " bytes, which is below the 1 MiB minimum. Requested " +
                        (spec.SizeMode == DiskPartitionSizeMode.Percentage
                            ? spec.Value + "% of " + remaining + " remaining bytes"
                            : spec.Value + " bytes") + ".");
                }

                planned.Add(new PlannedPartition
                {
                    Spec = spec,
                    StartingOffset = cursor,
                    LengthBytes = length,
                    PartitionNumber = i + 1,
                });
                cursor += length;
            }

            return planned;
        }

        // (value * numerator) / denominator without overflowing a 64-bit disk size.
        private static long MulDiv(long value, long numerator, long denominator)
        {
            return (long)((decimal)value * numerator / denominator);
        }

        private static long AlignUp(long value, long alignment)
        {
            var rem = value % alignment;
            return rem == 0 ? value : value + (alignment - rem);
        }

        private static long AlignDown(long value, long alignment)
        {
            return value - (value % alignment);
        }
    }
}
