---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Set-WindowsRecoveryBootImage

## SYNOPSIS
Replaces the boot image on an existing recovery boot entry.

## SYNTAX

### ByName (Default)
```
Set-WindowsRecoveryBootImage [-Name] <String> -BootImagePath <FileInfo> [-Force] [-PassThru]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ByInput
```
Set-WindowsRecoveryBootImage -InputObject <WindowsRecoveryBootEntryInfo> -BootImagePath <FileInfo> [-Force]
 [-PassThru] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ByIdentifier
```
Set-WindowsRecoveryBootImage -Identifier <String> -BootImagePath <FileInfo> [-Force] [-PassThru]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Stages a new boot image over the WIM an existing ramdisk recovery boot entry currently boots from, so the entry immediately uses the new image. The entry's staged path and boot.sdi are preserved and the BCD store is not modified. -Name accepts wildcards and updates every matching entry. Fails for entries that are not staged-WIM ramdisk entries or whose recovery partition cannot be resolved.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Set-WindowsRecoveryBootImage -Name 'Grace Solutions Recovery' -BootImagePath 'C:\RecoveryImages\winre-new.wim' -PassThru
```

Replaces the boot image on the named recovery entry with winre-new.wim.

## PARAMETERS

### -BootImagePath
New source WIM to stage over the entry's current boot image.

```yaml
Type: FileInfo
Parameter Sets: (All)
Aliases: SourceImagePath

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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

### -Identifier
BCD GUID identifier of the target boot entry.

```yaml
Type: String
Parameter Sets: ByIdentifier
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Object received from the pipeline that the cmdlet should act on.

```yaml
Type: WindowsRecoveryBootEntryInfo
Parameter Sets: ByInput
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Name
Friendly display name of the boot entry to update.
Supports wildcards (for example Grace or Recovery*) and updates every match; a name with no wildcard characters matches case-insensitively.

```yaml
Type: String
Parameter Sets: ByName
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
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

### PSRecoveryPartition.WindowsRecoveryBootEntryInfo
### System.IO.FileInfo
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryBootEntryInfo
## NOTES

## RELATED LINKS

