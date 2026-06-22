using System.IO;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Sets the Windows Recovery Environment image path and/or schedules a
    /// boot into Windows RE on the next restart.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "WindowsRecoveryEnvironment", SupportsShouldProcess = true)]
    [OutputType(typeof(WindowsRecoveryEnvironmentInfo))]
    public sealed class SetWindowsRecoveryEnvironmentCmdlet : RecoveryCmdletBase
    {
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public FileInfo WindowsREImagePath { get; set; }

        [Parameter]
        public DirectoryInfo Target { get; set; }

        [Parameter]
        public SwitchParameter BootToRE { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var engine = new WinReEngine(this);
            WindowsRecoveryEnvironmentInfo info = null;

            if (WindowsREImagePath != null)
            {
                if (ShouldProcess(WindowsREImagePath.FullName, "Register WindowsRE image"))
                {
                    info = engine.SetReImage(WindowsREImagePath, Target);
                }
            }
            if (BootToRE.IsPresent)
            {
                if (ShouldProcess("Windows", "Schedule boot to RE on next restart"))
                {
                    info = engine.BootToRE();
                }
            }
            if (info == null) { info = engine.GetInfo(); }

            Stamp(info);
            if (PassThru.IsPresent) { WriteObject(info); }
        }
    }
}
