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
        public bool AlreadySatisfied { get; set; }
    }

    /// <summary>
    /// Well-known plan step action names. Stable strings; the invoke cmdlet
    /// dispatches on these values.
    /// </summary>
    public static class RecoveryPartitionPlanActions
    {
        public const string CreatePartition         = "CreatePartition";
        public const string ResizePartition         = "ResizePartition";
        public const string CopyWinREImage          = "CopyWinREImage";
        public const string CopyBootImage           = "CopyBootImage";
        public const string RegisterWinRE           = "RegisterWinRE";
        public const string EnableWinRE             = "EnableWinRE";
        public const string CreateBootEntry         = "CreateBootEntry";
        public const string ConfigurePushButton     = "ConfigurePushButton";
        public const string Skip                    = "Skip";
    }

    /// <summary>
    /// Result of <c>New-RecoveryPartitionPlan</c>. Captures the resolved sizing,
    /// target disk, and ordered list of steps that <c>Invoke-RecoveryPartitionPlan</c>
    /// would carry out idempotently.
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
        public FileInfo BootImagePath { get; set; }
        public string BootEntryName { get; set; }
        public TimeSpan? BootTimeout { get; set; }
        public RecoveryBootEntryVisibility BootEntryVisibility { get; set; }
        public bool SetDefaultBootEntry { get; set; }
        public RecoveryEntryPointMode EntryPointMode { get; set; }
        public string PushButtonAction { get; set; }
        public int? ExistingPartitionNumber { get; set; }
        public long? ExistingPartitionSizeBytes { get; set; }
        public bool Changed { get; set; }
        public IList<RecoveryPartitionPlanStep> Steps { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
