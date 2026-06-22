using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Removes a recovery partition. Requires an explicit confirmation unless
    /// <c>-Force</c> is supplied because partition deletion is irreversible.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "RecoveryPartition",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(RecoveryPartitionInfo))]
    public sealed class RemoveRecoveryPartitionCmdlet : RecoveryCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int DiskNumber { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int PartitionNumber { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var target = "Disk " + DiskNumber + " Partition " + PartitionNumber;
            if (!Force.IsPresent && !ShouldContinue("Permanently remove " + target + "?", "Remove recovery partition"))
            {
                return;
            }
            if (!ShouldProcess(target, "Remove recovery partition")) { return; }

            var engine = new RecoveryPartitionEngine(this);
            var before = engine.Get(DiskNumber, recoveryOnly: false);
            engine.Remove(DiskNumber, PartitionNumber);
            if (PassThru.IsPresent)
            {
                foreach (var p in before)
                {
                    if (p.PartitionNumber == PartitionNumber) { Stamp(p); WriteObject(p); }
                }
            }
        }
    }
}
