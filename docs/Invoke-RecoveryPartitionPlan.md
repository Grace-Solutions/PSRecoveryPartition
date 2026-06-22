---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Invoke-RecoveryPartitionPlan

## SYNOPSIS
Executes a recovery partition plan idempotently.

## SYNTAX

```
Invoke-RecoveryPartitionPlan -InputObject <RecoveryPartitionPlan> [-PassThru] [-Force] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
Applies the steps in a plan produced by New-RecoveryPartitionPlan. Steps are processed in order and each step checks current state before applying changes. Honours -WhatIf and -Confirm and returns the resulting recovery partition when -PassThru is supplied.

## EXAMPLES

### Example 1: Single-line usage
```powershell
New-RecoveryPartitionPlan -DiskNumber 0 -SizePercent 2 | Invoke-RecoveryPartitionPlan -PassThru
```

Builds and applies the plan on disk 0 and emits the resulting recovery partition.

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

### -InputObject
{{ Fill InputObject Description }}

```yaml
Type: RecoveryPartitionPlan
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### PSRecoveryPartition.RecoveryPartitionPlan
## OUTPUTS

### PSRecoveryPartition.RecoveryPartitionInfo
## NOTES

## RELATED LINKS

