using System;
using System.Collections.Generic;
using System.IO;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Rich per-process execution result captured by the internal
    /// <c>Start-ProcessWithOutput</c> helper.
    /// </summary>
    public sealed class RecoveryProcessExecutionResult
    {
        public FileInfo FilePath { get; set; }
        public IList<string> ArgumentList { get; set; }
        public int ExitCode { get; set; }
        public bool ExitCodeAccepted { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
        public bool TimedOut { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset CompletedAtUtc { get; set; }
    }
}
