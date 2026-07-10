namespace PSRecoveryPartition
{
    /// <summary>
    /// Well-known GPT partition attribute bits, for use with the
    /// <see cref="DiskPartitionSpec"/> constructor overloads that take an explicit
    /// attribute mask.
    ///
    /// <para>These are exposed as <c>ulong</c> constants rather than a flags enum
    /// because the high bits do not survive a PowerShell numeric literal: the mask
    /// <c>0x8000000000000001</c> exceeds <see cref="long">Int64.MaxValue</see>, so
    /// writing it inline coerces to a negative number. Reference the constants
    /// instead:</para>
    /// <code>
    /// [PSRecoveryPartition.GptPartitionAttributes]::Recovery
    /// [PSRecoveryPartition.GptPartitionAttributes]::NoDriveLetter -bor [PSRecoveryPartition.GptPartitionAttributes]::Hidden
    /// </code>
    /// </summary>
    public static class GptPartitionAttributes
    {
        /// <summary>No attributes.</summary>
        public const ulong None = 0x0000000000000000UL;

        /// <summary>Partition is required by the platform and must not be deleted (bit 0).</summary>
        public const ulong PlatformRequired = 0x0000000000000001UL;

        /// <summary>Volume is not automatically mounted when the disk is first seen.</summary>
        public const ulong NoAutomount = 0x0000000080000000UL;

        /// <summary>Volume is mounted read-only.</summary>
        public const ulong ReadOnly = 0x1000000000000000UL;

        /// <summary>Volume is a shadow copy of another volume.</summary>
        public const ulong ShadowCopy = 0x2000000000000000UL;

        /// <summary>Volume is hidden from ordinary enumeration.</summary>
        public const ulong Hidden = 0x4000000000000000UL;

        /// <summary>No drive letter is assigned to the volume by default.</summary>
        public const ulong NoDriveLetter = 0x8000000000000000UL;

        /// <summary>
        /// The canonical Microsoft Windows RE recovery partition mask
        /// (<c>0x8000000000000001</c>): platform-required plus no drive letter.
        /// This is what a <see cref="DiskPartitionKind.Recovery"/> partition
        /// receives when no explicit mask is supplied.
        /// </summary>
        public const ulong Recovery = PlatformRequired | NoDriveLetter;
    }
}
