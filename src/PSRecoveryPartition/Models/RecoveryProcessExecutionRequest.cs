using System;
using System.Collections.Generic;
using System.IO;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Internal request object accepted by <c>Start-ProcessWithOutput</c>.
    /// Not exposed as a public cmdlet parameter; surfaced only on the
    /// per-process result objects returned alongside cmdlet output.
    /// </summary>
    public sealed class RecoveryProcessExecutionRequest
    {
        public FileInfo FilePath { get; set; }
        public IList<string> ArgumentList { get; set; }
        public IList<int> AcceptableExitCodeList { get; set; }
        public string WindowStyle { get; set; }
        public string Priority { get; set; }
        public bool LogOutput { get; set; }
        public TimeSpan ExecutionTimeout { get; set; }
        public TimeSpan ExecutionTimeoutInterval { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; set; }
        public bool SecureArgumentList { get; set; }
    }
}
