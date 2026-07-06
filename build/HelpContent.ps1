@{
    '__Common' = @{
        DiskNumber              = 'Number of the physical disk to operate on, as reported by Get-Disk.'
        PartitionNumber         = 'Number of the partition on the target disk, as reported by Get-Partition.'
        SizeBytes               = 'Explicit partition size in bytes. Mutually exclusive with -SizePercent.'
        SizePercent              = 'Partition size expressed as a percentage of the target disk size. Mutually exclusive with -SizeBytes.'
        Label                   = 'File system label assigned to the recovery volume. Defaults to RECOVERY.'
        FileSystem              = 'File system used to format the recovery partition. Defaults to NTFS.'
        CreationMode            = 'Placement strategy. The recovery partition is always created after the existing partitions (never before the OS). UseTrailingFreeSpace (default) appends into existing free space at the end of the disk and never moves or resizes anything, failing if there is not enough room. ShrinkToFit shrinks the last partition (typically the OS) by the shortfall to free trailing space first. RequireEmptyDisk only creates on a disk with no partitions.'
        WindowsREImagePath      = 'Path to a WindowsRE WIM image that should be staged into the recovery partition.'
        BootImagePath           = 'Path to a boot WIM image used by the BCD recovery entry.'
        BootEntryName           = 'Friendly name applied to the recovery BCD boot entry.'
        BootEntryVisibility     = 'Whether the recovery BCD boot entry is Visible or Hidden in the boot menu.'
        BootTimeout             = 'Boot menu timeout applied when a new recovery boot entry is configured.'
        EntryPointMode          = 'Selects which recovery entry points to configure: PushButton, BootEntry, or Both.'
        PushButtonAction        = 'Friendly action keyword translated to a Windows recovery push-button reset action (for example Reset, Refresh, FactoryReset, BootToRE).'
        RecoveryPartition       = 'Recovery partition (RecoveryPartitionInfo, typically piped from Get-RecoveryPartition) to stage the boot image onto and target with the BCD entry. Files are written through the partition''s \\?\GLOBALROOT path without assigning a drive letter or mounting the volume.'
        TargetPath              = 'An already-mounted directory to stage the boot image into. Use this when the destination volume has a drive letter or mount point.'
        StagingRelativePath     = 'Volume-relative folder the image (and boot.sdi) are staged into. Defaults to \Recovery\WindowsRE; pass an empty string or a single backslash (\) to stage at the volume root.'
        ExpandBootImage         = 'Expands the boot image flat onto the destination (non-RAM / flat boot) and wires the entry to boot it in place, instead of staging the WIM for ramdisk boot. Requires a destination (-RecoveryPartition or -TargetPath).'
        ImageIndex              = 'One-based image index inside the WIM to expand when -ExpandBootImage is used. Defaults to 1.'
        BootSdiPath             = 'Explicit boot.sdi to stage for ramdisk boot. When omitted it is resolved from the live OS and then extracted from the boot image.'
        AccessPath              = 'Folder mount path (for example C:\Mounts\Recovery) used as an access path for the recovery partition.'
        NoDefaultDriveLetter    = 'When set, prevents Windows from automatically assigning a drive letter to the partition.'
        IsHidden                = 'When set, marks the partition as hidden so it is omitted from common UI surfaces.'
        IncludeNonRecovery      = 'When set, the discovery surface also returns partitions that do not look like recovery partitions; useful for diagnostics.'
        DetectionMode           = 'Scopes discovery to a set of disks: CurrentOSDisk (default; the disk hosting the running OS), AllDisks, or SecondaryDisksOnly (every disk except the OS disk). Prevents recovery/BCD operations from fanning out across every disk on dual-disk or dual-boot systems. Ignored when -DiskNumber is supplied.'
        IncludeAll              = 'When set, returns every BCD entry instead of only those that match recovery heuristics.'
        IncludeHidden           = 'When set, returns boot entries that are flagged as hidden in BCD.'
        Path                    = 'File-system path to search for WIM images.'
        ImageKind               = 'Image classification (WindowsRE, WindowsPE, BootWim) used when tagging the result.'
        ComputeHash             = 'When set, computes a SHA-256 hash of each discovered image. Slower but useful for change detection.'
        InputObject             = 'Object received from the pipeline that the cmdlet should act on.'
        Identifier              = 'BCD GUID identifier of the target boot entry.'
        Name                    = 'Friendly display name of the target boot entry.'
        DestinationPath         = 'Target file or directory path. When it names an existing directory or ends with a path separator the source leaf name is appended; otherwise the value is treated as the full destination file path and the source is renamed on copy.'
        SourceImagePath         = 'Source WIM file that should be copied or staged.'
        SourcePath              = 'Source file path on a local or UNC volume used as the input to the copy.'
        SourceUri               = 'HTTPS URI from which the recovery image should be downloaded.'
        BootToRE                = 'When set, schedules the system to boot into the Windows Recovery Environment on the next restart.'
        Target                  = 'Reagentc target identifier used when registering or scheduling a WindowsRE boot.'
        AddLast                 = 'When set, appends the new BCD entry to the end of the boot order instead of the default position.'
        SetDefault              = 'When set, marks the new boot entry as the default in BCD.'
        SetDefaultBootEntry     = 'When set in a plan, instructs the plan to make the recovery BCD entry the default boot entry.'
        Force                   = 'Suppresses interactive prompts and overrides safety refusals that would otherwise block destructive or risky changes.'
        PassThru                = 'Returns the resulting object after the operation completes. By default the cmdlet returns nothing on success.'
    }
    'Get-RecoveryPartition' = @{
        Synopsis    = 'Discovers recovery partitions on local disks.'
        Description = 'Enumerates physical disks and emits a RecoveryPartitionInfo object for each partition that carries the Windows recovery partition type GUID. Use -IncludeNonRecovery to widen the search to non-recovery partitions for diagnostics.'
        OneLiner    = 'Get-RecoveryPartition -DiskNumber 0'
        OneLinerDescription = 'Lists every recovery partition on disk 0.'
        Parameters  = @{
            DiskNumber      = 'Optional disk filter. When omitted, every physical disk is searched.'
            PartitionNumber = 'Optional partition filter applied to the results of the disk-level enumeration.'
        }
        Splat       = @'
$GetRecoveryPartitionParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $GetRecoveryPartitionParameters.DiskNumber = 0
    $GetRecoveryPartitionParameters.IncludeNonRecovery = $False
    $GetRecoveryPartitionParameters.Verbose = $True

$GetRecoveryPartitionResult = Get-RecoveryPartition @GetRecoveryPartitionParameters

Write-Output -InputObject ($GetRecoveryPartitionResult)
'@
        SplatDescription = 'Discovers recovery partitions on disk 0 using a splatted OrderedDictionary parameter set.'
    }
    'New-RecoveryPartition' = @{
        Synopsis    = 'Creates a recovery partition on a target disk.'
        Description = 'Creates and formats a recovery partition on the specified disk. Use -SizeBytes for explicit sizing or -SizePercent for percentage-based sizing (mutually exclusive parameter sets). The partition is always placed after the existing partitions; -CreationMode controls how trailing free space is obtained. Image and boot-entry payloads are staged by the dedicated cmdlets (Set-WindowsRecoveryImage, New-WindowsRecoveryBootEntry).'
        OneLiner    = "New-RecoveryPartition -DiskNumber 0 -SizeBytes 1073741824 -PassThru"
        OneLinerDescription = 'Creates a 1 GiB recovery partition on disk 0 using existing trailing free space.'
        Splat       = @'
$NewRecoveryPartitionParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $NewRecoveryPartitionParameters.DiskNumber   = 0
    $NewRecoveryPartitionParameters.SizePercent  = 2
    $NewRecoveryPartitionParameters.Label        = 'RECOVERY'
    $NewRecoveryPartitionParameters.FileSystem   = 'NTFS'
    $NewRecoveryPartitionParameters.CreationMode = 'ShrinkToFit'
    $NewRecoveryPartitionParameters.PassThru     = $True
    $NewRecoveryPartitionParameters.Verbose      = $True

$NewRecoveryPartitionResult = New-RecoveryPartition @NewRecoveryPartitionParameters

Write-Output -InputObject ($NewRecoveryPartitionResult)
'@
        SplatDescription = 'Creates a percentage-sized recovery partition, shrinking the last partition to make room if the disk has no trailing free space.'
    }
    'Set-RecoveryPartition' = @{
        Synopsis    = 'Updates the metadata of an existing recovery partition.'
        Description = 'Updates editable metadata such as the friendly label of an existing recovery partition.'
        OneLiner    = "Set-RecoveryPartition -DiskNumber 0 -PartitionNumber 5 -Label 'Windows RE tools'"
        OneLinerDescription = 'Renames the recovery partition on disk 0 partition 5.'
    }
    'Resize-RecoveryPartition' = @{
        Synopsis    = 'Resizes a recovery partition to the requested size.'
        Description = 'Resizes a recovery partition. Use -SizeBytes for explicit sizing or -SizePercent for percentage-based sizing. The two are mutually exclusive parameter sets. The cmdlet inspects the surrounding partition layout and refuses to grow when there is not enough trailing free space unless -Force is supplied.'
        OneLiner    = 'Resize-RecoveryPartition -DiskNumber 0 -PartitionNumber 5 -SizeBytes 2147483648'
        OneLinerDescription = 'Resizes the recovery partition on disk 0 partition 5 to 2 GiB.'
    }
    'Remove-RecoveryPartition' = @{
        Synopsis    = 'Removes a recovery partition.'
        Description = 'Removes a recovery partition. Requires -Force or explicit confirmation because the operation is destructive. The cmdlet inspects the surrounding partition layout and refuses to remove when the immediately following partition is the OS partition unless -Force is supplied.'
        OneLiner    = 'Remove-RecoveryPartition -DiskNumber 0 -PartitionNumber 5 -Force'
        OneLinerDescription = 'Removes the recovery partition on disk 0 partition 5 without prompting.'
    }
    'Mount-RecoveryPartition' = @{
        Synopsis    = 'Mounts a recovery partition at a folder mount point.'
        Description = 'Assigns a folder mount point so the recovery partition can be inspected from Explorer or other tooling.'
        OneLiner    = "Mount-RecoveryPartition -DiskNumber 0 -PartitionNumber 5 -MountPath 'C:\Mounts\Recovery'"
        OneLinerDescription = 'Mounts the recovery partition on disk 0 partition 5 at the specified folder.'
    }
    'Dismount-RecoveryPartition' = @{
        Synopsis    = 'Removes a folder mount point from a recovery partition.'
        Description = 'Removes the specified folder mount point from a previously mounted recovery partition.'
        OneLiner    = "Dismount-RecoveryPartition -MountPath 'C:\Mounts\Recovery'"
        OneLinerDescription = 'Dismounts the recovery partition mount point at the specified folder.'
    }
    'Test-RecoveryPartition' = @{
        Synopsis    = 'Tests a recovery partition for size, layout, and contents.'
        Description = 'Inspects a recovery partition and returns a structured pass/fail report covering size, layout, and presence of a WindowsRE image.'
        OneLiner    = 'Test-RecoveryPartition -DiskNumber 0 -PartitionNumber 5'
        OneLinerDescription = 'Validates the recovery partition on disk 0 partition 5.'
    }
    'Get-WindowsRecoveryImage' = @{
        Synopsis    = 'Discovers Windows RE or Windows PE image files.'
        Description = 'Enumerates WIM files in the supplied path and returns structured information about each candidate recovery image.'
        OneLiner    = "Get-WindowsRecoveryImage -Path 'C:\RecoveryImages'"
        OneLinerDescription = 'Lists WIM files in the specified folder.'
    }
    'Set-WindowsRecoveryImage' = @{
        Synopsis    = 'Copies or updates a Windows RE or Windows PE image.'
        Description = 'Copies the supplied source image to a destination folder. The copy is skipped when the destination already matches the source by size and last-write timestamp.'
        OneLiner    = "Set-WindowsRecoveryImage -SourceImagePath 'C:\RecoveryImages\winre.wim' -DestinationPath 'D:\Recovery\winre.wim' -PassThru"
        OneLinerDescription = 'Copies winre.wim to the recovery volume and returns the staged image record. When DestinationPath ends with a directory separator or names an existing directory, the source leaf name is appended; otherwise the destination is treated as a full file path and the source is renamed on copy.'
    }
    'Save-RecoveryBootImage' = @{
        Synopsis    = 'Downloads or copies a recovery boot image to a local destination.'
        Description = 'Downloads a WIM from an HTTPS source or copies it from a local or UNC path to the supplied destination. When DestinationPath names an existing directory or ends with a directory separator the source leaf name is appended; otherwise it is treated as the target file path. Idempotent when the destination already matches the source.'
        OneLiner    = "Save-RecoveryBootImage -SourceUri 'https://example.com/boot.wim' -DestinationPath 'C:\RecoveryImages\boot.wim'"
        OneLinerDescription = 'Downloads the boot image from the supplied URI to C:\RecoveryImages\boot.wim.'
    }
    'Get-WindowsRecoveryEnvironment' = @{
        Synopsis    = 'Returns the current Windows Recovery Environment configuration.'
        Description = 'Queries the active Windows Recovery Environment configuration and returns a structured WindowsRecoveryEnvironmentInfo object.'
        OneLiner    = 'Get-WindowsRecoveryEnvironment'
        OneLinerDescription = 'Returns the current WinRE configuration.'
    }
    'Set-WindowsRecoveryEnvironment' = @{
        Synopsis    = 'Registers a WindowsRE image and/or schedules a boot to RE.'
        Description = 'Registers a WindowsRE image with the operating system and optionally schedules a boot into Windows RE on the next restart.'
        OneLiner    = "Set-WindowsRecoveryEnvironment -WindowsREImagePath 'C:\RecoveryImages\winre.wim' -PassThru"
        OneLinerDescription = 'Registers the supplied WindowsRE image and returns the updated configuration.'
    }
    'Enable-WindowsRecoveryEnvironment' = @{
        Synopsis    = 'Enables Windows Recovery Environment.'
        Description = 'Enables the Windows Recovery Environment and returns the resulting configuration.'
        OneLiner    = 'Enable-WindowsRecoveryEnvironment -PassThru'
        OneLinerDescription = 'Enables WinRE and returns the new state.'
    }
    'Disable-WindowsRecoveryEnvironment' = @{
        Synopsis    = 'Disables Windows Recovery Environment.'
        Description = 'Disables the Windows Recovery Environment. Requires -Force or explicit confirmation because the operation is high-impact.'
        OneLiner    = 'Disable-WindowsRecoveryEnvironment -Force -PassThru'
        OneLinerDescription = 'Disables WinRE without prompting and returns the new state.'
    }
    'Get-WindowsRecoveryBootEntry' = @{
        Synopsis    = 'Discovers recovery boot entries.'
        Description = 'Enumerates Boot Configuration Data entries and returns those that look like recovery boot entries. Use -IncludeAll to return every entry regardless of recovery heuristics.'
        OneLiner    = 'Get-WindowsRecoveryBootEntry'
        OneLinerDescription = 'Lists the recovery boot entries currently registered in BCD.'
    }
    'New-WindowsRecoveryBootEntry' = @{
        Synopsis    = 'Creates a custom recovery boot entry from a boot image.'
        Description = 'Stages a boot image onto a destination and creates a matching BCD boot entry. The destination is a recovery partition (written through its \\?\GLOBALROOT path with no mount), an already-mounted directory (-TargetPath), or the image''s existing location (the default ByImagePath set). By default the WIM is staged as-is and booted as a ramdisk, and a boot.sdi is resolved automatically (or supplied with -BootSdiPath); with -ExpandBootImage the image is expanded flat onto the destination for non-RAM boot and the entry is wired to boot it in place. The staging sub-path defaults to \Recovery\WindowsRE but is fully overridable via -StagingRelativePath. Existing entries with the same name are not duplicated.'
        OneLiner    = "Get-RecoveryPartition | New-WindowsRecoveryBootEntry -BootImagePath 'C:\RecoveryImages\winre.wim' -PassThru"
        OneLinerDescription = 'Stages winre.wim onto the recovery partition and creates a ramdisk recovery boot entry.'
    }
    'Remove-WindowsRecoveryBootEntry' = @{
        Synopsis    = 'Removes a recovery boot entry idempotently.'
        Description = 'Removes a BCD boot entry by identifier, name, or pipeline input. Requires -Force or explicit confirmation because the operation is destructive.'
        OneLiner    = "Remove-WindowsRecoveryBootEntry -Name 'Recovery' -Force"
        OneLinerDescription = 'Removes the recovery boot entry with the friendly name "Recovery".'
    }
    'Set-WindowsRecoveryEntryPoint' = @{
        Synopsis    = 'Configures Windows RE / push-button reset as a recovery entry point.'
        Description = 'Optionally registers a WindowsRE image, enables Windows RE, and schedules a boot into Windows RE on the next restart. Custom BCD boot entries are created by New-WindowsRecoveryBootEntry; this cmdlet no longer creates them, so boot-entry creation has a single, unambiguous path. -EntryPointMode BootEntry or Both is rejected and redirected to New-WindowsRecoveryBootEntry.'
        OneLiner    = 'Set-WindowsRecoveryEntryPoint -EntryPointMode PushButtonReset -PushButtonAction BootToRE -PassThru'
        OneLinerDescription = 'Enables Windows RE and schedules a boot into it on the next restart.'
        Splat       = @'
$SetWindowsRecoveryEntryPointParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $SetWindowsRecoveryEntryPointParameters.EntryPointMode = 'PushButtonReset'
    $SetWindowsRecoveryEntryPointParameters.WindowsREImagePath = 'C:\RecoveryImages\winre.wim'
    $SetWindowsRecoveryEntryPointParameters.PushButtonAction = 'BootToRE'
    $SetWindowsRecoveryEntryPointParameters.PassThru = $True
    $SetWindowsRecoveryEntryPointParameters.Verbose = $True

$SetWindowsRecoveryEntryPointResult = Set-WindowsRecoveryEntryPoint @SetWindowsRecoveryEntryPointParameters
Write-Output -InputObject ($SetWindowsRecoveryEntryPointResult)
'@
        SplatDescription = 'Splatted OrderedDictionary example for push-button / Windows RE configuration.'
    }
}
