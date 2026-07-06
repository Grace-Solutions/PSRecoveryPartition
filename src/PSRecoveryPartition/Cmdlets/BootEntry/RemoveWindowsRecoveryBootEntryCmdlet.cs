using System;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace PSRecoveryPartition.Cmdlets
{
    /// <summary>
    /// Removes a recovery boot entry idempotently. With <c>-DeleteImageFiles</c>
    /// the staged WIM and boot.sdi the entry booted from are also deleted off the
    /// recovery partition (best effort); by default only the BCD objects are
    /// removed and the staged files are left in place.
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
        [SupportsWildcards]
        public string Name { get; set; }

        // Also delete the staged WIM and boot.sdi the entry booted from. Opt-in:
        // file deletion is irreversible and the files may be shared.
        [Parameter]
        public SwitchParameter DeleteImageFiles { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            var engine = new BcdEditEngine(this);

            // Resolve the set of entries to remove. -Name is a wildcard and may
            // match several entries; -Identifier / -InputObject target exactly one.
            var targets = new System.Collections.Generic.List<WindowsRecoveryBootEntryInfo>();
            if (InputObject != null)
            {
                targets.Add(InputObject);
            }
            else if (!string.IsNullOrEmpty(Identifier))
            {
                targets.Add(new WindowsRecoveryBootEntryInfo { Identifier = Identifier });
            }
            else if (!string.IsNullOrEmpty(Name))
            {
                var pattern = WildcardPattern.Get(Name, WildcardOptions.IgnoreCase);
                targets.AddRange(engine.Enumerate(includeHidden: true)
                    .Where(e => pattern.IsMatch(e.Name ?? string.Empty)));
            }

            if (targets.Count == 0)
            {
                WriteWarning("No matching boot entry was found.");
                return;
            }

            foreach (var entry in targets)
            {
                RemoveOne(engine, entry.Identifier, entry.Name, entry);
            }
        }

        private void RemoveOne(BcdEditEngine engine, string identifier, string name, WindowsRecoveryBootEntryInfo match)
        {
            if (string.IsNullOrEmpty(identifier)) { return; }

            // Resolve the staged payload location BEFORE deleting the entry, while
            // its BCD device element is still readable.
            RamdiskStagingLocation staging = null;
            if (DeleteImageFiles.IsPresent)
            {
                try { staging = engine.ResolveRamdiskStaging(identifier); }
                catch (Exception ex) { WriteVerbose("Could not resolve staged files for cleanup: " + ex.Message); }
            }

            var target = identifier + (!string.IsNullOrEmpty(name) ? " (" + name + ")" : string.Empty);
            if (!Force.IsPresent && !ShouldProcess(target, "Remove recovery boot entry")) { return; }

            engine.Remove(identifier);

            if (DeleteImageFiles.IsPresent) { DeleteStagedFiles(staging); }

            if (PassThru.IsPresent && match != null) { Stamp(match); WriteObject(match); }
        }

        private void DeleteStagedFiles(RamdiskStagingLocation staging)
        {
            if (staging == null || !staging.IsRamdisk)
            {
                WriteWarning("No staged ramdisk image was associated with this entry; no files were deleted.");
                return;
            }
            if (!staging.LocationResolved)
            {
                WriteWarning("Could not resolve the recovery partition (volume token '" + staging.VolumeToken +
                    "'); staged files were left in place.");
                return;
            }

            VolumeStaging.WithVolumeRoot(staging.DiskNumber, staging.PartitionNumber, root =>
            {
                TryDeleteRelative(root, staging.ImageRelativePath, staging.DiskNumber, staging.PartitionNumber);
                if (!string.IsNullOrEmpty(staging.SdiRelativePath))
                {
                    TryDeleteRelative(root, staging.SdiRelativePath, staging.DiskNumber, staging.PartitionNumber);
                }
            });
        }

        private void TryDeleteRelative(string root, string relative, int disk, int partition)
        {
            if (string.IsNullOrEmpty(relative)) { return; }
            var full = JoinLocal(root, relative);
            try
            {
                if (File.Exists(full))
                {
                    // Staged recovery payloads carry Hidden/System/ReadOnly; clear
                    // them so the delete is not refused.
                    try { File.SetAttributes(full, FileAttributes.Normal); } catch { /* best effort */ }
                    File.Delete(full);
                    WriteVerbose("Deleted staged file " + relative + " on disk " + disk + " partition " + partition + ".");
                }
                else
                {
                    WriteVerbose("Staged file " + relative + " was not present; nothing to delete.");
                }
            }
            catch (Exception ex)
            {
                WriteWarning("Could not delete staged file " + relative + " (" + ex.Message + ").");
            }
        }

        private static string JoinLocal(string root, string relative)
        {
            var rel = (relative ?? string.Empty).Replace('/', '\\').Trim('\\');
            return rel.Length == 0 ? root : Path.Combine(root, rel);
        }
    }
}
