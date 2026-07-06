---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Set-WindowsRecoveryEntryPoint

## SYNOPSIS
Configures Windows RE / push-button reset as a recovery entry point.

## SYNTAX

```
Set-WindowsRecoveryEntryPoint [-EntryPointMode <RecoveryEntryPointMode>] [-WindowsREImagePath <FileInfo>]
 [-PushButtonAction <String>] [-Force] [-PassThru] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
Optionally registers a WindowsRE image, enables Windows RE, and schedules a boot into Windows RE on the next restart. Custom BCD boot entries are created by New-WindowsRecoveryBootEntry; this cmdlet no longer creates them, so boot-entry creation has a single, unambiguous path. -EntryPointMode BootEntry or Both is rejected and redirected to New-WindowsRecoveryBootEntry.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Set-WindowsRecoveryEntryPoint -EntryPointMode PushButtonReset -PushButtonAction BootToRE -PassThru
```

Enables Windows RE and schedules a boot into it on the next restart.

### Example 2: Splatted parameters with OrderedDictionary
```powershell
$SetWindowsRecoveryEntryPointParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $SetWindowsRecoveryEntryPointParameters.EntryPointMode = 'PushButtonReset'
    $SetWindowsRecoveryEntryPointParameters.WindowsREImagePath = 'C:\RecoveryImages\winre.wim'
    $SetWindowsRecoveryEntryPointParameters.PushButtonAction = 'BootToRE'
    $SetWindowsRecoveryEntryPointParameters.PassThru = $True
    $SetWindowsRecoveryEntryPointParameters.Verbose = $True

$SetWindowsRecoveryEntryPointResult = Set-WindowsRecoveryEntryPoint @SetWindowsRecoveryEntryPointParameters
Write-Output -InputObject ($SetWindowsRecoveryEntryPointResult)
```

Splatted OrderedDictionary example for push-button / Windows RE configuration.

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

### -EntryPointMode
Selects which recovery entry points to configure: PushButton, BootEntry, or Both.

```yaml
Type: RecoveryEntryPointMode
Parameter Sets: (All)
Aliases:
Accepted values: PushButtonReset, BootEntry, Both

Required: False
Position: Named
Default value: None
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

### -PushButtonAction
Friendly action keyword translated to a Windows recovery push-button reset action (for example Reset, Refresh, FactoryReset, BootToRE).

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
### System.Object
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryEntryPointResult
## NOTES

## RELATED LINKS

