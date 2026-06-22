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
Save-RecoveryBootImage [-SourceUri] <Uri> [-DestinationDirectory] <DirectoryInfo>
 [-DestinationFileName <String>] [-ImageKind <String>] [-Force] [-PassThru]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ByPath
```
Save-RecoveryBootImage [-SourcePath] <FileInfo> [-DestinationDirectory] <DirectoryInfo>
 [-DestinationFileName <String>] [-ImageKind <String>] [-Force] [-PassThru]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Downloads a WIM from an HTTPS source or copies it from a local or UNC path to the supplied destination folder. Idempotent when the destination already matches the source.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Save-RecoveryBootImage -SourceUri 'https://example.com/boot.wim' -DestinationDirectory 'C:\RecoveryImages'
```

Downloads the boot image from the supplied URI to C:\RecoveryImages.

## PARAMETERS

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DestinationDirectory
{{ Fill DestinationDirectory Description }}

```yaml
Type: DirectoryInfo
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DestinationFileName
{{ Fill DestinationFileName Description }}

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

### -Force
{{ Fill Force Description }}

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

### -ImageKind
{{ Fill ImageKind Description }}

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
{{ Fill PassThru Description }}

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

### -SourcePath
{{ Fill SourcePath Description }}

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
{{ Fill SourceUri Description }}

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Uri

### System.IO.FileInfo

## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryImageInfo

## NOTES

## RELATED LINKS

