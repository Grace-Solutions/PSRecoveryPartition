---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# New-RecoveryPartitionPlan

## SYNOPSIS
Builds an idempotent recovery partition plan.

## SYNTAX

### DefaultSize (Default)
```
New-RecoveryPartitionPlan -DiskNumber <Int32> [-Label <String>] [-FileSystem <String>]
 [-WindowsREImagePath <FileInfo>] [-BootImagePath <FileInfo>] [-BootEntryName <String>]
 [-BootTimeout <TimeSpan>] [-BootEntryVisibility <RecoveryBootEntryVisibility>] [-SetDefaultBootEntry]
 [-EntryPointMode <RecoveryEntryPointMode>] [-PushButtonAction <String>] [<CommonParameters>]
```

### ExplicitSize
```
New-RecoveryPartitionPlan -DiskNumber <Int32> -SizeBytes <Int64> [-Label <String>] [-FileSystem <String>]
 [-WindowsREImagePath <FileInfo>] [-BootImagePath <FileInfo>] [-BootEntryName <String>]
 [-BootTimeout <TimeSpan>] [-BootEntryVisibility <RecoveryBootEntryVisibility>] [-SetDefaultBootEntry]
 [-EntryPointMode <RecoveryEntryPointMode>] [-PushButtonAction <String>] [<CommonParameters>]
```

### PercentSize
```
New-RecoveryPartitionPlan -DiskNumber <Int32> -SizePercent <Int32> [-Label <String>] [-FileSystem <String>]
 [-WindowsREImagePath <FileInfo>] [-BootImagePath <FileInfo>] [-BootEntryName <String>]
 [-BootTimeout <TimeSpan>] [-BootEntryVisibility <RecoveryBootEntryVisibility>] [-SetDefaultBootEntry]
 [-EntryPointMode <RecoveryEntryPointMode>] [-PushButtonAction <String>] [<CommonParameters>]
```

## DESCRIPTION
Compares the current disk layout to the requested recovery topology and returns a plan covering partition create or resize, image staging, WindowsRE registration, BCD boot entry creation, and push-button reset configuration. Use Invoke-RecoveryPartitionPlan to apply the plan.

## EXAMPLES

### Example 1: Single-line usage
```powershell
New-RecoveryPartitionPlan -DiskNumber 0 -SizePercent 2 -WindowsREImagePath 'C:\RecoveryImages\winre.wim' -BootImagePath 'C:\RecoveryImages\boot.wim' -EntryPointMode Both
```

Builds the end-to-end plan for a 2 percent recovery partition with WindowsRE registration and a recovery boot entry.

### Example 2: Splatted parameters with OrderedDictionary
```powershell
$NewRecoveryPartitionPlanParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $NewRecoveryPartitionPlanParameters.DiskNumber = 0
    $NewRecoveryPartitionPlanParameters.SizeBytes = 1073741824
    $NewRecoveryPartitionPlanParameters.WindowsREImagePath = 'C:\RecoveryImages\winre.wim'
    $NewRecoveryPartitionPlanParameters.BootImagePath = 'C:\RecoveryImages\boot.wim'
    $NewRecoveryPartitionPlanParameters.EntryPointMode = 'Both'
    $NewRecoveryPartitionPlanParameters.BootEntryName = 'Grace Solutions Recovery'
    $NewRecoveryPartitionPlanParameters.BootTimeout = [Timespan]::FromSeconds(10)
    $NewRecoveryPartitionPlanParameters.Verbose = $True

$NewRecoveryPartitionPlanResult = New-RecoveryPartitionPlan @NewRecoveryPartitionPlanParameters
Write-Output -InputObject ($NewRecoveryPartitionPlanResult)
```

Builds a plan that creates a 1 GiB recovery partition, registers the WindowsRE image, and adds a recovery boot entry.

## PARAMETERS

### -BootEntryName
Friendly name applied to the recovery BCD boot entry.
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

Required: False
Position: Named
Default value: None
Accept pipeline input: False
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

### -DiskNumber
Number of the physical disk to operate on, as reported by Get-Disk.
```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -EntryPointMode
Selects which recovery entry points to configure: PushButton, BootEntry, or Both.
```yaml
Type: RecoveryEntryPointMode
Parameter Sets: (All)
Aliases:
Accepted values: None, PushButtonReset, BootEntry, Both

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FileSystem
File system used to format the recovery partition. Defaults to NTFS.
```yaml
Type: String
Parameter Sets: (All)
Aliases:
Accepted values: NTFS, FAT32, ReFS

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
File system label assigned to the recovery volume. Defaults to RECOVERY.
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

### -SetDefaultBootEntry
When set in a plan, instructs the plan to make the recovery BCD entry the default boot entry.
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

### -SizeBytes
Explicit partition size in bytes. Mutually exclusive with -SizePercent.
```yaml
Type: Int64
Parameter Sets: ExplicitSize
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SizePercent
Partition size expressed as a percentage of the target disk size. Mutually exclusive with -SizeBytes.
```yaml
Type: Int32
Parameter Sets: PercentSize
Aliases:

Required: True
Position: Named
Default value: None
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
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Int32
## OUTPUTS

### PSRecoveryPartition.RecoveryPartitionPlan
## NOTES

## RELATED LINKS

