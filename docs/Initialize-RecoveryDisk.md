---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Initialize-RecoveryDisk

## SYNOPSIS
Erases a disk and lays down a complete partition set.

## SYNTAX

### Preset (Default)
```
Initialize-RecoveryDisk [-DiskNumber] <Int32> [-PartitionScheme <DiskPartitionScheme>]
 [-PartitionLayoutPreset <DiskPartitionLayoutPreset>] [-Force] [-PassThru] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Custom
```
Initialize-RecoveryDisk [-DiskNumber] <Int32> [-PartitionScheme <DiskPartitionScheme>]
 -PartitionLayout <DiskPartitionSpec[]> [-Force] [-PassThru] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Rewrites the partition table on the target disk, creates every partition in order, and formats each one. Use -PartitionLayoutPreset for a named layout or -PartitionLayout for a custom ordered list of DiskPartitionSpec entries. Percentage entries are taken against the free space remaining at that point, so a trailing 100% entry consumes the rest of the disk. All work is done through native disk IOCTLs and fmifs!FormatEx; diskpart and format.com are never invoked. This is destructive and irreversible: the disk hosting the running operating system is always refused, so run from Windows PE to lay out the system disk.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Initialize-RecoveryDisk -DiskNumber 1 -PartitionScheme GPT -PartitionLayoutPreset RecoveryLast -Force -PassThru
```

Erases disk 1 and creates EFI (1 GiB), MSR (1 GiB), OS (80% of remaining), and RECOVERY (the rest).

### Example 2: Splatted parameters with OrderedDictionary
```powershell
$Attr = [PSRecoveryPartition.GptPartitionAttributes]

$PartitionLayout = New-Object -TypeName 'System.Collections.Generic.List[PSRecoveryPartition.DiskPartitionSpec]'
    $PartitionLayout.Add([PSRecoveryPartition.DiskPartitionSpec]::New('EFI',      'Size',       1GB))
    $PartitionLayout.Add([PSRecoveryPartition.DiskPartitionSpec]::New('MSR',      'Size',       1GB))
    $PartitionLayout.Add([PSRecoveryPartition.DiskPartitionSpec]::New('RECOVERY', 'Percentage', 20, 'Recovery', ($Attr::Recovery -bor $Attr::Hidden)))
    $PartitionLayout.Add([PSRecoveryPartition.DiskPartitionSpec]::New('OS',       'Percentage', 100))

$InitializeRecoveryDiskParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $InitializeRecoveryDiskParameters.DiskNumber      = 1
    $InitializeRecoveryDiskParameters.PartitionScheme = 'GPT'
    $InitializeRecoveryDiskParameters.PartitionLayout = $PartitionLayout
    $InitializeRecoveryDiskParameters.Force           = $True
    $InitializeRecoveryDiskParameters.PassThru        = $True
    $InitializeRecoveryDiskParameters.Verbose         = $True

$InitializeRecoveryDiskResult = Initialize-RecoveryDisk @InitializeRecoveryDiskParameters

Write-Output -InputObject ($InitializeRecoveryDiskResult)
```

Erases disk 1 and applies a custom ordered layout: a 1 GiB EFI system partition, a 1 GiB Microsoft Reserved partition, a recovery partition sized at 20% of the remaining space and tagged with the recovery attribute mask plus the hidden bit, and an OS partition that consumes the rest.

### Example 3: Custom recovery GPT attributes
```powershell
$Attr = [PSRecoveryPartition.GptPartitionAttributes]

# The default a Recovery partition already receives (PlatformRequired + NoDriveLetter).
[PSRecoveryPartition.DiskPartitionSpec]::New('RECOVERY', 'Percentage', 20, 'Recovery', $Attr::Recovery)

# Same, but also hidden from ordinary enumeration.
[PSRecoveryPartition.DiskPartitionSpec]::New('RECOVERY', 'Percentage', 20, 'Recovery', ($Attr::Recovery -bor $Attr::Hidden))

# A basic data partition that never receives a drive letter, formatted exFAT.
[PSRecoveryPartition.DiskPartitionSpec]::New('DATA', 'Percentage', 100, 'Basic', $Attr::NoDriveLetter, 'exFAT')
```

Builds DiskPartitionSpec entries with an explicit GPT attribute mask. The five-argument overload takes the mask; the six-argument overload additionally takes the file system. Use the GptPartitionAttributes constants rather than a hex literal, because 0x8000000000000001 does not survive PowerShell's Int64 numeric literal parsing.

## PARAMETERS

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -DiskNumber
Number of the physical disk to erase and repartition, as reported by Get-Disk.
Aliased to -DiskId.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases: DiskId

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Force
Suppresses interactive prompts and overrides safety refusals that would otherwise block destructive or risky changes.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -PartitionLayout
Ordered list of DiskPartitionSpec entries describing a custom layout.
Entries are created in list order; a Percentage entry takes that percentage of the free space remaining at that point, so a trailing 100% entry consumes the rest of the disk.

```yaml
Type: DiskPartitionSpec[]
Parameter Sets: Custom
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PartitionLayoutPreset
Named disk layout.
RecoveryLast (default) is EFI 1 GiB, MSR 1 GiB, OS 80% of remaining, RECOVERY 100% of remaining.
RecoveryFirst is EFI 1 GiB, MSR 1 GiB, RECOVERY 20% of remaining, OS 100% of remaining.
NoRecovery is EFI 1 GiB, MSR 1 GiB, OS 100% of remaining.

```yaml
Type: DiskPartitionLayoutPreset
Parameter Sets: Preset
Aliases:
Accepted values: RecoveryFirst, RecoveryLast, NoRecovery

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PartitionScheme
Partition table style written to the disk: Gpt (default, UEFI) or Mbr (legacy BIOS; at most four primary partitions and no Microsoft Reserved partition).

```yaml
Type: DiskPartitionScheme
Parameter Sets: (All)
Aliases:
Accepted values: Gpt, Mbr

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Returns the resulting object after the operation completes.
By default the cmdlet returns nothing on success.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
{{ Fill ProgressAction Description }}

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Int32
## OUTPUTS

### PSRecoveryPartition.DiskInitializationResult
## NOTES
GPT partition attributes

A Recovery partition is automatically tagged with the canonical Microsoft mask
PLATFORM_REQUIRED | NO_DRIVE_LETTER (0x8000000000000001), so you do not need to
supply it. The mask is applied natively (IOCTL_DISK_SET_DRIVE_LAYOUT_EX) after
the volume is formatted, because a partition that already carries the recovery
type GUID or the no-drive-letter bit will not mount and therefore cannot be
formatted.

To override the mask, use the five-argument DiskPartitionSpec overload together
with the [PSRecoveryPartition.GptPartitionAttributes] constants: None,
PlatformRequired, NoAutomount, ReadOnly, ShadowCopy, Hidden, NoDriveLetter, and
the composite Recovery. Combine them with -bor.

Do not write the mask as a raw PowerShell hex literal. 0x8000000000000001 exceeds
[Int64]::MaxValue, so PowerShell coerces it to a negative number and the value
you get is not the value you wrote. Reference the constants instead.

## RELATED LINKS

