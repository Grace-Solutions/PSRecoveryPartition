---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Set-WindowsRecoveryEntryPoint

## SYNOPSIS
Configures push-button reset, a boot entry, or both as recovery entry points.

## SYNTAX

```
Set-WindowsRecoveryEntryPoint -EntryPointMode <RecoveryEntryPointMode> [-BootImagePath <FileInfo>]
 [-WindowsREImagePath <FileInfo>] [-PushButtonAction <String>] [-Name <String>] [-BootTimeout <TimeSpan>]
 [-BootEntryVisibility <RecoveryBootEntryVisibility>] [-SetDefault] [-InputObject <Object>] [-Force]
 [-PassThru] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Combines WindowsRE registration and BCD boot entry creation behind a single high-level surface.
EntryPointMode selects which paths are configured.

## EXAMPLES

### Example 1: Single-line usage
```
Set-WindowsRecoveryEntryPoint -EntryPointMode Both -BootImagePath 'C:\RecoveryImages\boot.wim' -WindowsREImagePath 'C:\RecoveryImages\winre.wim' -BootTimeout ([timespan]'00:00:10') -PassThru
```

Configures both push-button reset and a recovery boot entry in a single call.

### Example 2: Splatted parameters with OrderedDictionary
```
$SetWindowsRecoveryEntryPointParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $SetWindowsRecoveryEntryPointParameters.EntryPointMode = 'Both'
    $SetWindowsRecoveryEntryPointParameters.BootImagePath = 'C:\RecoveryImages\boot.wim'
    $SetWindowsRecoveryEntryPointParameters.WindowsREImagePath = 'C:\RecoveryImages\winre.wim'
    $SetWindowsRecoveryEntryPointParameters.PushButtonAction = 'BootToRE'
    $SetWindowsRecoveryEntryPointParameters.Name = 'Grace Solutions Recovery'
    $SetWindowsRecoveryEntryPointParameters.BootTimeout = [Timespan]::FromSeconds(10)
    $SetWindowsRecoveryEntryPointParameters.BootEntryVisibility = 'Visible'
    $SetWindowsRecoveryEntryPointParameters.PassThru = $True
    $SetWindowsRecoveryEntryPointParameters.Verbose = $True

$SetWindowsRecoveryEntryPointResult = Set-WindowsRecoveryEntryPoint @SetWindowsRecoveryEntryPointParameters
Write-Output -InputObject ($SetWindowsRecoveryEntryPointResult)
```

Splatted OrderedDictionary example for the combined entry-point configuration.

## PARAMETERS

### -BootEntryVisibility
{{ Fill BootEntryVisibility Description }}

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

### -BootTimeout
{{ Fill BootTimeout Description }}

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

### -EntryPointMode
{{ Fill EntryPointMode Description }}

```yaml
Type: RecoveryEntryPointMode
Parameter Sets: (All)
Aliases:
Accepted values: PushButtonReset, BootEntry, Both

Required: True
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
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
{{ Fill InputObject Description }}

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
{{ Fill Name Description }}

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
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -PushButtonAction
{{ Fill PushButtonAction Description }}

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

### -SetDefault
{{ Fill SetDefault Description }}

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

### -WindowsREImagePath
{{ Fill WindowsREImagePath Description }}

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
