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

```
Set-WindowsRecoveryImage -SourceImagePath <FileInfo> -DestinationPath <FileInfo> [-Force] [-PassThru] [-WhatIf]
 [-Confirm] [<CommonParameters>]
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

### -SourceImagePath
{{ Fill SourceImagePath Description }}

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
{{ Fill DestinationPath Description }}

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.IO.FileInfo
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryImageInfo
## NOTES

## RELATED LINKS

