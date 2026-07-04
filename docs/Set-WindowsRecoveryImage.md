---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Set-WindowsRecoveryImage

## SYNOPSIS
Copies or updates a Windows RE or Windows PE image.

## SYNTAX

### ByPath (Default)
```
Set-WindowsRecoveryImage -SourceImagePath <FileInfo> -DestinationPath <FileInfo> [-Hidden] [-System] [-Force]
 [-PassThru] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ByUri
```
Set-WindowsRecoveryImage -SourceUri <Uri> [-Headers <IDictionary>] -DestinationPath <FileInfo> [-Hidden]
 [-System] [-Force] [-PassThru] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copies the supplied source image to a destination folder. The copy is skipped when the destination already matches the source by size and last-write timestamp.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Set-WindowsRecoveryImage -SourceImagePath 'C:\RecoveryImages\winre.wim' -DestinationPath 'D:\Recovery\winre.wim' -PassThru
```

Copies winre.wim to the recovery volume and returns the staged image record. When DestinationPath ends with a directory separator or names an existing directory, the source leaf name is appended; otherwise the destination is treated as a full file path and the source is renamed on copy.

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

### -SourceImagePath
Source WIM file that should be copied or staged.

```yaml
Type: FileInfo
Parameter Sets: ByPath
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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

### -DestinationPath
Target file or directory path.
When it names an existing directory or ends with a path separator the source leaf name is appended; otherwise the value is treated as the full destination file path and the source is renamed on copy.

```yaml
Type: FileInfo
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Headers
{{ Fill Headers Description }}

```yaml
Type: IDictionary
Parameter Sets: ByUri
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Hidden
{{ Fill Hidden Description }}

```yaml
Type: SwitchParameter
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

### -SourceUri
HTTPS URI from which the recovery image should be downloaded.
```yaml
Type: Uri
Parameter Sets: ByUri
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -System
{{ Fill System Description }}

```yaml
Type: SwitchParameter
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

### System.IO.FileInfo
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryImageInfo
## NOTES

## RELATED LINKS

