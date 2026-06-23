using System.Linq;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Removes a recovery boot entry idempotently.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "WindowsRecoveryBootEntry",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High,
        DefaultParameterSetName = "ByIdentifier")]
    [OutputType(typeof(WindowsRecoveryBootEntryInfo))]
    public sealed class RemoveWindowsRecoveryBootEntryCmdlet : RecoveryCmdletBase
    {
        [Parameter(ParameterSetName = "ByInput", Mandatory = true, ValueFromPipeline = true)]
        public WindowsRecoveryBootEntryInfo InputObject { get; set; }

        [Parameter(ParameterSetName = "ByIdentifier", Mandatory = true)]
        public string Identifier { get; set; }

        [Parameter(ParameterSetName = "ByName", Mandatory = true)]
        public string Name { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            var engine = new BcdEditEngine(this);
            string identifierToRemove = null;
            WindowsRecoveryBootEntryInfo match = null;

            if (InputObject != null) { identifierToRemove = InputObject.Identifier; match = InputObject; }
            else if (!string.IsNullOrEmpty(Identifier)) { identifierToRemove = Identifier; }
            else if (!string.IsNullOrEmpty(Name))
            {
                match = engine.Enumerate(includeHidden: true)
                    .FirstOrDefault(e => string.Equals(e.Name, Name, System.StringComparison.OrdinalIgnoreCase));
                identifierToRemove = match != null ? match.Identifier : null;
            }

            if (string.IsNullOrEmpty(identifierToRemove))
            {
                WriteWarning("No matching boot entry was found.");
                return;
            }

            var target = identifierToRemove + (match != null && match.Name != null ? " (" + match.Name + ")" : string.Empty);
            if (!Force.IsPresent && !ShouldProcess(target, "Remove recovery boot entry")) { return; }

            engine.Remove(identifierToRemove);
            if (PassThru.IsPresent && match != null) { Stamp(match); WriteObject(match); }
        }
    }
}
