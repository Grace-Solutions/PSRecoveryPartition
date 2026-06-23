using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Internal process execution helper. Models the public Start-ProcessWithOutput
    /// shape used elsewhere by the team but is intentionally kept private to this
    /// module per the design specification.
    /// </summary>
    internal static class ProcessExecution
    {
        private static readonly IList<int> DefaultAcceptableExitCodes = new List<int> { 0 };

        public static RecoveryProcessExecutionResult Run(
            RecoveryProcessExecutionRequest request,
            Cmdlet logger = null)
        {
            if (request == null) { throw new ArgumentNullException("request"); }
            if (request.FilePath == null) { throw new ArgumentException("FilePath is required.", "request"); }

            var argumentList = request.ArgumentList ?? new List<string>();
            var acceptable = (request.AcceptableExitCodeList != null && request.AcceptableExitCodeList.Count > 0)
                ? request.AcceptableExitCodeList
                : DefaultAcceptableExitCodes;

            var psi = new ProcessStartInfo
            {
                FileName = request.FilePath.FullName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            foreach (var arg in argumentList) { psi.ArgumentList_Add(arg); }

            if (request.EnvironmentVariables != null)
            {
                foreach (var kv in request.EnvironmentVariables)
                {
                    psi.EnvironmentVariables[kv.Key] = kv.Value;
                }
            }

            var displayArgs = request.SecureArgumentList
                ? "<arguments suppressed>"
                : string.Join(" ", argumentList.Select(QuoteIfNeeded));
            if (request.LogOutput && logger != null)
            {
                logger.WriteVerbose("Start-ProcessWithOutput: " + request.FilePath.FullName + " " + displayArgs);
            }

            var result = new RecoveryProcessExecutionResult
            {
                FilePath = request.FilePath,
                ArgumentList = new List<string>(argumentList),
                StartedAtUtc = DateTimeOffset.UtcNow
            };

            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();

            using (var process = new Process())
            {
                process.StartInfo = psi;
                process.OutputDataReceived += (s, e) => { if (e.Data != null) { stdoutBuilder.AppendLine(e.Data); } };
                process.ErrorDataReceived  += (s, e) => { if (e.Data != null) { stderrBuilder.AppendLine(e.Data); } };

                if (!process.Start())
                {
                    throw new InvalidOperationException(
                        "The process " + request.FilePath.Name + " could not be started.");
                }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var timeout = request.ExecutionTimeout > TimeSpan.Zero ? request.ExecutionTimeout : TimeSpan.FromMinutes(10);
                var interval = request.ExecutionTimeoutInterval > TimeSpan.Zero ? request.ExecutionTimeoutInterval : TimeSpan.FromMilliseconds(250);

                var deadline = DateTime.UtcNow + timeout;
                while (!process.HasExited)
                {
                    if (DateTime.UtcNow >= deadline)
                    {
                        try { process.Kill(); } catch { /* best effort */ }
                        result.TimedOut = true;
                        break;
                    }
                    Thread.Sleep(interval);
                }

                process.WaitForExit();
                result.ExitCode = process.ExitCode;
            }

            result.StandardOutput = stdoutBuilder.ToString();
            result.StandardError  = stderrBuilder.ToString();
            result.CompletedAtUtc = DateTimeOffset.UtcNow;
            result.ExitCodeAccepted = !result.TimedOut && acceptable.Contains(result.ExitCode);

            if (request.LogOutput && logger != null)
            {
                logger.WriteVerbose("Start-ProcessWithOutput: exit=" + result.ExitCode +
                                    " accepted=" + result.ExitCodeAccepted +
                                    " timedOut=" + result.TimedOut);
            }
            return result;
        }

        private static string QuoteIfNeeded(string value)
        {
            if (string.IsNullOrEmpty(value)) { return "\"\""; }
            if (value.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) { return value; }
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }

    /// <summary>
    /// Shim that lets the helper call <c>ProcessStartInfo.ArgumentList.Add</c>
    /// on netstandard2.0 (where the property exists at runtime on .NET Core /
    /// .NET 5+ hosts and is emulated via Arguments on Windows PowerShell 5.1).
    /// </summary>
    internal static class ProcessStartInfoArgumentListExtensions
    {
        public static void ArgumentList_Add(this ProcessStartInfo psi, string argument)
        {
            // ArgumentList is not part of the netstandard2.0 surface, but is
            // available at runtime on PS 7 / .NET Core 3+. Fall back to
            // Arguments quoting for WinPS 5.1 hosts where it is missing.
            var prop = psi.GetType().GetProperty("ArgumentList");
            if (prop != null)
            {
                var list = prop.GetValue(psi) as System.Collections.IList;
                if (list != null) { list.Add(argument); return; }
            }
            if (string.IsNullOrEmpty(psi.Arguments)) { psi.Arguments = Quote(argument); }
            else { psi.Arguments = psi.Arguments + " " + Quote(argument); }
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value)) { return "\"\""; }
            if (value.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) { return value; }
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
