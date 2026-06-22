---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Get-WindowsRecoveryImage

## SYNOPSIS
Discovers Windows RE or Windows PE image files.

## SYNTAX

```
Get-WindowsRecoveryImage [-Path <DirectoryInfo>] [-ImageKind <String>] [-ComputeHash] [<CommonParameters>]
```

## DESCRIPTION
Enumerates WIM files in the supplied path and returns structured information about each candidate recovery image.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Get-WindowsRecoveryImage -Path 'C:\RecoveryImages'
```

Lists WIM files in the specified folder.

## PARAMETERS

### -ComputeHash
When set, computes a SHA-256 hash of each discovered image. Slower but useful for change detection.
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
Accepted values: WindowsRE, WindowsPE, Boot, Custom

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
File-system path to search for WIM images.
```yaml
Type: DirectoryInfo
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.IO.DirectoryInfo
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryImageInfo
## NOTES

## RELATED LINKS

