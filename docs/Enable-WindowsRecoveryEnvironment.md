---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Enable-WindowsRecoveryEnvironment

## SYNOPSIS
Enables Windows Recovery Environment.

## SYNTAX

```
Enable-WindowsRecoveryEnvironment [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enables the Windows Recovery Environment and returns the resulting configuration.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Enable-WindowsRecoveryEnvironment -PassThru
```

Enables WinRE and returns the new state.

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

### None
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryEnvironmentInfo
## NOTES

## RELATED LINKS

