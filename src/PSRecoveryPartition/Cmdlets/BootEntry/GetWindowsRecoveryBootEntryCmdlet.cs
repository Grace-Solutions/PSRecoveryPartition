using System.IO;
using System.Linq;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Discovers recovery boot entries.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "WindowsRecoveryBootEntry")]
    [OutputType(typeof(WindowsRecoveryBootEntryInfo))]
    public sealed class GetWindowsRecoveryBootEntryCmdlet : RecoveryCmdletBase
    {
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public FileInfo BootImagePath { get; set; }

        [Parameter]
        public SwitchParameter IncludeHidden { get; set; }

        [Parameter]
        public SwitchParameter IncludeAll { get; set; }

        protected override void ProcessRecord()
        {
            var engine = new BcdEditEngine(this);
            var entries = engine.Enumerate(IncludeHidden.IsPresent || IncludeAll.IsPresent);
            foreach (var entry in entries)
            {
                if (!IncludeAll.IsPresent && !entry.IsRecoveryEntry) { continue; }
                if (!string.IsNullOrEmpty(Name) && !string.Equals(entry.Name, Name, System.StringComparison.OrdinalIgnoreCase)) { continue; }
                Stamp(entry);
                WriteObject(entry);
            }
        }
    }
}
