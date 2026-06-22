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
 [-WindowsREImagePath <FileInfo>] [-PassThru] [-Force] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

### ExplicitSize
```
New-RecoveryPartition -DiskNumber <Int32> -SizeBytes <Int64> [-Label <String>] [-FileSystem <String>]
 [-WindowsREImagePath <FileInfo>] [-PassThru] [-Force] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

### PercentSize
```
New-RecoveryPartition -DiskNumber <Int32> -SizePercent <Int32> [-Label <String>] [-FileSystem <String>]
 [-WindowsREImagePath <FileInfo>] [-PassThru] [-Force] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Creates a recovery partition on the specified disk and optionally stages a WindowsRE image into it.
Use -SizeBytes for explicit sizing or -SizePercent for percentage-based sizing.
The two are mutually exclusive parameter sets.

## EXAMPLES

### Example 1: Single-line usage
```
New-RecoveryPartition -DiskNumber 0 -SizeBytes 1073741824 -WindowsREImagePath 'C:\RecoveryImages\Winre.wim' -PassThru
```

Creates a 1 GiB recovery partition on disk 0 and stages the supplied WindowsRE image.

### Example 2: Splatted parameters with OrderedDictionary
```
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
```

Creates a percentage-sized recovery partition with a friendly label and stages a WindowsRE image.

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
{{ Fill DiskNumber Description }}

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
{{ Fill FileSystem Description }}

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
{{ Fill Force Description }}

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
{{ Fill Label Description }}

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
{{ Fill PassThru Description }}

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
{{ Fill SizeBytes Description }}

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
{{ Fill SizePercent Description }}

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

### -WindowsREImagePath
{{ Fill WindowsREImagePath Description }}

```yaml
Type: FileInfo
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
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

### PSRecoveryPartition.RecoveryPartitionInfo
## NOTES

## RELATED LINKS
