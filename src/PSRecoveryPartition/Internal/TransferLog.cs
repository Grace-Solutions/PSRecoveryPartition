using System.Management.Automation;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Emits the standard three-line verbose banner shown immediately before a
    /// file copy or download begins:
    /// <code>
    /// Attempting to copy the specified file.
    /// Source: &lt;source file or URI&gt;
    /// Destination: &lt;destination path&gt;
    /// </code>
    /// Centralised so every copy/download surface reports the operation
    /// identically under <c>-Verbose</c>.
    /// </summary>
    internal static class TransferLog
    {
        public static void Attempt(PSCmdlet owner, bool download, string source, string destination)
        {
            if (owner == null) { return; }
            owner.WriteVerbose(download
                ? "Attempting to download the specified file."
                : "Attempting to copy the specified file.");
            owner.WriteVerbose("Source: " + (source ?? "<null>"));
            owner.WriteVerbose("Destination: " + (destination ?? "<null>"));
        }
    }
}
