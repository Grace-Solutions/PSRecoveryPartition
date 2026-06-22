using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Removes a temporary access path from a recovery partition.
    /// </summary>
    [Cmdlet(VerbsData.Dismount, "RecoveryPartition", SupportsShouldProcess = true)]
    [OutputType(typeof(RecoveryPartitionMountResult))]
    public sealed class DismountRecoveryPartitionCmdlet : RecoveryCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int DiskNumber { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int PartitionNumber { get; set; }

        [Parameter(Mandatory = true)]
        public string AccessPath { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var target = "Disk " + DiskNumber + " Partition " + PartitionNumber + " -X- " + AccessPath;
            if (!ShouldProcess(target, "Dismount recovery partition")) { return; }

            var engine = new RecoveryPartitionEngine(this);
            var result = engine.RemoveAccessPath(DiskNumber, PartitionNumber, AccessPath);
            Stamp(result);
            if (PassThru.IsPresent) { WriteObject(result); }
        }
    }
}
