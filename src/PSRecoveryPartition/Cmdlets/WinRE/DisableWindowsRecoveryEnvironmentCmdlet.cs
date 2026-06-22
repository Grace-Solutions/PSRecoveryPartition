using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Disables Windows Recovery Environment.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Disable, "WindowsRecoveryEnvironment", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(WindowsRecoveryEnvironmentInfo))]
    public sealed class DisableWindowsRecoveryEnvironmentCmdlet : RecoveryCmdletBase
    {
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            if (!Force.IsPresent && !ShouldProcess("Windows Recovery Environment", "Disable")) { return; }
            var engine = new WinReEngine(this);
            var info = engine.Disable();
            Stamp(info);
            if (PassThru.IsPresent) { WriteObject(info); }
        }
    }
}
