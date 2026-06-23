using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Returns the current Windows Recovery Environment configuration.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "WindowsRecoveryEnvironment")]
    [OutputType(typeof(WindowsRecoveryEnvironmentInfo))]
    public sealed class GetWindowsRecoveryEnvironmentCmdlet : RecoveryCmdletBase
    {
        protected override void ProcessRecord()
        {
            var engine = new WinReEngine(this);
            var info = engine.GetInfo();
            Stamp(info);
            WriteObject(info);
        }
    }
}
