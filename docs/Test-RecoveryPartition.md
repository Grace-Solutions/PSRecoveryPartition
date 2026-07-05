---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Test-RecoveryPartition

## SYNOPSIS
Tests a recovery partition for size, layout, and contents.

## SYNTAX

```
Test-RecoveryPartition [-DiskNumber <Int32>] [-PartitionNumber <Int32>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Inspects a recovery partition and returns a structured pass/fail report covering size, layout, and presence of a WindowsRE image.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Test-RecoveryPartition -DiskNumber 0 -PartitionNumber 5
```

Validates the recovery partition on disk 0 partition 5.

## PARAMETERS

### -DiskNumber
Number of the physical disk to operate on, as reported by Get-Disk.

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

### -PartitionNumber
Number of the partition on the target disk, as reported by Get-Partition.

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

### System.Boolean
## NOTES

## RELATED LINKS


