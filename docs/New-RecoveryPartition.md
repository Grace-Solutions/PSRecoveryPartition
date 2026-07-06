---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# New-RecoveryPartition

## SYNOPSIS
Creates a recovery partition on a target disk.

## SYNTAX

### DefaultSize (Default)
```
New-RecoveryPartition -DiskNumber <Int32> [-Label <String>] [-FileSystem <String>]
 [-CreationMode <RecoveryPartitionCreationMode>] [-PassThru] [-Force] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ExplicitSize
```
New-RecoveryPartition -DiskNumber <Int32> -SizeBytes <Int64> [-Label <String>] [-FileSystem <String>]
 [-CreationMode <RecoveryPartitionCreationMode>] [-PassThru] [-Force] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

### PercentSize
```
New-RecoveryPartition -DiskNumber <Int32> -SizePercent <Int32> [-Label <String>] [-FileSystem <String>]
 [-CreationMode <RecoveryPartitionCreationMode>] [-PassThru] [-Force] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Creates and formats a recovery partition on the specified disk. Use -SizeBytes for explicit sizing or -SizePercent for percentage-based sizing (mutually exclusive parameter sets). The partition is always placed after the existing partitions; -CreationMode controls how trailing free space is obtained. Image and boot-entry payloads are staged by the dedicated cmdlets (Set-WindowsRecoveryImage, New-WindowsRecoveryBootEntry).

## EXAMPLES

### Example 1: Single-line usage
```powershell
New-RecoveryPartition -DiskNumber 0 -SizeBytes 1073741824 -PassThru
```

Creates a 1 GiB recovery partition on disk 0 using existing trailing free space.

### Example 2: Splatted parameters with OrderedDictionary
```powershell
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
```

Creates a percentage-sized recovery partition, shrinking the last partition to make room if the disk has no trailing free space.

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
Number of the physical disk to operate on, as reported by Get-Disk.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -FileSystem
File system used to format the recovery partition.
Defaults to NTFS.

```yaml
Type: String
Parameter Sets: (All)
Aliases:
Accepted values: NTFS, FAT32, ReFS

Required: False
Position: Named
Default value: None
Accept pipeline input: False
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

### -Label
File system label assigned to the recovery volume.
Defaults to RECOVERY.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

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

### -SizeBytes
Explicit partition size in bytes.
Mutually exclusive with -SizePercent.

```yaml
Type: Int64
Parameter Sets: ExplicitSize
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SizePercent
Partition size expressed as a percentage of the target disk size.
Mutually exclusive with -SizeBytes.

```yaml
Type: Int32
Parameter Sets: PercentSize
Aliases:

Required: True
Position: Named
Default value: None
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

### -CreationMode
Placement strategy.
The recovery partition is always created after the existing partitions (never before the OS).
UseTrailingFreeSpace (default) appends into existing free space at the end of the disk and never moves or resizes anything, failing if there is not enough room.
ShrinkToFit shrinks the last partition (typically the OS) by the shortfall to free trailing space first.
RequireEmptyDisk only creates on a disk with no partitions.

```yaml
Type: RecoveryPartitionCreationMode
Parameter Sets: (All)
Aliases:

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

### PSRecoveryPartition.RecoveryPartitionInfo
## NOTES

## RELATED LINKS

