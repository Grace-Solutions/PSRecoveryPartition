using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Creates a recovery partition plan without applying changes. Useful for
    /// reviewing exactly which actions <c>Invoke-RecoveryPartitionPlan</c> will
    /// take given the supplied inputs.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RecoveryPartitionPlan",
        DefaultParameterSetName = SizeResolver.ParameterSetDefaultSize)]
    [OutputType(typeof(RecoveryPartitionPlan))]
    public sealed class GetRecoveryPartitionPlanCmdlet : RecoveryCmdletBase
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
            var existing = engine.Get(DiskNumber, recoveryOnly: true);

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
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Changed = existing.Count == 0
            };
            ExecutionMethod = RecoveryExecutionMethod.Storage;

            if (existing.Count == 0)
            {
                plan.Steps.Add(new RecoveryPartitionPlanStep
                {
                    Action = "CreatePartition",
                    Target = "Disk " + DiskNumber,
                    Description = "Create a new recovery partition of " + resolved + " bytes.",
                    Parameters = new Dictionary<string, object>
                    {
                        { "DiskNumber", DiskNumber },
                        { "SizeBytes", resolved },
                        { "Label", Label },
                        { "FileSystem", FileSystem }
                    }
                });
                if (WindowsREImagePath != null)
                {
                    plan.Steps.Add(new RecoveryPartitionPlanStep
                    {
                        Action = "CopyWinREImage",
                        Target = WindowsREImagePath.FullName,
                        Description = "Copy WindowsRE image onto the new partition."
                    });
                }
            }
            else
            {
                plan.Steps.Add(new RecoveryPartitionPlanStep
                {
                    Action = "Skip",
                    Target = "Disk " + DiskNumber,
                    Description = "Recovery partition already exists; no creation required."
                });
            }

            Stamp(plan);
            WriteObject(plan);
        }
    }
}
