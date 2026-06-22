using System.Collections.Generic;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Common surface for cmdlet result objects. Every public result must
    /// disclose the implementation path and whether process fallback was used,
    /// and may carry the captured per-process execution results.
    /// </summary>
    public abstract class RecoveryResultBase
    {
        protected RecoveryResultBase()
        {
            ProcessResults = new List<RecoveryProcessExecutionResult>();
        }

        public RecoveryExecutionMethod ExecutionMethod { get; set; }
        public bool ProcessFallbackUsed { get; set; }
        public IList<RecoveryProcessExecutionResult> ProcessResults { get; set; }
    }
}
