using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Discovers recovery partitions and returns rich <see cref="RecoveryPartitionInfo"/> objects.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RecoveryPartition")]
    [OutputType(typeof(RecoveryPartitionInfo))]
    public sealed class GetRecoveryPartitionCmdlet : RecoveryCmdletBase
    {
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int? DiskNumber { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int? PartitionNumber { get; set; }

        [Parameter]
        public SwitchParameter IncludeNonRecovery { get; set; }

        protected override void ProcessRecord()
        {
            var engine = new RecoveryPartitionEngine(this);
            var partitions = engine.Get(DiskNumber, recoveryOnly: !IncludeNonRecovery.IsPresent);
            foreach (var p in partitions)
            {
                if (PartitionNumber.HasValue && p.PartitionNumber != PartitionNumber.Value) { continue; }
                Stamp(p);
                WriteObject(p);
            }
        }
    }
}
