using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Updates recovery partition metadata idempotently.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "RecoveryPartition", SupportsShouldProcess = true)]
    [OutputType(typeof(RecoveryPartitionInfo))]
    public sealed class SetRecoveryPartitionCmdlet : RecoveryCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int DiskNumber { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int PartitionNumber { get; set; }

        [Parameter]
        public string Label { get; set; }

        [Parameter]
        public bool? NoDefaultDriveLetter { get; set; }

        [Parameter]
        public bool? IsHidden { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var target = "Disk " + DiskNumber + " Partition " + PartitionNumber;
            if (!ShouldProcess(target, "Update recovery partition metadata")) { return; }

            var engine = new RecoveryPartitionEngine(this);
            var info = engine.SetMetadata(DiskNumber, PartitionNumber, Label, NoDefaultDriveLetter, IsHidden);
            Stamp(info);
            if (PassThru.IsPresent) { WriteObject(info); }
        }
    }
}
