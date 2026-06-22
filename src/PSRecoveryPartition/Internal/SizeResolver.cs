using System;
using System.Collections;
using System.Management.Automation;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Resolves the three sizing parameter sets defined in the design spec:
    /// ExplicitSize (bytes), PercentSize (% of disk), DefaultSize (1 GiB).
    /// </summary>
    internal static class SizeResolver
    {
        public const string ParameterSetExplicitSize = "ExplicitSize";
        public const string ParameterSetPercentSize  = "PercentSize";
        public const string ParameterSetDefaultSize  = "DefaultSize";

        public static long Resolve(string parameterSetName, long? sizeBytes, int? sizePercent, long diskSizeBytes, out RecoveryPartitionSizingMode mode)
        {
            switch (parameterSetName)
            {
                case ParameterSetExplicitSize:
                    if (!sizeBytes.HasValue || sizeBytes.Value <= 0)
                    {
                        throw new ArgumentException("SizeBytes must be greater than zero.");
                    }
                    mode = RecoveryPartitionSizingMode.ExplicitBytes;
                    return sizeBytes.Value;

                case ParameterSetPercentSize:
                    if (!sizePercent.HasValue || sizePercent.Value <= 0)
                    {
                        throw new ArgumentException("SizePercent must be greater than 0.");
                    }
                    if (sizePercent.Value > RecoveryPartitionConstants.DefaultSizePercentMax)
                    {
                        throw new ArgumentException(
                            "SizePercent must be less than or equal to " +
                            RecoveryPartitionConstants.DefaultSizePercentMax + ".");
                    }
                    mode = RecoveryPartitionSizingMode.Percent;
                    return (long)(diskSizeBytes * (sizePercent.Value / 100.0));

                default:
                    mode = RecoveryPartitionSizingMode.Default;
                    return RecoveryPartitionConstants.DefaultSizeBytes;
            }
        }

        public static long GetDiskSizeBytes(PSCmdlet cmdlet, int diskNumber)
        {
            var storage = new StorageInvoker(cmdlet);
            var disk = storage.InvokeSingle("Get-Disk", new Hashtable { { "Number", diskNumber } });
            if (disk == null) { throw new InvalidOperationException("Disk " + diskNumber + " was not found."); }
            var sizeProp = disk.Properties["Size"];
            if (sizeProp == null || sizeProp.Value == null) { return 0L; }
            return Convert.ToInt64(sizeProp.Value);
        }
    }
}
