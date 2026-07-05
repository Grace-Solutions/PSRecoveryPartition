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

        // Scopes discovery to the current OS disk (default), all disks, or the
        // secondary disk(s). Prevents recovery/BCD operations from fanning out
        // across every disk on dual-disk or dual-boot systems. Ignored when an
        // explicit -DiskNumber is supplied.
        [Parameter]
        public RecoveryPartitionDetectionMode DetectionMode { get; set; } = RecoveryPartitionDetectionMode.CurrentOSDisk;

        [Parameter]
        public SwitchParameter IncludeNonRecovery { get; set; }

        protected override void ProcessRecord()
        {
            WriteVerbose(DiskNumber.HasValue
                ? "Scanning disk " + DiskNumber.Value + " for recovery partitions."
                : "Scanning disks (DetectionMode " + DetectionMode + ") for recovery partitions.");

            var engine = new RecoveryPartitionEngine(this);
            var partitions = engine.Get(DiskNumber, recoveryOnly: !IncludeNonRecovery.IsPresent, detectionMode: DetectionMode);

            var emitted = 0;
            foreach (var p in partitions)
            {
                if (PartitionNumber.HasValue && p.PartitionNumber != PartitionNumber.Value) { continue; }
                Stamp(p);
                WriteObject(p);
                emitted++;
            }
            WriteVerbose("Found " + emitted + " recovery partition(s).");
        }
    }
}
