---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Get-RecoveryPartitionPlan

## SYNOPSIS
Produces an idempotent plan for the requested recovery layout.

## SYNTAX

### DefaultSize (Default)
```
Get-RecoveryPartitionPlan -DiskNumber <Int32> [-Label <String>] [-FileSystem <String>]
 [-WindowsREImagePath <FileInfo>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### ExplicitSize
```
Get-RecoveryPartitionPlan -DiskNumber <Int32> -SizeBytes <Int64> [-Label <String>] [-FileSystem <String>]
 [-WindowsREImagePath <FileInfo>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### PercentSize
```
Get-RecoveryPartitionPlan -DiskNumber <Int32> -SizePercent <Int32> [-Label <String>] [-FileSystem <String>]
 [-WindowsREImagePath <FileInfo>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Compares the current disk layout to the requested recovery layout and returns a structured plan describing the steps required to converge.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Get-RecoveryPartitionPlan -DiskNumber 0 -SizePercent 2
```

Returns the plan required to provision a 2 percent recovery partition on disk 0.

### Example 2: Splatted parameters with OrderedDictionary
```powershell
$GetRecoveryPartitionPlanParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $GetRecoveryPartitionPlanParameters.DiskNumber = 0
    $GetRecoveryPartitionPlanParameters.SizeBytes = 1073741824
    $GetRecoveryPartitionPlanParameters.WindowsREImagePath = 'C:\RecoveryImages\winre.wim'
    $GetRecoveryPartitionPlanParameters.Verbose = $True

$GetRecoveryPartitionPlanResult = Get-RecoveryPartitionPlan @GetRecoveryPartitionPlanParameters
Write-Output -InputObject ($GetRecoveryPartitionPlanResult)
```

Builds a plan to create a 1 GiB recovery partition staged with the supplied WindowsRE image.

## PARAMETERS

### -DiskNumber
{{ Fill DiskNumber Description }}

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

### -FileSystem
{{ Fill FileSystem Description }}

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
{{ Fill Label Description }}

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

### -SizeBytes
{{ Fill SizeBytes Description }}

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
{{ Fill SizePercent Description }}

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
{{ Fill WindowsREImagePath Description }}

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

### System.Int32

## OUTPUTS

### PSRecoveryPartition.RecoveryPartitionPlan

## NOTES

## RELATED LINKS

