using System;
using System.IO;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Configures push-button reset / Windows Recovery Environment as a recovery
    /// entry point. Custom BCD boot entries are created by
    /// <c>New-WindowsRecoveryBootEntry</c>; this cmdlet no longer creates them, so
    /// there is a single, unambiguous path for boot-entry creation.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "WindowsRecoveryEntryPoint",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(WindowsRecoveryEntryPointResult))]
    public sealed class SetWindowsRecoveryEntryPointCmdlet : RecoveryCmdletBase
    {
        [Parameter]
        public RecoveryEntryPointMode EntryPointMode { get; set; } = RecoveryEntryPointMode.PushButtonReset;

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public FileInfo WindowsREImagePath { get; set; }

        [Parameter]
        public string PushButtonAction { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            if (EntryPointMode == RecoveryEntryPointMode.BootEntry
                || EntryPointMode == RecoveryEntryPointMode.Both)
            {
                throw new PSNotSupportedException(
                    "Set-WindowsRecoveryEntryPoint no longer creates BCD boot entries. " +
                    "Use New-WindowsRecoveryBootEntry for custom boot entries; use this cmdlet with " +
                    "-EntryPointMode PushButtonReset to configure Windows RE / push-button reset.");
            }

            var result = new WindowsRecoveryEntryPointResult
            {
                EntryPointMode = EntryPointMode,
                StartedAtUtc = DateTimeOffset.UtcNow
            };

            var action = WindowsRecoveryPushButtonAction.BootToRE;
            if (!string.IsNullOrEmpty(PushButtonAction))
            {
                action = PushButtonActionConverter.Convert(PushButtonAction);
            }

            var winRe = new WinReEngine(this);
            var info = winRe.GetInfo();
            result.RecoveryEnvironment = info;

            if (WindowsREImagePath != null)
            {
                var alreadyRegistered = info.WindowsRELocation != null
                    && info.WindowsRELocation.FullName.IndexOf(WindowsREImagePath.DirectoryName ?? string.Empty,
                        StringComparison.OrdinalIgnoreCase) >= 0;
                if (!alreadyRegistered && (Force.IsPresent || ShouldProcess(WindowsREImagePath.FullName, "Register WindowsRE image")))
                {
                    result.RecoveryEnvironment = winRe.SetReImage(WindowsREImagePath, null);
                    result.ActionsTaken.Add("Registered WindowsRE image '" + WindowsREImagePath.FullName + "'");
                    result.Changed = true;
                }
            }
            if (!result.RecoveryEnvironment.Enabled
                && (Force.IsPresent || ShouldProcess("Windows Recovery Environment", "Enable")))
            {
                result.RecoveryEnvironment = winRe.Enable();
                result.ActionsTaken.Add("Enabled Windows Recovery Environment");
                result.Changed = true;
            }
            if (action == WindowsRecoveryPushButtonAction.BootToRE
                && (Force.IsPresent || ShouldProcess("Windows", "Schedule boot to RE on next restart")))
            {
                result.RecoveryEnvironment = winRe.BootToRE();
                result.ActionsTaken.Add("Scheduled boot to RE on next restart");
                result.Changed = true;
            }
            else if (action != WindowsRecoveryPushButtonAction.BootToRE)
            {
                throw new NotSupportedException(
                    "The requested push-button action " + action +
                    " could not be configured because the current Windows installation does not expose a supported configuration path for that action.");
            }

            result.Success = true;
            result.CompletedAtUtc = DateTimeOffset.UtcNow;
            Stamp(result);
            if (PassThru.IsPresent) { WriteObject(result); }
        }
    }
}
