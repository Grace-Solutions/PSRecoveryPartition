using System.Collections.Generic;
using System.Management.Automation;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Base class for module cmdlets. Provides shared state for accumulating
    /// internal process-execution results so they can be surfaced on the
    /// returned result object as required by the design specification.
    /// </summary>
    public abstract class RecoveryCmdletBase : PSCmdlet
    {
        private readonly List<RecoveryProcessExecutionResult> _processResults = new List<RecoveryProcessExecutionResult>();

        internal bool ProcessFallbackUsed { get; private set; }
        internal RecoveryExecutionMethod ExecutionMethod { get; set; } = RecoveryExecutionMethod.Storage;

        internal void RecordProcessResult(RecoveryProcessExecutionResult result)
        {
            if (result == null) { return; }
            _processResults.Add(result);
            ProcessFallbackUsed = true;
            ExecutionMethod = RecoveryExecutionMethod.ProcessFallback;
        }

        internal IList<RecoveryProcessExecutionResult> ProcessResults
        {
            get { return _processResults; }
        }

        internal void Stamp(RecoveryResultBase result)
        {
            if (result == null) { return; }
            result.ExecutionMethod = ExecutionMethod;
            result.ProcessFallbackUsed = ProcessFallbackUsed;
            foreach (var pr in _processResults) { result.ProcessResults.Add(pr); }
        }
    }
}
