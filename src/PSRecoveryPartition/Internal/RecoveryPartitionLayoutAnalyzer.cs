using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Inspects on-disk partition geometry around a recovery partition (existing
    /// or proposed) and produces a <see cref="RecoveryPartitionLayoutAnalysis"/>.
    /// The analyzer is intentionally read-only; it never mutates layout.
    /// </summary>
    internal static class RecoveryPartitionLayoutAnalyzer
    {
        // EFI System Partition GPT type GUID.
        private const string GptTypeEsp = "c12a7328-f81f-11d2-ba4b-00a0c93ec93b";
        // Microsoft Reserved Partition GPT type GUID.
        private const string GptTypeMsr = "e3c9e316-0b5c-4db8-817d-f92df00215ae";
        // Basic Data Partition GPT type GUID (used by Windows OS volumes).
        private const string GptTypeBasicData = "ebd0a0a2-b9e5-4433-87c0-68b6b72699c7";

        public static RecoveryPartitionLayoutAnalysis Analyze(StorageInvoker storage, int diskNumber, int? partitionNumber, long proposedSizeBytes)
        {
            var analysis = new RecoveryPartitionLayoutAnalysis
            {
                DiskNumber = diskNumber,
                PartitionNumber = partitionNumber
            };

            var disk = storage.InvokeSingle("Get-Disk", new Hashtable { { "Number", diskNumber } });
            long diskSize = disk == null ? 0L : GetLong(disk, "Size");

            var partitions = storage.Invoke("Get-Partition", new Hashtable { { "DiskNumber", diskNumber } })
                .Where(p => GetLong(p, "Size") > 0)
                .OrderBy(p => GetLong(p, "Offset"))
                .ToList();

            var os = partitions.FirstOrDefault(IsOsPartition);
            if (os != null)
            {
                analysis.OsPartitionNumber = GetInt(os, "PartitionNumber");
                analysis.OsPartitionOffset = GetLong(os, "Offset");
                analysis.OsPartitionSizeBytes = GetLong(os, "Size");
            }

            PSObject self = null;
            if (partitionNumber.HasValue)
            {
                self = partitions.FirstOrDefault(p => GetInt(p, "PartitionNumber") == partitionNumber.Value);
            }

            long selfOffset = self != null ? GetLong(self, "Offset") : 0L;
            long selfSize   = self != null ? GetLong(self, "Size")   : proposedSizeBytes;
            long selfEnd    = selfOffset + selfSize;

            if (self == null && os == null)
            {
                analysis.Position = RecoveryPartitionLayoutPosition.Standalone;
            }
            else if (self != null && os != null)
            {
                analysis.Position = selfOffset == analysis.OsPartitionOffset
                    ? RecoveryPartitionLayoutPosition.SameAsOs
                    : (selfOffset < analysis.OsPartitionOffset
                        ? RecoveryPartitionLayoutPosition.BeforeOs
                        : RecoveryPartitionLayoutPosition.AfterOs);
            }
            else if (self == null)
            {
                analysis.Position = RecoveryPartitionLayoutPosition.Unknown;
            }
            else
            {
                analysis.Position = RecoveryPartitionLayoutPosition.Standalone;
            }

            if (self != null)
            {
                var preceding = partitions
                    .Where(p => GetLong(p, "Offset") + GetLong(p, "Size") <= selfOffset)
                    .OrderByDescending(p => GetLong(p, "Offset"))
                    .FirstOrDefault();
                if (preceding != null)
                {
                    analysis.PrecedingPartitionNumber = GetInt(preceding, "PartitionNumber");
                    analysis.PrecedingPartitionType   = DescribeType(preceding);
                    analysis.PrecedingPartitionIsOs   = IsOsPartition(preceding);
                    analysis.LeadingFreeSpaceBytes    = selfOffset - (GetLong(preceding, "Offset") + GetLong(preceding, "Size"));
                }
                else
                {
                    analysis.LeadingFreeSpaceBytes = selfOffset;
                }

                var following = partitions
                    .Where(p => GetLong(p, "Offset") >= selfEnd)
                    .OrderBy(p => GetLong(p, "Offset"))
                    .FirstOrDefault();
                if (following != null)
                {
                    analysis.FollowingPartitionNumber = GetInt(following, "PartitionNumber");
                    analysis.FollowingPartitionType   = DescribeType(following);
                    analysis.FollowingPartitionIsOs   = IsOsPartition(following);
                    analysis.TrailingFreeSpaceBytes   = GetLong(following, "Offset") - selfEnd;
                }
                else
                {
                    analysis.TrailingFreeSpaceBytes = Math.Max(0L, diskSize - selfEnd);
                }
            }

            var growBy = Math.Max(0L, proposedSizeBytes - selfSize);
            analysis.CanGrowInPlace  = self == null || analysis.TrailingFreeSpaceBytes >= growBy;
            analysis.CanShrinkInPlace = self == null || proposedSizeBytes <= selfSize;
            analysis.CanRemoveSafely = !analysis.FollowingPartitionIsOs;

            if (analysis.FollowingPartitionIsOs)
            {
                analysis.Warnings.Add(
                    "Recovery partition is immediately followed by the OS partition; growing or removing it cannot be done in place without first relocating the OS.");
            }
            if (self != null && proposedSizeBytes > selfSize && !analysis.CanGrowInPlace)
            {
                analysis.Warnings.Add(
                    "Requested size (" + proposedSizeBytes + " bytes) exceeds the available trailing free space (" + analysis.TrailingFreeSpaceBytes + " bytes); the resize would fail without shrinking a neighbour first.");
            }
            if (analysis.Position == RecoveryPartitionLayoutPosition.BeforeOs && self != null && proposedSizeBytes != selfSize)
            {
                analysis.Warnings.Add(
                    "Recovery partition is located before the OS partition; any size change will shift the OS partition offset and is not supported in place.");
            }

            return analysis;
        }

        private static bool IsOsPartition(PSObject partition)
        {
            if (partition == null) { return false; }
            if (GetBool(partition, "IsBoot")) { return true; }
            var driveLetter = GetString(partition, "DriveLetter");
            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            if (!string.IsNullOrEmpty(driveLetter) && !string.IsNullOrEmpty(systemDrive)
                && systemDrive.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private static string DescribeType(PSObject partition)
        {
            var gpt = NormalizeGuid(GetString(partition, "GptType"));
            if (!string.IsNullOrEmpty(gpt))
            {
                if (gpt.Equals(GptTypeEsp, StringComparison.OrdinalIgnoreCase)) { return "EFI System"; }
                if (gpt.Equals(GptTypeMsr, StringComparison.OrdinalIgnoreCase)) { return "Microsoft Reserved"; }
                if (gpt.Equals(GptTypeBasicData, StringComparison.OrdinalIgnoreCase)) { return "Basic Data"; }
                if (PartitionMapper.IsRecoveryGptType(gpt)) { return "Recovery"; }
                return gpt;
            }
            var mbr = GetString(partition, "MbrType");
            return string.IsNullOrEmpty(mbr) ? "Unknown" : mbr;
        }

        private static string NormalizeGuid(string value)
        {
            return value == null ? null : value.Trim('{', '}');
        }

        private static string GetString(PSObject o, string n)
        {
            var p = o.Properties[n]; return p == null || p.Value == null ? null : p.Value.ToString();
        }
        private static int GetInt(PSObject o, string n)
        {
            var p = o.Properties[n]; if (p == null || p.Value == null) { return 0; }
            try { return Convert.ToInt32(p.Value); } catch { return 0; }
        }
        private static long GetLong(PSObject o, string n)
        {
            var p = o.Properties[n]; if (p == null || p.Value == null) { return 0L; }
            try { return Convert.ToInt64(p.Value); } catch { return 0L; }
        }
        private static bool GetBool(PSObject o, string n)
        {
            var p = o.Properties[n]; if (p == null || p.Value == null) { return false; }
            try { return Convert.ToBoolean(p.Value); } catch { return false; }
        }
    }
}
