using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Builds an idempotent recovery partition plan without applying changes.
    /// The plan captures every step needed to stand up a recovery partition
    /// end-to-end: create or resize the partition, copy a WindowsRE image,
    /// copy a boot image, register WinRE, create a BCD recovery boot entry,
    /// and configure push-button reset. Use <c>Invoke-RecoveryPartitionPlan</c>
    /// to apply the plan.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "RecoveryPartitionPlan",
        DefaultParameterSetName = SizeResolver.ParameterSetDefaultSize)]
    [OutputType(typeof(RecoveryPartitionPlan))]
    public sealed class NewRecoveryPartitionPlanCmdlet : RecoveryCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int DiskNumber { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = SizeResolver.ParameterSetExplicitSize)]
        [ValidateRange(1L, long.MaxValue)]
        public long SizeBytes { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = SizeResolver.ParameterSetPercentSize)]
        [ValidateRange(1, 100)]
        public int SizePercent { get; set; }

        [Parameter]
        public string Label { get; set; } = RecoveryPartitionConstants.DefaultLabel;

        [Parameter]
        [ValidateSet("NTFS", "FAT32", "ReFS")]
        public string FileSystem { get; set; } = "NTFS";

        [Parameter]
        public FileInfo WindowsREImagePath { get; set; }

        [Parameter]
        public FileInfo BootImagePath { get; set; }

        [Parameter]
        public string BootEntryName { get; set; } = "Grace Solutions Recovery";

        [Parameter]
        public TimeSpan BootTimeout { get; set; } = TimeSpan.FromSeconds(10);

        [Parameter]
        public RecoveryBootEntryVisibility BootEntryVisibility { get; set; } = RecoveryBootEntryVisibility.Visible;

        [Parameter]
        public SwitchParameter SetDefaultBootEntry { get; set; }

        [Parameter]
        public RecoveryEntryPointMode EntryPointMode { get; set; } = RecoveryEntryPointMode.None;

        [Parameter]
        public string PushButtonAction { get; set; }

        protected override void ProcessRecord()
        {
            var diskSize = SizeResolver.GetDiskSizeBytes(this, DiskNumber);
            RecoveryPartitionSizingMode mode;
            var resolved = SizeResolver.Resolve(
                ParameterSetName,
                ParameterSetName == SizeResolver.ParameterSetExplicitSize ? SizeBytes : (long?)null,
                ParameterSetName == SizeResolver.ParameterSetPercentSize ? SizePercent : (int?)null,
                diskSize, out mode);

            var engine = new RecoveryPartitionEngine(this);
            var existing = engine.Get(DiskNumber, recoveryOnly: true).FirstOrDefault();
            var layout = RecoveryPartitionLayoutAnalyzer.Analyze(
                this,
                DiskNumber,
                existing != null ? (int?)existing.PartitionNumber : null,
                resolved);

            var plan = new RecoveryPartitionPlan
            {
                DiskNumber = DiskNumber,
                SizingMode = mode,
                ResolvedSizeBytes = resolved,
                SizePercent = ParameterSetName == SizeResolver.ParameterSetPercentSize ? (int?)SizePercent : null,
                RequestedSizeBytes = ParameterSetName == SizeResolver.ParameterSetExplicitSize ? (long?)SizeBytes : null,
                Label = Label,
                FileSystem = FileSystem,
                WindowsREImagePath = WindowsREImagePath,
                BootImagePath = BootImagePath,
                BootEntryName = BootEntryName,
                BootTimeout = BootTimeout,
                BootEntryVisibility = BootEntryVisibility,
                SetDefaultBootEntry = SetDefaultBootEntry.IsPresent,
                EntryPointMode = EntryPointMode,
                PushButtonAction = PushButtonAction,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ExistingPartitionNumber = existing != null ? (int?)existing.PartitionNumber : null,
                ExistingPartitionSizeBytes = existing != null ? (long?)existing.SizeBytes : null,
                LayoutAnalysis = layout
            };
            ExecutionMethod = RecoveryExecutionMethod.Storage;

            foreach (var warning in layout.Warnings) { WriteWarning(warning); }

            RecoveryPlanBuilder.AddPartitionSteps(plan, existing, resolved, layout);
            RecoveryPlanBuilder.AddImageSteps(plan);
            RecoveryPlanBuilder.AddEntryPointSteps(plan);

            plan.Changed = plan.Steps.Any(s => !s.AlreadySatisfied && s.Action != RecoveryPartitionPlanActions.Skip);

            Stamp(plan);
            WriteObject(plan);
        }
    }
}
