using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Helpers for translating Storage module CIM partition / volume objects
    /// into <see cref="RecoveryPartitionInfo"/> instances.
    /// </summary>
    internal static class PartitionMapper
    {
        public static RecoveryPartitionInfo FromPartition(PSObject partition, PSObject volume = null)
        {
            if (partition == null) { return null; }
            var info = new RecoveryPartitionInfo
            {
                DiscoveredAtUtc = DateTimeOffset.UtcNow,
                ExecutionMethod = RecoveryExecutionMethod.Storage,
                DiskNumber          = GetInt(partition, "DiskNumber"),
                PartitionNumber     = GetInt(partition, "PartitionNumber"),
                DiskPath            = GetString(partition, "DiskPath"),
                PartitionUniqueId   = GetString(partition, "UniqueId"),
                Guid                = GetString(partition, "Guid"),
                GptType             = GetString(partition, "GptType"),
                MbrType             = GetString(partition, "MbrType"),
                Offset              = GetLong(partition, "Offset"),
                SizeBytes           = GetLong(partition, "Size"),
                DriveLetter         = GetString(partition, "DriveLetter"),
                IsHidden            = GetBool(partition, "IsHidden"),
                NoDefaultDriveLetter = GetBool(partition, "NoDefaultDriveLetter")
            };

            var accessPaths = partition.Properties["AccessPaths"];
            if (accessPaths != null && accessPaths.Value is IEnumerable<object>)
            {
                info.AccessPaths = ((IEnumerable<object>)accessPaths.Value)
                    .Select(o => o == null ? null : o.ToString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
            }
            else if (accessPaths != null && accessPaths.Value is object[])
            {
                info.AccessPaths = ((object[])accessPaths.Value)
                    .Select(o => o == null ? null : o.ToString())
                    .ToArray();
            }

            if (volume != null)
            {
                info.Label = GetString(volume, "FileSystemLabel");
                info.FileSystem = GetString(volume, "FileSystem");
            }

            info.IsRecoveryPartition = IsRecoveryGptType(info.GptType)
                || info.MbrType == "Recovery"
                || string.Equals(info.Label, RecoveryPartitionConstants.DefaultLabel, StringComparison.OrdinalIgnoreCase);

            return info;
        }

        public static bool IsRecoveryGptType(string gptType)
        {
            if (string.IsNullOrEmpty(gptType)) { return false; }
            return string.Equals(gptType.Trim('{', '}'),
                RecoveryPartitionConstants.GptTypeRecovery.Trim('{', '}'),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string GetString(PSObject obj, string name)
        {
            var prop = obj.Properties[name];
            return prop == null || prop.Value == null ? null : prop.Value.ToString();
        }

        private static int GetInt(PSObject obj, string name)
        {
            var prop = obj.Properties[name];
            if (prop == null || prop.Value == null) { return 0; }
            try { return Convert.ToInt32(prop.Value); } catch { return 0; }
        }

        private static long GetLong(PSObject obj, string name)
        {
            var prop = obj.Properties[name];
            if (prop == null || prop.Value == null) { return 0L; }
            try { return Convert.ToInt64(prop.Value); } catch { return 0L; }
        }

        private static bool GetBool(PSObject obj, string name)
        {
            var prop = obj.Properties[name];
            if (prop == null || prop.Value == null) { return false; }
            try { return Convert.ToBoolean(prop.Value); } catch { return false; }
        }
    }
}
