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
        public DirectoryInfo WindowsRELocation { get; set; }
        public FileInfo WindowsREImagePath { get; set; }
        public string BootConfigurationDataIdentifier { get; set; }
        public string RecoveryImageLocation { get; set; }
        public string RecoveryImageIndex { get; set; }
        public string CustomImageLocation { get; set; }
        public string CustomImageIndex { get; set; }
        public string StatusText { get; set; }
    }
}
