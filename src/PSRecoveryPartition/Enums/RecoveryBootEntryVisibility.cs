namespace PSRecoveryPartition
{
    /// <summary>
    /// Boot-entry visibility. Hidden entries are only created when a supported
    /// BCD configuration path is understood and tested.
    /// </summary>
    public enum RecoveryBootEntryVisibility
    {
        Visible,
        Hidden
    }
}
