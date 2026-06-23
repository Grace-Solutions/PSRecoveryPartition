---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# New-WindowsRecoveryBootEntry

## SYNOPSIS
Creates a recovery boot entry idempotently.

## SYNTAX

```
New-WindowsRecoveryBootEntry -BootImagePath <FileInfo> [-Name <String>] [-BootTimeout <TimeSpan>]
 [-BootEntryVisibility <RecoveryBootEntryVisibility>] [-SetDefault] [-AddLast] [-InputObject <Object>] [-Force]
 [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Creates a BCD boot entry that boots from the supplied WIM image. Existing entries with the same name are not duplicated.

## EXAMPLES

### Example 1: Single-line usage
```powershell
New-WindowsRecoveryBootEntry -BootImagePath 'C:\RecoveryImages\boot.wim' -Name 'Recovery' -PassThru
```

Creates a recovery boot entry that boots boot.wim.

## PARAMETERS

### -AddLast
When set, appends the new BCD entry to the end of the boot order instead of the default position.
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

### -BootEntryVisibility
Whether the recovery BCD boot entry is Visible or Hidden in the boot menu.
```yaml
Type: RecoveryBootEntryVisibility
Parameter Sets: (All)
Aliases:
Accepted values: Visible, Hidden

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -BootImagePath
Path to a boot WIM image used by the BCD recovery entry.
```yaml
Type: FileInfo
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -BootTimeout
Boot menu timeout applied when a new recovery boot entry is configured.
```yaml
Type: TimeSpan
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
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

### -InputObject
Object received from the pipeline that the cmdlet should act on.
```yaml
Type: Object
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Name
Friendly display name of the target boot entry.
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
Returns the resulting object after the operation completes. By default the cmdlet returns nothing on success.
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

### -SetDefault
When set, marks the new boot entry as the default in BCD.
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

### System.IO.FileInfo
### System.Object
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryBootEntryInfo
## NOTES

## RELATED LINKS

