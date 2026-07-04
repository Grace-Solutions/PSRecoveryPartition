---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Get-RecoveryPartition

## SYNOPSIS
Discovers recovery partitions on local disks.

## SYNTAX

```
Get-RecoveryPartition [-DiskNumber <Int32>] [-PartitionNumber <Int32>] [-IncludeNonRecovery]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Enumerates physical disks and emits a RecoveryPartitionInfo object for each partition that carries the Windows recovery partition type GUID. Use -IncludeNonRecovery to widen the search to non-recovery partitions for diagnostics.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Get-RecoveryPartition -DiskNumber 0
```

Lists every recovery partition on disk 0.

### Example 2: Splatted parameters with OrderedDictionary
```powershell
$GetRecoveryPartitionParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $GetRecoveryPartitionParameters.DiskNumber = 0
    $GetRecoveryPartitionParameters.IncludeNonRecovery = $False
    $GetRecoveryPartitionParameters.Verbose = $True

$GetRecoveryPartitionResult = Get-RecoveryPartition @GetRecoveryPartitionParameters

Write-Output -InputObject ($GetRecoveryPartitionResult)
```

Discovers recovery partitions on disk 0 using a splatted OrderedDictionary parameter set.

## PARAMETERS

### -DiskNumber
Optional disk filter.
When omitted, every physical disk is searched.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -IncludeNonRecovery
When set, the discovery surface also returns partitions that do not look like recovery partitions; useful for diagnostics.

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

### -PartitionNumber
Optional partition filter applied to the results of the disk-level enumeration.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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

### System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
## OUTPUTS

### PSRecoveryPartition.RecoveryPartitionInfo
## NOTES

## RELATED LINKS

