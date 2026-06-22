---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Get-WindowsRecoveryEnvironment

## SYNOPSIS
Returns the current Windows Recovery Environment configuration.

## SYNTAX

```
Get-WindowsRecoveryEnvironment [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Queries the active Windows Recovery Environment configuration and returns a structured WindowsRecoveryEnvironmentInfo object.

## EXAMPLES

### Example 1: Single-line usage
```
Get-WindowsRecoveryEnvironment
```

Returns the current WinRE configuration.

## PARAMETERS

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

### None
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryEnvironmentInfo
## NOTES

## RELATED LINKS
