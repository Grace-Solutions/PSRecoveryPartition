using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Adds a temporary access path to a recovery partition.
    /// </summary>
    [Cmdlet(VerbsData.Mount, "RecoveryPartition", SupportsShouldProcess = true)]
    [OutputType(typeof(RecoveryPartitionMountResult))]
    public sealed class MountRecoveryPartitionCmdlet : RecoveryCmdletBase
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
            var target = "Disk " + DiskNumber + " Partition " + PartitionNumber + " -> " + AccessPath;
            if (!ShouldProcess(target, "Mount recovery partition")) { return; }

            var engine = new RecoveryPartitionEngine(this);
            var result = engine.AddAccessPath(DiskNumber, PartitionNumber, AccessPath);
            Stamp(result);
            if (PassThru.IsPresent) { WriteObject(result); }
        }
    }
}
