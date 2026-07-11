using System;

namespace PSRecoveryPartition
{
    /// <summary>
    /// One entry in a custom disk layout. Entries are processed in list order and
    /// each takes either an absolute size or a percentage of the free space that
    /// remains after the preceding entries.
    ///
    /// <para>Constructible from PowerShell:</para>
    /// <code>
    /// $Layout = New-Object 'System.Collections.Generic.List[PSRecoveryPartition.DiskPartitionSpec]'
    /// $Layout.Add([PSRecoveryPartition.DiskPartitionSpec]::new('EFI',      'Size',       1GB))
    /// $Layout.Add([PSRecoveryPartition.DiskPartitionSpec]::new('MSR',      'Size',       1GB))
    /// $Layout.Add([PSRecoveryPartition.DiskPartitionSpec]::new('RECOVERY', 'Percentage', 20))
    /// $Layout.Add([PSRecoveryPartition.DiskPartitionSpec]::new('OS',       'Percentage', 100))
    /// </code>
    /// <para>The three-argument form infers <see cref="Kind"/> from the label
    /// (EFI/SYSTEM, MSR/RESERVED, RECOVERY/WINRE; anything else is Basic). Pass
    /// the kind explicitly to override, and optionally GPT attributes and a file
    /// system.</para>
    /// </summary>
    public sealed class DiskPartitionSpec
    {
        private const long OneGiB = 1024L * 1024L * 1024L;

        /// <summary>Volume label. Also the GPT partition name.</summary>
        public string Label { get; set; }

        /// <summary>Whether <see cref="Value"/> is bytes or a percentage of remaining free space.</summary>
        public DiskPartitionSizeMode SizeMode { get; set; }

        /// <summary>Size in bytes when <see cref="SizeMode"/> is Size; 1-100 when Percentage.</summary>
        public long Value { get; set; }

        /// <summary>Partition role, which drives the partition type and attributes.</summary>
        public DiskPartitionKind Kind { get; set; }

        /// <summary>File system to format with. Null selects the default for the kind (FAT32 for EFI, NTFS otherwise). MSR is never formatted.</summary>
        public DiskFileSystem? FileSystem { get; set; }

        /// <summary>Explicit GPT attribute mask. When not supplied the default for the kind is used.</summary>
        public GptPartitionAttributes GptAttributes { get; set; }

        /// <summary>True when <see cref="GptAttributes"/> was supplied explicitly.</summary>
        public bool HasExplicitGptAttributes { get; set; }

        /// <summary>The GPT partition type this entry receives once fully tagged, derived from <see cref="Kind"/>.</summary>
        public GptPartitionType GptType { get { return DiskEnumMaps.ToGptType(Kind); } }

        public DiskPartitionSpec() { }

        public DiskPartitionSpec(string label, DiskPartitionSizeMode sizeMode, long value)
            : this(label, sizeMode, value, InferKind(label)) { }

        public DiskPartitionSpec(string label, DiskPartitionSizeMode sizeMode, long value, DiskPartitionKind kind)
        {
            Label = label;
            SizeMode = sizeMode;
            Value = value;
            Kind = kind;
            Validate();
        }

        /// <summary>
        /// Attributes are taken as an array so they read naturally from PowerShell:
        /// <c>@('Recovery','Hidden')</c>. A single value binds too (PowerShell wraps
        /// a scalar into a one-element array), as does a pre-combined mask built
        /// with <c>-bor</c>. The values are OR-ed together.
        /// </summary>
        public DiskPartitionSpec(string label, DiskPartitionSizeMode sizeMode, long value, DiskPartitionKind kind, GptPartitionAttributes[] gptAttributes)
            : this(label, sizeMode, value, kind)
        {
            GptAttributes = Combine(gptAttributes);
            HasExplicitGptAttributes = true;
        }

        public DiskPartitionSpec(string label, DiskPartitionSizeMode sizeMode, long value, DiskPartitionKind kind, GptPartitionAttributes[] gptAttributes, DiskFileSystem fileSystem)
            : this(label, sizeMode, value, kind, gptAttributes)
        {
            FileSystem = fileSystem;
        }

        // No (label, sizeMode, value, kind, DiskFileSystem) overload: it would be
        // ambiguous with the attributes overload above, since PowerShell ranks the
        // string-to-enum conversions identically for both. To set a file system
        // without an explicit mask, pass @('None') to the six-argument form, or
        // assign the FileSystem property.

        private static GptPartitionAttributes Combine(GptPartitionAttributes[] values)
        {
            var combined = GptPartitionAttributes.None;
            if (values != null)
            {
                foreach (var v in values) { combined |= v; }
            }
            return combined;
        }

        private void Validate()
        {
            if (SizeMode == DiskPartitionSizeMode.Percentage)
            {
                if (Value < 1 || Value > 100)
                {
                    throw new ArgumentOutOfRangeException("value", Value,
                        "A Percentage size must be between 1 and 100.");
                }
            }
            else if (Value <= 0)
            {
                throw new ArgumentOutOfRangeException("value", Value,
                    "A Size must be greater than zero bytes.");
            }
        }

        /// <summary>
        /// Maps a conventional label to a partition role so the short constructor
        /// does the obvious thing for the standard EFI / MSR / RECOVERY / OS names.
        /// </summary>
        internal static DiskPartitionKind InferKind(string label)
        {
            if (string.IsNullOrEmpty(label)) { return DiskPartitionKind.Basic; }
            var l = label.Trim();
            if (Eq(l, "EFI") || Eq(l, "ESP") || Eq(l, "SYSTEM")) { return DiskPartitionKind.Efi; }
            if (Eq(l, "MSR") || Eq(l, "RESERVED")) { return DiskPartitionKind.Msr; }
            if (Eq(l, "RECOVERY") || Eq(l, "WINRE")) { return DiskPartitionKind.Recovery; }
            return DiskPartitionKind.Basic;
        }

        private static bool Eq(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Default 1 GiB EFI System Partition entry.</summary>
        internal static DiskPartitionSpec Efi() { return new DiskPartitionSpec("EFI", DiskPartitionSizeMode.Size, OneGiB, DiskPartitionKind.Efi); }

        /// <summary>Default 1 GiB Microsoft Reserved entry.</summary>
        internal static DiskPartitionSpec Msr() { return new DiskPartitionSpec("MSR", DiskPartitionSizeMode.Size, OneGiB, DiskPartitionKind.Msr); }

        public override string ToString()
        {
            return Label + " (" + Kind + ", " +
                (SizeMode == DiskPartitionSizeMode.Percentage ? Value + "% of remaining" : Value + " bytes") + ")";
        }
    }
}
