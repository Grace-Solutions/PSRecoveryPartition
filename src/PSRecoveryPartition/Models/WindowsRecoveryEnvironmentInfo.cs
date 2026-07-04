using System.IO;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Snapshot of Windows Recovery Environment (WinRE) configuration as reported
    /// by <c>reagentc /info</c> or the equivalent native path.
    /// </summary>
    public sealed class WindowsRecoveryEnvironmentInfo : RecoveryResultBase
    {
        public bool Enabled { get; set; }
        // Managed DirectoryInfo view of the WinRE location. Null when reagentc
        // reports a kernel device path (\\?\GlobalRoot...) that managed file
        // APIs cannot open; the raw string is always available on
        // WindowsRELocationPath.
        public DirectoryInfo WindowsRELocation { get; set; }
        // Raw WinRE location exactly as reported by reagentc /info, including
        // \\?\GlobalRoot\device\harddiskN\partitionM\Recovery\WindowsRE forms.
        public string WindowsRELocationPath { get; set; }
        public FileInfo WindowsREImagePath { get; set; }
        public string BootConfigurationDataIdentifier { get; set; }
        public string RecoveryImageLocation { get; set; }
        public string RecoveryImageIndex { get; set; }
        public string CustomImageLocation { get; set; }
        public string CustomImageIndex { get; set; }
        public string StatusText { get; set; }
    }
}
