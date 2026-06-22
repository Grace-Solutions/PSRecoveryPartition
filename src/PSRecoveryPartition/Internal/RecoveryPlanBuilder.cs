using System.Collections.Generic;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Produces the ordered, idempotent step list embedded in a
    /// <see cref="RecoveryPartitionPlan"/>. Steps flagged with
    /// <see cref="RecoveryPartitionPlanStep.AlreadySatisfied"/> = true are
    /// still surfaced so callers can see the full intended topology, but the
    /// invoke cmdlet will skip them.
    /// </summary>
    internal static class RecoveryPlanBuilder
    {
        public static void AddPartitionSteps(RecoveryPartitionPlan plan, RecoveryPartitionInfo existing, long resolvedSize)
        {
            if (existing == null)
            {
                plan.Steps.Add(new RecoveryPartitionPlanStep
                {
                    Action = RecoveryPartitionPlanActions.CreatePartition,
                    Target = "Disk " + plan.DiskNumber,
                    Description = "Create a recovery partition of " + resolvedSize + " bytes labeled '" + plan.Label + "'.",
                    Parameters = new Dictionary<string, object>
                    {
                        { "DiskNumber", plan.DiskNumber },
                        { "SizeBytes", resolvedSize },
                        { "Label", plan.Label },
                        { "FileSystem", plan.FileSystem }
                    }
                });
                if (plan.WindowsREImagePath != null)
                {
                    plan.Steps.Add(new RecoveryPartitionPlanStep
                    {
                        Action = RecoveryPartitionPlanActions.CopyWinREImage,
                        Target = plan.WindowsREImagePath.FullName,
                        Description = "Copy WindowsRE image '" + plan.WindowsREImagePath.Name + "' onto the new partition."
                    });
                }
            }
            else if (existing.SizeBytes < resolvedSize)
            {
                plan.Steps.Add(new RecoveryPartitionPlanStep
                {
                    Action = RecoveryPartitionPlanActions.ResizePartition,
                    Target = "Disk " + plan.DiskNumber + " Partition " + existing.PartitionNumber,
                    Description = "Resize partition from " + existing.SizeBytes + " to " + resolvedSize + " bytes.",
                    Parameters = new Dictionary<string, object>
                    {
                        { "DiskNumber", plan.DiskNumber },
                        { "PartitionNumber", existing.PartitionNumber },
                        { "SizeBytes", resolvedSize }
                    }
                });
            }
            else
            {
                plan.Steps.Add(new RecoveryPartitionPlanStep
                {
                    Action = RecoveryPartitionPlanActions.Skip,
                    Target = "Disk " + plan.DiskNumber + " Partition " + existing.PartitionNumber,
                    Description = "Recovery partition already exists at the requested size.",
                    AlreadySatisfied = true
                });
            }
        }

        public static void AddImageSteps(RecoveryPartitionPlan plan)
        {
            if (plan.BootImagePath != null)
            {
                plan.Steps.Add(new RecoveryPartitionPlanStep
                {
                    Action = RecoveryPartitionPlanActions.CopyBootImage,
                    Target = plan.BootImagePath.FullName,
                    Description = "Copy boot image '" + plan.BootImagePath.Name + "' onto the recovery partition."
                });
            }
            if (plan.WindowsREImagePath != null)
            {
                plan.Steps.Add(new RecoveryPartitionPlanStep
                {
                    Action = RecoveryPartitionPlanActions.RegisterWinRE,
                    Target = plan.WindowsREImagePath.FullName,
                    Description = "Register WindowsRE image with reagentc /setreimage."
                });
                plan.Steps.Add(new RecoveryPartitionPlanStep
                {
                    Action = RecoveryPartitionPlanActions.EnableWinRE,
                    Target = "Windows Recovery Environment",
                    Description = "Ensure Windows Recovery Environment is enabled."
                });
            }
        }

        public static void AddEntryPointSteps(RecoveryPartitionPlan plan)
        {
            var configureBoot = plan.EntryPointMode == RecoveryEntryPointMode.BootEntry
                || plan.EntryPointMode == RecoveryEntryPointMode.Both;
            var configurePush = plan.EntryPointMode == RecoveryEntryPointMode.PushButtonReset
                || plan.EntryPointMode == RecoveryEntryPointMode.Both;

            if (configureBoot && plan.BootImagePath != null)
            {
                plan.Steps.Add(new RecoveryPartitionPlanStep
                {
                    Action = RecoveryPartitionPlanActions.CreateBootEntry,
                    Target = "BCD: '" + plan.BootEntryName + "'",
                    Description = "Create or reuse the BCD recovery boot entry '" + plan.BootEntryName + "'."
                });
            }
            if (configurePush)
            {
                plan.Steps.Add(new RecoveryPartitionPlanStep
                {
                    Action = RecoveryPartitionPlanActions.ConfigurePushButton,
                    Target = "Push-button reset (" + (plan.PushButtonAction ?? "BootToRE") + ")",
                    Description = "Configure push-button reset action '" + (plan.PushButtonAction ?? "BootToRE") + "'."
                });
            }
        }
    }
}
