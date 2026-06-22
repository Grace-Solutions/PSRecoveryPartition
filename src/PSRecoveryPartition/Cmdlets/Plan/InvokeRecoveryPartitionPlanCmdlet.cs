using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Applies a recovery partition plan produced by
    /// <c>Get-RecoveryPartitionPlan</c>. Steps are processed in order and the
    /// final <see cref="RecoveryPartitionInfo"/> is emitted when <c>-PassThru</c>
    /// is supplied.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "RecoveryPartitionPlan",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(RecoveryPartitionInfo))]
    public sealed class InvokeRecoveryPartitionPlanCmdlet : RecoveryCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public RecoveryPartitionPlan InputObject { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            var target = "Disk " + InputObject.DiskNumber + " (" + InputObject.Steps.Count + " step(s))";
            if (!Force.IsPresent && !ShouldProcess(target, "Invoke recovery partition plan")) { return; }

            var engine = new RecoveryPartitionEngine(this);
            RecoveryPartitionInfo created = null;
            foreach (var step in InputObject.Steps)
            {
                switch (step.Action)
                {
                    case "CreatePartition":
                        created = engine.Create(
                            InputObject.DiskNumber,
                            InputObject.ResolvedSizeBytes,
                            InputObject.Label,
                            InputObject.FileSystem,
                            InputObject.WindowsREImagePath);
                        break;
                    case "CopyWinREImage":
                        // Image copy is performed inline by Create when an image is supplied.
                        break;
                    case "Skip":
                        WriteVerbose("Skipping step: " + step.Description);
                        break;
                    default:
                        WriteWarning("Unknown plan step '" + step.Action + "'; ignoring.");
                        break;
                }
            }

            if (PassThru.IsPresent && created != null)
            {
                Stamp(created);
                WriteObject(created);
            }
        }
    }
}
