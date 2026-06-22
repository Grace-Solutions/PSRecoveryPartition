using System;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Identifies which implementation path was used to satisfy a recovery operation.
    /// Reflected on every public result object as <c>ExecutionMethod</c>. The module
    /// now produces only <see cref="Native"/> or <see cref="ProcessFallback"/>; the
    /// <see cref="Storage"/>, <see cref="CIM"/>, and <see cref="WMI"/> values are
    /// retained for binary / source compatibility and will be removed in a future
    /// major version.
    /// </summary>
    public enum RecoveryExecutionMethod
    {
        Native,

        [Obsolete("The Storage module path was removed in Phase 17. The engine now drives Win32 IOCTLs directly and never reports Storage. This member will be removed in a future major version.", false)]
        Storage,

        [Obsolete("The CIM path was never produced by the shipping engine and is removed in the IOCTL-primary architecture. This member will be removed in a future major version.", false)]
        CIM,

        [Obsolete("The WMI path was never produced by the shipping engine and is removed in the IOCTL-primary architecture. This member will be removed in a future major version.", false)]
        WMI,

        ProcessFallback
    }
}
