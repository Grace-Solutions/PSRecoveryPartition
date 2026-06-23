namespace PSRecoveryPartition
{
    /// <summary>
    /// Push-button reset action understood internally. Friendly string values
    /// supplied to <c>-PushButtonAction</c> are normalized to one of these.
    /// </summary>
    public enum WindowsRecoveryPushButtonAction
    {
        Reset,
        Refresh,
        FactoryReset,
        AdvancedStartup,
        BootToRE
    }
}
