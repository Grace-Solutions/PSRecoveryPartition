using System.IO;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Creates a recovery partition idempotently. Sizing is expressed via one
    /// of three mutually exclusive parameter sets: ExplicitSize (-SizeBytes),
    /// PercentSize (-SizePercent), or DefaultSize (1 GiB).
    /// </summary>
    [Cmdlet(VerbsCommon.New, "RecoveryPartition",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High,
        DefaultParameterSetName = SizeResolver.ParameterSetDefaultSize)]
    [OutputType(typeof(RecoveryPartitionInfo))]
    public sealed class NewRecoveryPartitionCmdlet : RecoveryCmdletBase
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
        public string Label { get; set; }

        [Parameter]
        [ValidateSet("NTFS", "FAT32", "ReFS")]
        public string FileSystem { get; set; } = "NTFS";

        [Parameter]
        public FileInfo WindowsREImagePath { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            var diskSize = SizeResolver.GetDiskSizeBytes(this, DiskNumber);
            RecoveryPartitionSizingMode mode;
            var resolved = SizeResolver.Resolve(
                ParameterSetName,
                ParameterSetName == SizeResolver.ParameterSetExplicitSize ? SizeBytes : (long?)null,
                ParameterSetName == SizeResolver.ParameterSetPercentSize ? SizePercent : (int?)null,
                diskSize, out mode);

            var target = "Disk " + DiskNumber + " (size " + resolved + " bytes, mode " + mode + ")";
            if (!ShouldProcess(target, "Create recovery partition")) { return; }

            WriteVerbose("Creating recovery partition on disk " + DiskNumber + ": " + resolved +
                " bytes (" + mode + "), " + FileSystem + ", label '" + (Label ?? "RECOVERY") + "'" +
                (WindowsREImagePath != null ? ", staging '" + WindowsREImagePath.Name + "'" : "") + ".");
            var engine = new RecoveryPartitionEngine(this);
            var info = engine.Create(DiskNumber, resolved, Label, FileSystem, WindowsREImagePath);
            WriteVerbose("Created recovery partition: disk " + info.DiskNumber + " partition " +
                info.PartitionNumber + " at offset " + info.Offset + ".");
            Stamp(info);
            if (PassThru.IsPresent) { WriteObject(info); }
        }
    }
}
