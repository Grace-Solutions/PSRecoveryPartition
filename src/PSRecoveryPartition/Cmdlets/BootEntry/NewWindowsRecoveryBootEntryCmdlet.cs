using System;
using System.IO;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Creates a recovery boot entry idempotently. The boot image is supplied
    /// either as a full <c>-BootImagePath</c> or as a <c>-RecoveryPartition</c>
    /// (typically pipelined from <c>Get-RecoveryPartition</c>) plus an optional
    /// <c>-BootImageRelativePath</c>. In the partition case the partition is
    /// transiently mounted on a free drive letter for the duration of the
    /// bcdedit call so the BCD device element is captured as a persistent NT
    /// volume identifier rather than a runtime drive letter.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "WindowsRecoveryBootEntry",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High,
        DefaultParameterSetName = "ByImagePath")]
    [OutputType(typeof(WindowsRecoveryBootEntryInfo))]
    public sealed class NewWindowsRecoveryBootEntryCmdlet : RecoveryCmdletBase
    {
        [Parameter(ParameterSetName = "ByImagePath", Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public FileInfo BootImagePath { get; set; }

        [Parameter(ParameterSetName = "ByRecoveryPartition", Mandatory = true, ValueFromPipeline = true)]
        public RecoveryPartitionInfo RecoveryPartition { get; set; }

        [Parameter(ParameterSetName = "ByRecoveryPartition")]
        public string BootImageRelativePath { get; set; } = @"\Recovery\WindowsRE\Winre.wim";

        [Parameter]
        public string Name { get; set; } = "Grace Solutions Recovery";

        [Parameter]
        public TimeSpan BootTimeout { get; set; } = TimeSpan.FromSeconds(10);

        [Parameter]
        public RecoveryBootEntryVisibility BootEntryVisibility { get; set; } = RecoveryBootEntryVisibility.Visible;

        [Parameter]
        public SwitchParameter SetDefault { get; set; }

        [Parameter]
        public SwitchParameter AddLast { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            if (ParameterSetName == "ByRecoveryPartition")
            {
                if (RecoveryPartition == null)
                {
                    throw new PSArgumentNullException("RecoveryPartition");
                }
                var rel = string.IsNullOrEmpty(BootImageRelativePath)
                    ? @"\Recovery\WindowsRE\Winre.wim"
                    : (BootImageRelativePath.StartsWith("\\") ? BootImageRelativePath : "\\" + BootImageRelativePath);
                var target = "BCD: '" + Name + "' -> disk " + RecoveryPartition.DiskNumber +
                             " partition " + RecoveryPartition.PartitionNumber + rel;
                if (!Force.IsPresent && !ShouldProcess(target, "Create recovery boot entry")) { return; }

                WindowsRecoveryBootEntryInfo created = null;
                VolumeStaging.WithDriveLetter(RecoveryPartition.DiskNumber, RecoveryPartition.PartitionNumber, letter =>
                {
                    var image = new FileInfo(letter + rel);
                    if (!image.Exists)
                    {
                        throw new FileNotFoundException(
                            "Boot image was not found on the recovery partition.", image.FullName);
                    }
                    var engine = new BcdEditEngine(this);
                    created = engine.Create(Name, image, BootTimeout, BootEntryVisibility, SetDefault.IsPresent, AddLast.IsPresent);
                });
                Stamp(created);
                if (PassThru.IsPresent) { WriteObject(created); }
                return;
            }

            if (BootImagePath == null || !BootImagePath.Exists)
            {
                throw new FileNotFoundException("Boot image was not found.", BootImagePath != null ? BootImagePath.FullName : "<null>");
            }
            var imageTarget = "BCD: '" + Name + "' -> " + BootImagePath.FullName;
            if (!Force.IsPresent && !ShouldProcess(imageTarget, "Create recovery boot entry")) { return; }

            var bcd = new BcdEditEngine(this);
            var entry = bcd.Create(Name, BootImagePath, BootTimeout, BootEntryVisibility, SetDefault.IsPresent, AddLast.IsPresent);
            Stamp(entry);
            if (PassThru.IsPresent) { WriteObject(entry); }
        }
    }
}
