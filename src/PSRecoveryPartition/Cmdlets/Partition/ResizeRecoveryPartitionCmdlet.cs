using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Resizes a recovery partition idempotently using either an explicit byte
    /// count or a percentage of the parent disk.
    /// </summary>
    [Cmdlet(VerbsCommon.Resize, "RecoveryPartition",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High,
        DefaultParameterSetName = SizeResolver.ParameterSetExplicitSize)]
    [OutputType(typeof(RecoveryPartitionInfo))]
    public sealed class ResizeRecoveryPartitionCmdlet : RecoveryCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int DiskNumber { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int PartitionNumber { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = SizeResolver.ParameterSetExplicitSize)]
        [ValidateRange(1L, long.MaxValue)]
        public long SizeBytes { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = SizeResolver.ParameterSetPercentSize)]
        [ValidateRange(1, 100)]
        public int SizePercent { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var diskSize = SizeResolver.GetDiskSizeBytes(this, DiskNumber);
            RecoveryPartitionSizingMode mode;
            var resolved = SizeResolver.Resolve(
                ParameterSetName,
                ParameterSetName == SizeResolver.ParameterSetExplicitSize ? SizeBytes : (long?)null,
                ParameterSetName == SizeResolver.ParameterSetPercentSize ? SizePercent : (int?)null,
                diskSize, out mode);

            var target = "Disk " + DiskNumber + " Partition " + PartitionNumber + " -> " + resolved + " bytes";
            if (!ShouldProcess(target, "Resize recovery partition")) { return; }

            var engine = new RecoveryPartitionEngine(this);
            var info = engine.Resize(DiskNumber, PartitionNumber, resolved);
            Stamp(info);
            if (PassThru.IsPresent) { WriteObject(info); }
        }
    }
}
