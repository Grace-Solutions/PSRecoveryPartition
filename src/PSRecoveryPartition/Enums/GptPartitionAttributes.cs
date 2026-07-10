using System;

namespace PSRecoveryPartition
{
    /// <summary>
    /// GPT partition attribute bits, as a flags enum over the native 64-bit mask.
    ///
    /// <para>Because this is an enum, PowerShell binds the member names directly --
    /// no hex literals and no helper variable:</para>
    /// <code>
    /// [PSRecoveryPartition.DiskPartitionSpec]::New('RECOVERY', 'Percentage', 20, 'Recovery', 'Recovery')
    /// [PSRecoveryPartition.DiskPartitionSpec]::New('RECOVERY', 'Percentage', 20, 'Recovery', 'Recovery, Hidden')
    /// [PSRecoveryPartition.GptPartitionAttributes]::Recovery -bor [PSRecoveryPartition.GptPartitionAttributes]::Hidden
    /// </code>
    /// <para>Writing the mask as a raw literal does not work: <c>0x8000000000000001</c>
    /// exceeds <c>[Int64]::MaxValue</c>, so PowerShell coerces it to a negative
    /// number. Use the member names.</para>
    /// </summary>
    [Flags]
    public enum GptPartitionAttributes : ulong
    {
        /// <summary>No attributes.</summary>
        None = 0x0000000000000000UL,

        /// <summary>Partition is required by the platform and must not be deleted (bit 0).</summary>
        PlatformRequired = 0x0000000000000001UL,

        /// <summary>Volume is not automatically mounted when the disk is first seen.</summary>
        NoAutomount = 0x0000000080000000UL,

        /// <summary>Volume is mounted read-only.</summary>
        ReadOnly = 0x1000000000000000UL,

        /// <summary>Volume is a shadow copy of another volume.</summary>
        ShadowCopy = 0x2000000000000000UL,

        /// <summary>Volume is hidden from ordinary enumeration.</summary>
        Hidden = 0x4000000000000000UL,

        /// <summary>No drive letter is assigned to the volume by default.</summary>
        NoDriveLetter = 0x8000000000000000UL,

        /// <summary>
        /// The canonical Microsoft Windows RE recovery mask
        /// (<c>0x8000000000000001</c>): <see cref="PlatformRequired"/> plus
        /// <see cref="NoDriveLetter"/>. Applied automatically to a
        /// <see cref="DiskPartitionKind.Recovery"/> partition.
        /// </summary>
        Recovery = PlatformRequired | NoDriveLetter,
    }
}
