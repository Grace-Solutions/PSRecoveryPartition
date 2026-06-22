using System;
using System.Collections.Generic;
using System.IO;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Sizing source used by a recovery partition plan.
    /// </summary>
    public enum RecoveryPartitionSizingMode
    {
        Default,
        ExplicitBytes,
        Percent
    }

    /// <summary>
    /// Per-step entry inside a recovery partition plan. Each step describes a
    /// single intent (create / resize / mount / image-copy / etc.) and the
    /// resolved target values, but does not perform the action.
    /// </summary>
    public sealed class RecoveryPartitionPlanStep
    {
        public string Action { get; set; }
        public string Target { get; set; }
        public string Description { get; set; }
        public IDictionary<string, object> Parameters { get; set; }
    }

    /// <summary>
    /// Result of <c>Get-RecoveryPartitionPlan</c>. Captures the resolved sizing,
    /// target disk, and ordered list of steps that <c>Invoke-RecoveryPartitionPlan</c>
    /// would carry out.
    /// </summary>
    public sealed class RecoveryPartitionPlan : RecoveryResultBase
    {
        public RecoveryPartitionPlan()
        {
            Steps = new List<RecoveryPartitionPlanStep>();
        }

        public int DiskNumber { get; set; }
        public RecoveryPartitionSizingMode SizingMode { get; set; }
        public long ResolvedSizeBytes { get; set; }
        public int? SizePercent { get; set; }
        public long? RequestedSizeBytes { get; set; }
        public string Label { get; set; }
        public string FileSystem { get; set; }
        public FileInfo WindowsREImagePath { get; set; }
        public bool Changed { get; set; }
        public IList<RecoveryPartitionPlanStep> Steps { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
