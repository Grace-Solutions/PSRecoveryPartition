using System.Linq;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Tests whether a recovery partition exists on the specified disk (or on
    /// any disk when <c>-DiskNumber</c> is omitted) and returns <c>$true</c>
    /// or <c>$false</c>.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "RecoveryPartition")]
    [OutputType(typeof(bool))]
    public sealed class TestRecoveryPartitionCmdlet : RecoveryCmdletBase
    {
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int? DiskNumber { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int? PartitionNumber { get; set; }

        protected override void ProcessRecord()
        {
            var engine = new RecoveryPartitionEngine(this);
            var partitions = engine.Get(DiskNumber, recoveryOnly: true);
            var match = PartitionNumber.HasValue
                ? partitions.Any(p => p.PartitionNumber == PartitionNumber.Value)
                : partitions.Any();
            WriteObject(match);
        }
    }
}
