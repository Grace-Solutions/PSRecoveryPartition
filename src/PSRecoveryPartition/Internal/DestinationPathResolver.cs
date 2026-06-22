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
    }
}
