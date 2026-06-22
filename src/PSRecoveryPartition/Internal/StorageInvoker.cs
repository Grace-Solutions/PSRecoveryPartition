using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Thin wrapper around <see cref="Cmdlet.InvokeCommand"/> for running the
    /// in-box Storage module cmdlets. Centralising the call site keeps every
    /// storage-related operation tagged with <see cref="RecoveryExecutionMethod.Storage"/>.
    /// </summary>
    internal sealed class StorageInvoker
    {
        private readonly PSCmdlet _owner;

        public StorageInvoker(PSCmdlet owner)
        {
            if (owner == null) { throw new ArgumentNullException("owner"); }
            _owner = owner;
        }

        public IList<PSObject> Invoke(string commandName, IDictionary parameters)
        {
            if (string.IsNullOrEmpty(commandName)) { throw new ArgumentNullException("commandName"); }
            var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            try
            {
                ps.AddCommand(commandName);
                if (parameters != null)
                {
                    foreach (DictionaryEntry e in parameters)
                    {
                        if (e.Value == null) { continue; }
                        ps.AddParameter(e.Key.ToString(), e.Value);
                    }
                }
                var output = ps.Invoke();
                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        throw new InvalidOperationException(
                            commandName + " failed: " + error.Exception.Message, error.Exception);
                    }
                }
                return output;
            }
            finally
            {
                ps.Dispose();
            }
        }

        public PSObject InvokeSingle(string commandName, IDictionary parameters)
        {
            var results = Invoke(commandName, parameters);
            if (results == null || results.Count == 0) { return null; }
            return results[0];
        }
    }
}
