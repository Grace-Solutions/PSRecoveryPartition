using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Enables Windows Recovery Environment.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "WindowsRecoveryEnvironment", SupportsShouldProcess = true)]
    [OutputType(typeof(WindowsRecoveryEnvironmentInfo))]
    public sealed class EnableWindowsRecoveryEnvironmentCmdlet : RecoveryCmdletBase
    {
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess("Windows Recovery Environment", "Enable")) { return; }
            var engine = new WinReEngine(this);
            var info = engine.Enable();
            Stamp(info);
            if (PassThru.IsPresent) { WriteObject(info); }
        }
    }
}
