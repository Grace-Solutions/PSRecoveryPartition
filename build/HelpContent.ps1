@{
    'Get-RecoveryPartition' = @{
        Synopsis    = 'Discovers recovery partitions on local disks.'
        Description = 'Enumerates physical disks and emits a RecoveryPartitionInfo object for each partition that carries the Windows recovery partition type GUID. Use -IncludeNonRecovery to widen the search to non-recovery partitions for diagnostics.'
        OneLiner    = 'Get-RecoveryPartition -DiskNumber 0'
        OneLinerDescription = 'Lists every recovery partition on disk 0.'
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
        Description = 'Creates a recovery partition on the specified disk and optionally stages a WindowsRE image into it. Use -SizeBytes for explicit sizing or -SizePercent for percentage-based sizing. The two are mutually exclusive parameter sets.'
        OneLiner    = "New-RecoveryPartition -DiskNumber 0 -SizeBytes 1073741824 -WindowsREImagePath 'C:\RecoveryImages\Winre.wim' -PassThru"
        OneLinerDescription = 'Creates a 1 GiB recovery partition on disk 0 and stages the supplied WindowsRE image.'
        Splat       = @'
$NewRecoveryPartitionParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $NewRecoveryPartitionParameters.DiskNumber = 0
    $NewRecoveryPartitionParameters.SizePercent = 2
    $NewRecoveryPartitionParameters.Label = 'Windows RE tools'
    $NewRecoveryPartitionParameters.FileSystem = 'NTFS'
    $NewRecoveryPartitionParameters.WindowsREImagePath = 'C:\RecoveryImages\winre.wim'
    $NewRecoveryPartitionParameters.PassThru = $True
    $NewRecoveryPartitionParameters.Verbose = $True

$NewRecoveryPartitionResult = New-RecoveryPartition @NewRecoveryPartitionParameters
Write-Output -InputObject ($NewRecoveryPartitionResult)
'@
        SplatDescription = 'Creates a percentage-sized recovery partition with a friendly label and stages a WindowsRE image.'
    }
    'Set-RecoveryPartition' = @{
        Synopsis    = 'Updates the metadata of an existing recovery partition.'
        Description = 'Updates editable metadata such as the friendly label of an existing recovery partition.'
        OneLiner    = "Set-RecoveryPartition -DiskNumber 0 -PartitionNumber 5 -Label 'Windows RE tools'"
        OneLinerDescription = 'Renames the recovery partition on disk 0 partition 5.'
    }
    'Resize-RecoveryPartition' = @{
        Synopsis    = 'Resizes a recovery partition to the requested size.'
        Description = 'Resizes a recovery partition. Use -SizeBytes for explicit sizing or -SizePercent for percentage-based sizing. The two are mutually exclusive parameter sets.'
        OneLiner    = 'Resize-RecoveryPartition -DiskNumber 0 -PartitionNumber 5 -SizeBytes 2147483648'
        OneLinerDescription = 'Resizes the recovery partition on disk 0 partition 5 to 2 GiB.'
    }
    'Remove-RecoveryPartition' = @{
        Synopsis    = 'Removes a recovery partition.'
        Description = 'Removes a recovery partition. Requires -Force or explicit confirmation because the operation is destructive.'
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
    'Get-RecoveryPartitionPlan' = @{
        Synopsis    = 'Produces an idempotent plan for the requested recovery layout.'
        Description = 'Compares the current disk layout to the requested recovery layout and returns a structured plan describing the steps required to converge.'
        OneLiner    = 'Get-RecoveryPartitionPlan -DiskNumber 0 -SizePercent 2'
        OneLinerDescription = 'Returns the plan required to provision a 2 percent recovery partition on disk 0.'
        Splat       = @'
$GetRecoveryPartitionPlanParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $GetRecoveryPartitionPlanParameters.DiskNumber = 0
    $GetRecoveryPartitionPlanParameters.SizeBytes = 1073741824
    $GetRecoveryPartitionPlanParameters.WindowsREImagePath = 'C:\RecoveryImages\winre.wim'
    $GetRecoveryPartitionPlanParameters.Verbose = $True

$GetRecoveryPartitionPlanResult = Get-RecoveryPartitionPlan @GetRecoveryPartitionPlanParameters
Write-Output -InputObject ($GetRecoveryPartitionPlanResult)
'@
        SplatDescription = 'Builds a plan to create a 1 GiB recovery partition staged with the supplied WindowsRE image.'
    }
    'Invoke-RecoveryPartitionPlan' = @{
        Synopsis    = 'Executes a recovery partition plan.'
        Description = 'Executes the steps in a recovery partition plan produced by Get-RecoveryPartitionPlan. Honours -WhatIf and -Confirm and returns a structured result.'
        OneLiner    = 'Get-RecoveryPartitionPlan -DiskNumber 0 -SizePercent 2 | Invoke-RecoveryPartitionPlan -PassThru'
        OneLinerDescription = 'Builds a plan and applies it on disk 0 then emits the plan result.'
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
        Synopsis    = 'Creates a recovery boot entry idempotently.'
        Description = 'Creates a BCD boot entry that boots from the supplied WIM image. Existing entries with the same name are not duplicated.'
        OneLiner    = "New-WindowsRecoveryBootEntry -BootImagePath 'C:\RecoveryImages\boot.wim' -Name 'Recovery' -PassThru"
        OneLinerDescription = 'Creates a recovery boot entry that boots boot.wim.'
    }
    'Remove-WindowsRecoveryBootEntry' = @{
        Synopsis    = 'Removes a recovery boot entry idempotently.'
        Description = 'Removes a BCD boot entry by identifier, name, or pipeline input. Requires -Force or explicit confirmation because the operation is destructive.'
        OneLiner    = "Remove-WindowsRecoveryBootEntry -Name 'Recovery' -Force"
        OneLinerDescription = 'Removes the recovery boot entry with the friendly name "Recovery".'
    }
    'Set-WindowsRecoveryEntryPoint' = @{
        Synopsis    = 'Configures push-button reset, a boot entry, or both as recovery entry points.'
        Description = 'Combines WindowsRE registration and BCD boot entry creation behind a single high-level surface. EntryPointMode selects which paths are configured.'
        OneLiner    = "Set-WindowsRecoveryEntryPoint -EntryPointMode Both -BootImagePath 'C:\RecoveryImages\boot.wim' -WindowsREImagePath 'C:\RecoveryImages\winre.wim' -BootTimeout ([timespan]'00:00:10') -PassThru"
        OneLinerDescription = 'Configures both push-button reset and a recovery boot entry in a single call.'
        Splat       = @'
$SetWindowsRecoveryEntryPointParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $SetWindowsRecoveryEntryPointParameters.EntryPointMode = 'Both'
    $SetWindowsRecoveryEntryPointParameters.BootImagePath = 'C:\RecoveryImages\boot.wim'
    $SetWindowsRecoveryEntryPointParameters.WindowsREImagePath = 'C:\RecoveryImages\winre.wim'
    $SetWindowsRecoveryEntryPointParameters.PushButtonAction = 'BootToRE'
    $SetWindowsRecoveryEntryPointParameters.Name = 'Grace Solutions Recovery'
    $SetWindowsRecoveryEntryPointParameters.BootTimeout = [Timespan]::FromSeconds(10)
    $SetWindowsRecoveryEntryPointParameters.BootEntryVisibility = 'Visible'
    $SetWindowsRecoveryEntryPointParameters.PassThru = $True
    $SetWindowsRecoveryEntryPointParameters.Verbose = $True

$SetWindowsRecoveryEntryPointResult = Set-WindowsRecoveryEntryPoint @SetWindowsRecoveryEntryPointParameters
Write-Output -InputObject ($SetWindowsRecoveryEntryPointResult)
'@
        SplatDescription = 'Splatted OrderedDictionary example for the combined entry-point configuration.'
    }
}
