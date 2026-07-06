---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Set-WindowsRecoveryEnvironment

## SYNOPSIS
Registers a WindowsRE image and/or schedules a boot to RE.

## SYNTAX

```
Set-WindowsRecoveryEnvironment [-WindowsREImagePath <FileInfo>] [-Target <DirectoryInfo>] [-BootToRE]
 [-PassThru] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Registers a WindowsRE image with the operating system and optionally schedules a boot into Windows RE on the next restart.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Set-WindowsRecoveryEnvironment -WindowsREImagePath 'C:\RecoveryImages\winre.wim' -PassThru
```

Registers the supplied WindowsRE image and returns the updated configuration.

## PARAMETERS

### -BootToRE
When set, schedules the system to boot into the Windows Recovery Environment on the next restart.

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

### -Target
Reagentc target identifier used when registering or scheduling a WindowsRE boot.

```yaml
Type: DirectoryInfo
Parameter Sets: (All)
Aliases:

Required: False
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
Path to a WindowsRE WIM image that should be staged into the recovery partition.

```yaml
Type: FileInfo
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

### System.IO.FileInfo
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryEnvironmentInfo
## NOTES

## RELATED LINKS

