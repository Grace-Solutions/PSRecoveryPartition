using System;
using System.Text;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Builds the plain-English error message required by the design
    /// specification whenever an internal process fallback fails.
    /// </summary>
    internal static class ProcessFallbackExceptions
    {
        public static InvalidOperationException BuildFailure(
            string operationDescription,
            RecoveryProcessExecutionResult result)
        {
            if (result == null) { throw new ArgumentNullException("result"); }
            var tool = result.FilePath != null ? result.FilePath.Name : "<unknown tool>";

            var stderr = result.StandardError ?? string.Empty;
            stderr = stderr.Replace("\r", " ").Replace("\n", " ").Trim();
            if (stderr.Length > 1024) { stderr = stderr.Substring(0, 1024) + "..."; }

            var sb = new StringBuilder();
            sb.Append(operationDescription).Append(" because ").Append(tool);
            if (result.TimedOut)
            {
                sb.Append(" timed out after ")
                  .Append((result.CompletedAtUtc - result.StartedAtUtc).TotalSeconds.ToString("0.0"))
                  .Append(" seconds.");
            }
            else
            {
                sb.Append(" returned exit code ").Append(result.ExitCode).Append(".");
            }
            if (!string.IsNullOrEmpty(stderr))
            {
                sb.Append(" Standard error: ").Append(stderr);
            }
            return new InvalidOperationException(sb.ToString());
        }
    }
}
