using System;
using System.IO;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Resolves a user-supplied <c>-DestinationPath</c> into a concrete target
    /// <see cref="FileInfo"/>. If the destination already exists as a directory,
    /// or it lacks an extension and the supplied path string ends in a directory
    /// separator, the source leaf name is appended. Otherwise the destination is
    /// treated as a full file path which may rename the source on copy.
    /// </summary>
    internal static class DestinationPathResolver
    {
        public static FileInfo Resolve(FileInfo destination, string sourceLeafName)
        {
            if (destination == null) { throw new ArgumentNullException("destination"); }
            if (string.IsNullOrEmpty(sourceLeafName)) { throw new ArgumentNullException("sourceLeafName"); }

            RejectUncShare(destination.FullName);
            var raw = destination.FullName;
            var trimmed = raw.TrimEnd();
            var endsWithSeparator = trimmed.Length > 0
                && (trimmed[trimmed.Length - 1] == Path.DirectorySeparatorChar
                    || trimmed[trimmed.Length - 1] == Path.AltDirectorySeparatorChar);

            if (endsWithSeparator || Directory.Exists(raw))
            {
                return new FileInfo(Path.Combine(raw.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), sourceLeafName));
            }

            return destination;
        }

        public static void EnsureParentDirectoryExists(FileInfo target)
        {
            if (target == null) { return; }
            var parent = target.Directory;
            if (parent != null && !parent.Exists) { parent.Create(); }
        }

        /// <summary>
        /// Rejects remote SMB destinations (<c>\\server\share\...</c>) so recovery
        /// payloads cannot be staged across the network. The Win32 device
        /// namespaces (<c>\\?\</c>, <c>\\.\</c>) and their GLOBALROOT form remain
        /// allowed so callers can target volume GUID paths directly.
        /// </summary>
        internal static void RejectUncShare(string path)
        {
            if (string.IsNullOrEmpty(path)) { return; }
            if (path.Length < 2) { return; }
            if (path[0] != '\\' || path[1] != '\\') { return; }
            // \\?\..., \\.\..., \\?\UNC\... and \\?\GLOBALROOT\... are local
            // device namespace entries, not SMB shares.
            if (path.Length >= 4 && (path[2] == '?' || path[2] == '.') && path[3] == '\\')
            {
                if (path.Length >= 8 && path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        "Destination paths on remote SMB shares are not supported (" + path + ").",
                        "destination");
                }
                return;
            }
            throw new ArgumentException(
                "Destination paths on remote SMB shares are not supported (" + path + "). " +
                "Use a local path, a drive letter, or a \\\\?\\Volume{guid}\\ device path.",
                "destination");
        }
    }
}
