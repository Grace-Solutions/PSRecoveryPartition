---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Get-WindowsRecoveryBootEntry

## SYNOPSIS
Discovers recovery boot entries.

## SYNTAX

```
Get-WindowsRecoveryBootEntry [-Name <String>] [-BootImagePath <FileInfo>] [-IncludeHidden] [-IncludeAll]
 [<CommonParameters>]
```

## DESCRIPTION
Enumerates Boot Configuration Data entries and returns those that look like recovery boot entries. Use -IncludeAll to return every entry regardless of recovery heuristics.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Get-WindowsRecoveryBootEntry
```

Lists the recovery boot entries currently registered in BCD.

## PARAMETERS

### -BootImagePath
{{ Fill BootImagePath Description }}

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

### -IncludeAll
{{ Fill IncludeAll Description }}

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

### -IncludeHidden
{{ Fill IncludeHidden Description }}

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

### -Name
{{ Fill Name Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String
### System.IO.FileInfo
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryBootEntryInfo
## NOTES

## RELATED LINKS

