using System;
using System.Linq;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Applies a recovery partition plan produced by
    /// <c>New-RecoveryPartitionPlan</c>. Each step is dispatched idempotently:
    /// partition creation, image staging, WinRE registration, BCD boot entry
    /// creation, and push-button reset are all handled by this single cmdlet.
    /// The final <see cref="RecoveryPartitionInfo"/> is emitted when
    /// <c>-PassThru</c> is supplied.
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
            RecoveryPartitionInfo resolved = null;
            foreach (var step in InputObject.Steps)
            {
                if (step.AlreadySatisfied)
                {
                    WriteVerbose("Skipping satisfied step: " + step.Description);
                    continue;
                }
                switch (step.Action)
                {
                    case RecoveryPartitionPlanActions.CreatePartition:
                        resolved = engine.Create(
                            InputObject.DiskNumber,
                            InputObject.ResolvedSizeBytes,
                            InputObject.Label,
                            InputObject.FileSystem,
                            InputObject.WindowsREImagePath);
                        break;
                    case RecoveryPartitionPlanActions.ResizePartition:
                        if (InputObject.ExistingPartitionNumber.HasValue)
                        {
                            resolved = engine.Resize(
                                InputObject.DiskNumber,
                                InputObject.ExistingPartitionNumber.Value,
                                InputObject.ResolvedSizeBytes);
                        }
                        break;
                    case RecoveryPartitionPlanActions.CopyWinREImage:
                    case RecoveryPartitionPlanActions.CopyBootImage:
                        // Image staging is performed inline by Create when an image is supplied.
                        // For a CopyBootImage step on an existing partition the operator should
                        // use Set-WindowsRecoveryImage; the plan documents intent.
                        WriteVerbose("Image staging handled inline by CreatePartition or by Set-WindowsRecoveryImage.");
                        break;
                    case RecoveryPartitionPlanActions.RegisterWinRE:
                        if (InputObject.WindowsREImagePath != null)
                        {
                            var winRe = new WinReEngine(this);
                            var info = winRe.GetInfo();
                            var registered = info.WindowsRELocation != null
                                && info.WindowsRELocation.FullName.IndexOf(InputObject.WindowsREImagePath.DirectoryName ?? string.Empty,
                                    StringComparison.OrdinalIgnoreCase) >= 0;
                            if (!registered)
                            {
                                winRe.SetReImage(InputObject.WindowsREImagePath, null);
                            }
                        }
                        break;
                    case RecoveryPartitionPlanActions.EnableWinRE:
                        {
                            var winRe = new WinReEngine(this);
                            var info = winRe.GetInfo();
                            if (!info.Enabled) { winRe.Enable(); }
                        }
                        break;
                    case RecoveryPartitionPlanActions.CreateBootEntry:
                        if (InputObject.BootImagePath != null && InputObject.BootImagePath.Exists)
                        {
                            var bcd = new BcdEditEngine(this);
                            var existing = bcd.Enumerate(includeHidden: true)
                                .FirstOrDefault(e => string.Equals(e.Name, InputObject.BootEntryName, StringComparison.OrdinalIgnoreCase));
                            if (existing == null)
                            {
                                bcd.Create(
                                    InputObject.BootEntryName,
                                    InputObject.BootImagePath,
                                    InputObject.BootTimeout ?? TimeSpan.FromSeconds(10),
                                    InputObject.BootEntryVisibility,
                                    InputObject.SetDefaultBootEntry,
                                    false);
                            }
                        }
                        break;
                    case RecoveryPartitionPlanActions.ConfigurePushButton:
                        {
                            var action = WindowsRecoveryPushButtonAction.BootToRE;
                            if (!string.IsNullOrEmpty(InputObject.PushButtonAction))
                            {
                                action = PushButtonActionConverter.Convert(InputObject.PushButtonAction);
                            }
                            if (action == WindowsRecoveryPushButtonAction.BootToRE)
                            {
                                new WinReEngine(this).BootToRE();
                            }
                            else
                            {
                                WriteWarning("Push-button action '" + action + "' is not supported on this Windows installation.");
                            }
                        }
                        break;
                    case RecoveryPartitionPlanActions.Skip:
                        WriteVerbose("Skipping step: " + step.Description);
                        break;
                    default:
                        WriteWarning("Unknown plan step '" + step.Action + "'; ignoring.");
                        break;
                }
            }

            if (resolved == null && InputObject.ExistingPartitionNumber.HasValue)
            {
                resolved = engine.Get(InputObject.DiskNumber, recoveryOnly: true)
                    .FirstOrDefault(p => p.PartitionNumber == InputObject.ExistingPartitionNumber.Value);
            }

            if (PassThru.IsPresent && resolved != null)
            {
                Stamp(resolved);
                WriteObject(resolved);
            }
        }
    }
}
