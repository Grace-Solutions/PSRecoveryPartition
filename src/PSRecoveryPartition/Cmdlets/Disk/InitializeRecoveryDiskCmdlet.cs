using System.Collections.Generic;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Erases a disk and lays down a complete partition set, MDT-style: the
    /// partition table is rewritten, every partition is created in list order, and
    /// each is formatted. Work is done entirely through native disk IOCTLs and
    /// <c>fmifs!FormatEx</c> -- no diskpart and no format.com.
    ///
    /// <para>Use <c>-PartitionLayoutPreset</c> for a named layout, or supply an
    /// ordered <c>-PartitionLayout</c> of <see cref="DiskPartitionSpec"/> entries.
    /// Percentage entries are taken against the free space remaining at that point,
    /// so a trailing 100% entry consumes the rest of the disk.</para>
    ///
    /// <para>This is destructive and irreversible. The disk hosting the running
    /// operating system is always refused; run from Windows PE to lay out the
    /// system disk.</para>
    /// </summary>
    [Cmdlet(VerbsData.Initialize, "RecoveryDisk",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High,
        DefaultParameterSetName = "Preset")]
    [OutputType(typeof(DiskInitializationResult))]
    public sealed class InitializeRecoveryDiskCmdlet : RecoveryCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("DiskId")]
        public int DiskNumber { get; set; }

        [Parameter]
        public DiskPartitionScheme PartitionScheme { get; set; } = DiskPartitionScheme.Gpt;

        // Named layout. Defaults to the modern Windows Setup shape
        // (EFI, MSR, OS, RECOVERY).
        [Parameter(ParameterSetName = "Preset")]
        public DiskPartitionLayoutPreset PartitionLayoutPreset { get; set; } = DiskPartitionLayoutPreset.RecoveryLast;

        // Ordered custom layout. Entries are laid down in index order.
        [Parameter(ParameterSetName = "Custom", Mandatory = true)]
        public DiskPartitionSpec[] PartitionLayout { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            ExecutionMethod = RecoveryExecutionMethod.Native;

            if (DiskNumber < 0)
            {
                throw new PSArgumentOutOfRangeException("DiskNumber", DiskNumber, "Disk number must be non-negative.");
            }

            IList<DiskPartitionSpec> specs = ParameterSetName == "Custom"
                ? new List<DiskPartitionSpec>(PartitionLayout)
                : DiskLayoutPlanner.FromPreset(PartitionLayoutPreset);

            specs = DiskLayoutPlanner.AdaptToScheme(specs, PartitionScheme, WriteWarning);

            var summary = string.Join(", ", Describe(specs));
            var target = "disk " + DiskNumber + " (" + PartitionScheme + "): " + summary;

            if (!Force.IsPresent && !ShouldProcess(target, "Erase and repartition disk")) { return; }

            WriteWarning("Disk " + DiskNumber + " will be erased. All existing partitions and data on it are destroyed.");

            var engine = new DiskInitializationEngine(this);
            var result = engine.Initialize(DiskNumber, PartitionScheme, specs);

            Stamp(result);
            if (PassThru.IsPresent) { WriteObject(result); }
        }

        private static IEnumerable<string> Describe(IEnumerable<DiskPartitionSpec> specs)
        {
            foreach (var s in specs) { yield return s.ToString(); }
        }
    }
}
