using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Sanctioned legacy fallback for stamping an MBR partition type byte (in
    /// practice 0x27 for Windows recovery). The native IOCTL path
    /// (<c>Win32PartitionWriter.SetMbrType</c>) is preferred and is tried
    /// first by callers; this fallback exists for the rare cases where the
    /// IOCTL is rejected by the volume manager and a full layout rewrite is
    /// unsafe. The fallback writes a temp diskpart script and shells out to
    /// <c>diskpart.exe</c> via <see cref="ProcessExecution"/>.
    /// </summary>
    internal static class DiskpartMbrTypeSetter
    {
        public static RecoveryProcessExecutionResult Apply(
            PSCmdlet cmdlet, int diskNumber, int partitionNumber, byte mbrType)
        {
            var scriptPath = Path.Combine(
                Path.GetTempPath(),
                "PSRecoveryPartition-setid-" + Guid.NewGuid().ToString("N") + ".txt");

            // Diskpart consumes one statement per line. SET ID overrides the
            // partition type byte; OVERRIDE forces the change on partitions
            // whose volume is currently mounted.
            var script = string.Join(Environment.NewLine, new[]
            {
                "select disk " + diskNumber.ToString(CultureInfo.InvariantCulture),
                "select partition " + partitionNumber.ToString(CultureInfo.InvariantCulture),
                "set id=" + mbrType.ToString("X2", CultureInfo.InvariantCulture) + " override",
                "exit",
                string.Empty,
            });
            File.WriteAllText(scriptPath, script);

            try
            {
                var request = new RecoveryProcessExecutionRequest
                {
                    FilePath               = new FileInfo(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\diskpart.exe")),
                    ArgumentList           = new List<string> { "/s", scriptPath },
                    AcceptableExitCodeList = new List<int> { 0 },
                    LogOutput              = true,
                };
                return ProcessExecution.Run(request, cmdlet);
            }
            finally
            {
                try { File.Delete(scriptPath); } catch { /* best effort */ }
            }
        }
    }
}
