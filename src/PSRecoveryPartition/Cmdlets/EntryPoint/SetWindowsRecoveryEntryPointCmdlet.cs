using System;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Configures push-button reset, a boot entry, or both as recovery entry
    /// points. Combines <see cref="WinReEngine"/> and <see cref="BcdEditEngine"/>
    /// behind a single high-level surface.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "WindowsRecoveryEntryPoint",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(WindowsRecoveryEntryPointResult))]
    public sealed class SetWindowsRecoveryEntryPointCmdlet : RecoveryCmdletBase
    {
        [Parameter(Mandatory = true)]
        public RecoveryEntryPointMode EntryPointMode { get; set; } = RecoveryEntryPointMode.BootEntry;

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public FileInfo BootImagePath { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public FileInfo WindowsREImagePath { get; set; }

        // When supplied, the recovery partition is transiently mounted on a
        // free drive letter for the duration of the bcdedit call so the BCD
        // device element is captured as a persistent NT volume identifier.
        [Parameter(ValueFromPipeline = true)]
        public RecoveryPartitionInfo RecoveryPartition { get; set; }

        [Parameter]
        public string BootImageRelativePath { get; set; } = @"\Recovery\WindowsRE\Winre.wim";

        [Parameter]
        public string PushButtonAction { get; set; }

        [Parameter]
        public string Name { get; set; } = "Grace Solutions Recovery";

        [Parameter]
        public TimeSpan BootTimeout { get; set; } = TimeSpan.FromSeconds(10);

        [Parameter]
        public RecoveryBootEntryVisibility BootEntryVisibility { get; set; } = RecoveryBootEntryVisibility.Visible;

        [Parameter]
        public SwitchParameter SetDefault { get; set; }

        [Parameter(ValueFromPipeline = true)]
        public object InputObject { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var result = new WindowsRecoveryEntryPointResult
            {
                EntryPointMode = EntryPointMode,
                StartedAtUtc = DateTimeOffset.UtcNow
            };

            WindowsRecoveryPushButtonAction action = WindowsRecoveryPushButtonAction.BootToRE;
            if (!string.IsNullOrEmpty(PushButtonAction))
            {
                action = PushButtonActionConverter.Convert(PushButtonAction);
            }

            var configurePushButton = EntryPointMode == RecoveryEntryPointMode.PushButtonReset
                || EntryPointMode == RecoveryEntryPointMode.Both;
            var configureBootEntry = EntryPointMode == RecoveryEntryPointMode.BootEntry
                || EntryPointMode == RecoveryEntryPointMode.Both;

            if (configurePushButton)
            {
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
            }

            if (configureBootEntry)
            {
                var bcd = new BcdEditEngine(this);
                var existing = bcd.Enumerate(includeHidden: true)
                    .FirstOrDefault(e => string.Equals(e.Name, Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    result.BootEntry = existing;
                    result.ActionsTaken.Add("Reused existing boot entry '" + Name + "' (" + existing.Identifier + ")");
                }
                else if (RecoveryPartition != null)
                {
                    var rel = string.IsNullOrEmpty(BootImageRelativePath)
                        ? @"\Recovery\WindowsRE\Winre.wim"
                        : (BootImageRelativePath.StartsWith("\\") ? BootImageRelativePath : "\\" + BootImageRelativePath);
                    if (Force.IsPresent || ShouldProcess(
                            "BCD: '" + Name + "' -> disk " + RecoveryPartition.DiskNumber +
                            " partition " + RecoveryPartition.PartitionNumber + rel,
                            "Create recovery boot entry"))
                    {
                        WindowsRecoveryBootEntryInfo created = null;
                        VolumeStaging.WithDriveLetter(RecoveryPartition.DiskNumber, RecoveryPartition.PartitionNumber, letter =>
                        {
                            var image = new FileInfo(letter + rel);
                            if (!image.Exists)
                            {
                                throw new FileNotFoundException(
                                    "Boot image was not found on the recovery partition.", image.FullName);
                            }
                            created = bcd.Create(Name, image, BootTimeout, BootEntryVisibility, SetDefault.IsPresent, false);
                        });
                        result.BootEntry = created;
                        result.ActionsTaken.Add("Created boot entry '" + Name + "' (" + (created != null ? created.Identifier : "?") + ")");
                        result.Changed = true;
                    }
                }
                else
                {
                    if (BootImagePath == null || !BootImagePath.Exists)
                    {
                        throw new FileNotFoundException(
                            "BootImagePath or RecoveryPartition is required for EntryPointMode '" + EntryPointMode + "' and the image must exist.",
                            BootImagePath != null ? BootImagePath.FullName : "<null>");
                    }
                    if (Force.IsPresent || ShouldProcess("BCD: '" + Name + "' -> " + BootImagePath.FullName, "Create recovery boot entry"))
                    {
                        result.BootEntry = bcd.Create(Name, BootImagePath, BootTimeout, BootEntryVisibility, SetDefault.IsPresent, false);
                        result.ActionsTaken.Add("Created boot entry '" + Name + "' (" + result.BootEntry.Identifier + ")");
                        result.Changed = true;
                    }
                }
            }

            result.Success = true;
            result.CompletedAtUtc = DateTimeOffset.UtcNow;
            Stamp(result);
            if (PassThru.IsPresent) { WriteObject(result); }
        }
    }
}
