namespace PSRecoveryPartition
{
    /// <summary>
    /// Recovery entry-point modes. <c>BootEntry</c> is the default for
    /// <c>Set-WindowsRecoveryEntryPoint</c>. <c>None</c> is only meaningful for
    /// recovery partition plans that opt out of entry-point configuration.
    /// </summary>
    public enum RecoveryEntryPointMode
    {
        None,
        PushButtonReset,
        BootEntry,
        Both
    }
}
