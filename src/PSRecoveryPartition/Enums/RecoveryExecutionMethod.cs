namespace PSRecoveryPartition
{
    /// <summary>
    /// Identifies which implementation path was used to satisfy a recovery operation.
    /// Reflected on every public result object as <c>ExecutionMethod</c>.
    /// </summary>
    public enum RecoveryExecutionMethod
    {
        Native,
        Storage,
        CIM,
        WMI,
        ProcessFallback
    }
}
