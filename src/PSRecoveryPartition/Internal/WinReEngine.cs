using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Internal engine for Windows Recovery Environment configuration. Wraps
    /// the Microsoft inbox <c>reagentc.exe</c> tool via the controlled process
    /// fallback. The public surface never exposes this implementation detail.
    /// </summary>
    internal sealed class WinReEngine
    {
        private readonly RecoveryCmdletBase _owner;

        public WinReEngine(RecoveryCmdletBase owner) { _owner = owner; }

        public WindowsRecoveryEnvironmentInfo GetInfo()
        {
            var result = RunReagentc(new List<string> { "/info" }, "query Windows Recovery Environment configuration");
            var info = Parse(result.StandardOutput ?? string.Empty);
            info.ExecutionMethod = RecoveryExecutionMethod.ProcessFallback;
            info.ProcessFallbackUsed = true;
            info.ProcessResults.Add(result);
            _owner.RecordProcessResult(result);
            return info;
        }

        public WindowsRecoveryEnvironmentInfo Enable()
        {
            var result = RunReagentc(new List<string> { "/enable" }, "enable Windows Recovery Environment");
            _owner.RecordProcessResult(result);
            var info = GetInfo();
            info.ProcessResults.Insert(0, result);
            return info;
        }

        public WindowsRecoveryEnvironmentInfo Disable()
        {
            var result = RunReagentc(new List<string> { "/disable" }, "disable Windows Recovery Environment");
            _owner.RecordProcessResult(result);
            var info = GetInfo();
            info.ProcessResults.Insert(0, result);
            return info;
        }

        public WindowsRecoveryEnvironmentInfo SetReImage(FileInfo imagePath, DirectoryInfo target)
        {
            if (imagePath == null) { throw new ArgumentNullException("imagePath"); }
            var imageDirectory = imagePath.Directory != null ? imagePath.Directory.FullName : imagePath.FullName;
            var args = new List<string> { "/setreimage", "/path", imageDirectory };
            if (target != null) { args.Add("/target"); args.Add(target.FullName); }
            var result = RunReagentc(args, "register WindowsRE image");
            _owner.RecordProcessResult(result);
            var info = GetInfo();
            info.ProcessResults.Insert(0, result);
            return info;
        }

        public WindowsRecoveryEnvironmentInfo BootToRE()
        {
            var result = RunReagentc(new List<string> { "/boottore" }, "schedule a boot into Windows RE");
            _owner.RecordProcessResult(result);
            var info = GetInfo();
            info.ProcessResults.Insert(0, result);
            return info;
        }

        private RecoveryProcessExecutionResult RunReagentc(IList<string> argumentList, string operationDescription)
        {
            var request = new RecoveryProcessExecutionRequest
            {
                FilePath = InboxTools.Reagentc,
                ArgumentList = argumentList,
                AcceptableExitCodeList = new List<int> { 0 },
                LogOutput = true,
                ExecutionTimeout = TimeSpan.FromMinutes(2),
                ExecutionTimeoutInterval = TimeSpan.FromMilliseconds(250),
                WindowStyle = "Hidden",
                Priority = "Normal"
            };
            var result = ProcessExecution.Run(request, _owner);
            if (!result.ExitCodeAccepted)
            {
                throw ProcessFallbackExceptions.BuildFailure(
                    "Could not " + operationDescription, result);
            }
            return result;
        }

        private static readonly Regex KeyValueLine = new Regex(@"^\s*(?<k>[^:]+?)\s*:\s*(?<v>.*)$", RegexOptions.Compiled);

        /// <summary>
        /// Builds a <see cref="DirectoryInfo"/> for a normal filesystem path, or
        /// returns null for the kernel device paths reagentc emits. Managed
        /// DirectoryInfo throws for <c>\\?\GlobalRoot...</c> ("internal to the
        /// kernel") and for any path containing '?' ("Illegal characters in
        /// path"), so those are surfaced only as the raw string.
        /// </summary>
        private static DirectoryInfo TryDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { return null; }
            if (path.IndexOf('?') >= 0 ||
                path.StartsWith(@"\\?\", StringComparison.Ordinal) ||
                path.IndexOf("GlobalRoot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return null;
            }
            try { return new DirectoryInfo(path); }
            catch (ArgumentException) { return null; }
            catch (NotSupportedException) { return null; }
            catch (PathTooLongException) { return null; }
        }

        internal static WindowsRecoveryEnvironmentInfo Parse(string reagentcInfoOutput)
        {
            var info = new WindowsRecoveryEnvironmentInfo
            {
                StatusText = reagentcInfoOutput ?? string.Empty
            };
            if (string.IsNullOrWhiteSpace(reagentcInfoOutput)) { return info; }

            foreach (var rawLine in reagentcInfoOutput.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                var m = KeyValueLine.Match(line);
                if (!m.Success) { continue; }
                var key = m.Groups["k"].Value.Trim();
                var value = m.Groups["v"].Value.Trim();
                if (key.IndexOf("Windows RE status", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    info.Enabled = value.IndexOf("Enabled", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                else if (key.IndexOf("Windows RE location", StringComparison.OrdinalIgnoreCase) >= 0 && value.Length > 0)
                {
                    info.WindowsRELocationPath = value;
                    info.WindowsRELocation = TryDirectory(value);
                }
                else if (key.IndexOf("Boot Configuration Data", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    info.BootConfigurationDataIdentifier = value;
                }
                else if (key.IndexOf("Recovery image location", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    info.RecoveryImageLocation = value;
                }
                else if (key.IndexOf("Recovery image index", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    info.RecoveryImageIndex = value;
                }
                else if (key.IndexOf("Custom image location", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    info.CustomImageLocation = value;
                }
                else if (key.IndexOf("Custom image index", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    info.CustomImageIndex = value;
                }
            }
            return info;
        }
    }
}
