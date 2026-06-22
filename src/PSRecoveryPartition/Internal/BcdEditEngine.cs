using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Internal engine for Windows Boot Configuration Data manipulation. Wraps
    /// <c>bcdedit.exe</c> via the controlled process fallback. Surface is kept
    /// internal because the public design forbids exposing the shell-out
    /// decision to users.
    /// </summary>
    internal sealed class BcdEditEngine
    {
        private readonly RecoveryCmdletBase _owner;

        public BcdEditEngine(RecoveryCmdletBase owner) { _owner = owner; }

        public IList<WindowsRecoveryBootEntryInfo> Enumerate(bool includeHidden = false)
        {
            var args = new List<string> { "/enum", "all" };
            var result = Run(args, "enumerate boot entries");
            var parsed = ParseEnumerate(result.StandardOutput ?? string.Empty);
            foreach (var e in parsed)
            {
                e.ExecutionMethod = RecoveryExecutionMethod.ProcessFallback;
                e.ProcessFallbackUsed = true;
                e.ProcessResults.Add(result);
                e.DiscoveredAtUtc = DateTimeOffset.UtcNow;
            }
            return includeHidden
                ? parsed
                : parsed.Where(p => p.Visibility == RecoveryBootEntryVisibility.Visible).ToList();
        }

        public WindowsRecoveryBootEntryInfo Create(string name, FileInfo bootImagePath, TimeSpan timeout, RecoveryBootEntryVisibility visibility, bool setDefault, bool addLast)
        {
            if (bootImagePath == null) { throw new ArgumentNullException("bootImagePath"); }

            var createArgs = new List<string> { "/create", "/d", name ?? "Recovery", "/application", "OSLOADER" };
            var create = Run(createArgs, "create boot entry");
            var identifier = ExtractIdentifier(create.StandardOutput);
            if (string.IsNullOrEmpty(identifier))
            {
                throw new InvalidOperationException(
                    "bcdedit.exe did not return a new boot entry identifier. Output: " + (create.StandardOutput ?? "<empty>"));
            }

            var volumeRoot = bootImagePath.Directory != null ? bootImagePath.Directory.Root.FullName.TrimEnd('\\') : "C:";
            var relativeImagePath = bootImagePath.FullName.Substring(volumeRoot.Length).Replace('/', '\\');
            if (!relativeImagePath.StartsWith("\\")) { relativeImagePath = "\\" + relativeImagePath; }

            Run(new List<string> { "/set", identifier, "device", "ramdisk=[" + volumeRoot + "]" + relativeImagePath + ",{ramdiskoptions}" }, "configure boot device");
            Run(new List<string> { "/set", identifier, "osdevice", "ramdisk=[" + volumeRoot + "]" + relativeImagePath + ",{ramdiskoptions}" }, "configure boot osdevice");
            Run(new List<string> { "/set", identifier, "systemroot", "\\Windows" }, "configure boot systemroot");
            Run(new List<string> { "/set", identifier, "winpe", "yes" }, "mark boot entry as Windows PE");

            if (visibility == RecoveryBootEntryVisibility.Hidden)
            {
                Run(new List<string> { "/set", identifier, "displaymessage", "Recovery" }, "configure boot displaymessage");
            }
            if (addLast)
            {
                Run(new List<string> { "/displayorder", identifier, "/addlast" }, "append boot entry to displayorder");
            }
            else
            {
                Run(new List<string> { "/displayorder", identifier, "/addfirst" }, "prepend boot entry to displayorder");
            }
            if (timeout > TimeSpan.Zero)
            {
                Run(new List<string> { "/timeout", ((int)timeout.TotalSeconds).ToString() }, "set boot manager timeout");
            }
            if (setDefault)
            {
                Run(new List<string> { "/default", identifier }, "set boot entry as default");
            }

            return new WindowsRecoveryBootEntryInfo
            {
                Identifier = identifier,
                Name = name ?? "Recovery",
                BootImagePath = bootImagePath,
                BootTimeout = timeout,
                Visibility = visibility,
                IsDefault = setDefault,
                IsRecoveryEntry = true,
                ExecutionMethod = RecoveryExecutionMethod.ProcessFallback,
                ProcessFallbackUsed = true,
                DiscoveredAtUtc = DateTimeOffset.UtcNow
            };
        }

        public void Remove(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) { throw new ArgumentNullException("identifier"); }
            Run(new List<string> { "/delete", identifier, "/f", "/cleanup" }, "remove boot entry " + identifier);
        }

        internal RecoveryProcessExecutionResult Run(IList<string> argumentList, string operationDescription)
        {
            var request = new RecoveryProcessExecutionRequest
            {
                FilePath = InboxTools.Bcdedit,
                ArgumentList = argumentList,
                AcceptableExitCodeList = new List<int> { 0 },
                LogOutput = true,
                ExecutionTimeout = TimeSpan.FromMinutes(2),
                ExecutionTimeoutInterval = TimeSpan.FromMilliseconds(250),
                WindowStyle = "Hidden",
                Priority = "Normal"
            };
            var result = ProcessExecution.Run(request, _owner);
            _owner.RecordProcessResult(result);
            if (!result.ExitCodeAccepted)
            {
                throw ProcessFallbackExceptions.BuildFailure(
                    "Could not " + operationDescription, result);
            }
            return result;
        }

        private static readonly Regex IdentifierRegex = new Regex(@"\{[0-9a-fA-F\-]+\}", RegexOptions.Compiled);
        private static readonly Regex EntryHeaderRegex = new Regex(@"^[A-Za-z].*$", RegexOptions.Compiled);

        private static string ExtractIdentifier(string output)
        {
            if (string.IsNullOrEmpty(output)) { return null; }
            var m = IdentifierRegex.Match(output);
            return m.Success ? m.Value : null;
        }

        internal static IList<WindowsRecoveryBootEntryInfo> ParseEnumerate(string output)
        {
            var entries = new List<WindowsRecoveryBootEntryInfo>();
            if (string.IsNullOrWhiteSpace(output)) { return entries; }

            var lines = output.Replace("\r", string.Empty).Split('\n');
            WindowsRecoveryBootEntryInfo current = null;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) { continue; }
                if (line.StartsWith("---"))
                {
                    if (current != null) { entries.Add(current); }
                    var header = i > 0 ? lines[i - 1].Trim() : string.Empty;
                    current = new WindowsRecoveryBootEntryInfo
                    {
                        Name = header,
                        Visibility = RecoveryBootEntryVisibility.Visible
                    };
                    continue;
                }
                if (current == null) { continue; }
                if (line.StartsWith("identifier", StringComparison.OrdinalIgnoreCase))
                {
                    current.Identifier = line.Substring("identifier".Length).Trim();
                }
                else if (line.StartsWith("description", StringComparison.OrdinalIgnoreCase))
                {
                    var v = line.Substring("description".Length).Trim();
                    if (!string.IsNullOrEmpty(v)) { current.Name = v; }
                    if (v.IndexOf("Recovery", StringComparison.OrdinalIgnoreCase) >= 0) { current.IsRecoveryEntry = true; }
                }
                else if (line.StartsWith("default", StringComparison.OrdinalIgnoreCase))
                {
                    current.IsDefault = true;
                }
            }
            if (current != null) { entries.Add(current); }
            return entries;
        }
    }
}
