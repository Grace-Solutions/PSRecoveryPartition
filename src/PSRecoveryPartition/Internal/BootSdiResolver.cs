using System;
using System.Collections.Generic;
using System.IO;
using PSRecoveryPartition.Native;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Resolves a <c>boot.sdi</c> to stage alongside a RAM-booted recovery image.
    /// Resolution order:
    /// <list type="number">
    ///   <item>an explicit <c>-BootSdiPath</c> when supplied;</item>
    ///   <item>a copy already present on the live OS (the boot files carry one);</item>
    ///   <item>extraction from the supplied boot image via WIMGAPI.</item>
    /// </list>
    /// Returns a path to a readable <c>boot.sdi</c> on the local file system (which
    /// may be a temp extraction the caller is expected to copy onto the target).
    /// </summary>
    internal static class BootSdiResolver
    {
        // Locations on a live Windows install that commonly carry boot.sdi.
        private static IEnumerable<string> LiveOsCandidates()
        {
            var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!string.IsNullOrEmpty(windir))
            {
                yield return Path.Combine(windir, @"Boot\DVD\PCAT\boot.sdi");
                yield return Path.Combine(windir, @"Boot\PCAT\boot.sdi");
            }
            var sysDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
            if (!string.IsNullOrEmpty(sysDrive))
            {
                yield return Path.Combine(sysDrive, @"Recovery\WindowsRE\boot.sdi");
            }
        }

        // Paths inside a WIM where boot.sdi is typically found.
        private static readonly string[] InImageCandidates =
        {
            @"\Windows\Boot\DVD\PCAT\boot.sdi",
            @"\Windows\Boot\PCAT\boot.sdi",
            @"\Windows\Boot\DVD\EFI\boot.sdi",
        };

        /// <summary>
        /// Resolves boot.sdi. <paramref name="explicitPath"/> wins when supplied.
        /// <paramref name="bootImage"/>/<paramref name="imageIndex"/> are used only
        /// for the extraction fallback. <paramref name="scratchDir"/> receives any
        /// temp extraction. Throws when no source can be found.
        /// </summary>
        public static FileInfo Resolve(FileInfo explicitPath, FileInfo bootImage, int imageIndex, string scratchDir, Action<string> verbose)
        {
            if (explicitPath != null)
            {
                if (!explicitPath.Exists)
                {
                    throw new FileNotFoundException("The specified boot.sdi was not found.", explicitPath.FullName);
                }
                Log(verbose, "boot.sdi: using supplied '" + explicitPath.FullName + "'.");
                return explicitPath;
            }

            foreach (var candidate in LiveOsCandidates())
            {
                if (File.Exists(candidate))
                {
                    Log(verbose, "boot.sdi: found on live OS at '" + candidate + "'.");
                    return new FileInfo(candidate);
                }
            }

            if (bootImage != null && bootImage.Exists)
            {
                Directory.CreateDirectory(scratchDir);
                var dest = Path.Combine(scratchDir, "boot.sdi");
                foreach (var inImage in InImageCandidates)
                {
                    try
                    {
                        if (WimgApi.TryExtractFile(bootImage.FullName, imageIndex, inImage, dest))
                        {
                            Log(verbose, "boot.sdi: extracted '" + inImage + "' from '" + bootImage.Name + "'.");
                            return new FileInfo(dest);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(verbose, "boot.sdi: extract attempt '" + inImage + "' failed (" + ex.Message + ").");
                    }
                }
            }

            throw new FileNotFoundException(
                "Could not resolve a boot.sdi. Supply one with -BootSdiPath, or use a boot image that contains one. " +
                "boot.sdi is required for the default RAM-boot mode; -ExpandBootImage (flat boot) does not need it.");
        }

        private static void Log(Action<string> verbose, string message)
        {
            if (verbose != null) { verbose(message); }
        }
    }
}
