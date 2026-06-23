using System;
using System.Collections.Generic;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Combined result returned by <c>Set-WindowsRecoveryEntryPoint</c>.
    /// </summary>
    public sealed class WindowsRecoveryEntryPointResult : RecoveryResultBase
    {
        public WindowsRecoveryEntryPointResult()
        {
            ActionsTaken = new List<string>();
        }

        public RecoveryEntryPointMode EntryPointMode { get; set; }
        public WindowsRecoveryEnvironmentInfo RecoveryEnvironment { get; set; }
        public WindowsRecoveryBootEntryInfo BootEntry { get; set; }
        public bool Changed { get; set; }
        public bool Success { get; set; }
        public IList<string> ActionsTaken { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset CompletedAtUtc { get; set; }
    }
}
