---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Save-RecoveryBootImage

## SYNOPSIS
Downloads or copies a recovery boot image to a local destination.

## SYNTAX

### ByUri (Default)
```
Save-RecoveryBootImage [-SourceUri] <Uri> -DestinationPath <FileInfo> [-ImageKind <String>]
 [-Headers <IDictionary>] [-Hidden] [-System] [-Force] [-PassThru] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ByPath
```
Save-RecoveryBootImage [-SourcePath] <FileInfo> -DestinationPath <FileInfo> [-ImageKind <String>] [-Hidden]
 [-System] [-Force] [-PassThru] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Downloads a WIM from an HTTPS source or copies it from a local or UNC path to the supplied destination. When DestinationPath names an existing directory or ends with a directory separator the source leaf name is appended; otherwise it is treated as the target file path. Idempotent when the destination already matches the source.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Save-RecoveryBootImage -SourceUri 'https://example.com/boot.wim' -DestinationPath 'C:\RecoveryImages\boot.wim'
```

Downloads the boot image from the supplied URI to C:\RecoveryImages\boot.wim.

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

### -ImageKind
Image classification (WindowsRE, WindowsPE, BootWim) used when tagging the result.

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

### -SourcePath
Source file path on a local or UNC volume used as the input to the copy.

```yaml
Type: FileInfo
Parameter Sets: ByPath
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -SourceUri
HTTPS URI from which the recovery image should be downloaded.

```yaml
Type: Uri
Parameter Sets: ByUri
Aliases:

Required: True
Position: 0
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

### -System
{{ Fill System Description }}

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Uri
### System.IO.FileInfo
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryImageInfo
## NOTES

## RELATED LINKS

