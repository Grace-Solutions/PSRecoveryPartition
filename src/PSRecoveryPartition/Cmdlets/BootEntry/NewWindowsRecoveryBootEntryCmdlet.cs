using System;
using System.IO;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Creates a recovery boot entry idempotently. Requires a boot image path
    /// (Winre.wim, Boot.wim, or a custom Windows PE WIM).
    /// </summary>
    [Cmdlet(VerbsCommon.New, "WindowsRecoveryBootEntry",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(WindowsRecoveryBootEntryInfo))]
    public sealed class NewWindowsRecoveryBootEntryCmdlet : RecoveryCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public FileInfo BootImagePath { get; set; }

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

        [Parameter(ValueFromPipeline = true)]
        public object InputObject { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            if (BootImagePath == null || !BootImagePath.Exists)
            {
                throw new FileNotFoundException("Boot image was not found.", BootImagePath != null ? BootImagePath.FullName : "<null>");
            }
            var target = "BCD: '" + Name + "' -> " + BootImagePath.FullName;
            if (!Force.IsPresent && !ShouldProcess(target, "Create recovery boot entry")) { return; }

            var engine = new BcdEditEngine(this);
            var entry = engine.Create(Name, BootImagePath, BootTimeout, BootEntryVisibility, SetDefault.IsPresent, AddLast.IsPresent);
            Stamp(entry);
            if (PassThru.IsPresent) { WriteObject(entry); }
        }
    }
}
