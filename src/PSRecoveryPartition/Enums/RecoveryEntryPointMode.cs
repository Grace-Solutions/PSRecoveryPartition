namespace PSRecoveryPartition
{
    /// <summary>
    /// Recovery entry-point modes. <c>BootEntry</c> is the default for
    /// <c>Set-WindowsRecoveryEntryPoint</c>.
    /// </summary>
    public enum RecoveryEntryPointMode
    {
        PushButtonReset,
        BootEntry,
        Both
    }
}
