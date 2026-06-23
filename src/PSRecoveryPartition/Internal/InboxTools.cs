using System;
using System.IO;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Resolves the absolute paths of the Microsoft inbox executables that the
    /// internal process fallback may invoke. Centralising this prevents PATH
    /// hijacking and keeps the audit surface small.
    /// </summary>
    internal static class InboxTools
    {
        public static FileInfo Reagentc { get { return ResolveSystem32("reagentc.exe"); } }
        public static FileInfo Bcdedit  { get { return ResolveSystem32("bcdedit.exe");  } }
        public static FileInfo Mountvol { get { return ResolveSystem32("mountvol.exe"); } }

        private static FileInfo ResolveSystem32(string name)
        {
            var sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (string.IsNullOrEmpty(sys)) { sys = @"C:\Windows\System32"; }
            return new FileInfo(Path.Combine(sys, name));
        }
    }
}
