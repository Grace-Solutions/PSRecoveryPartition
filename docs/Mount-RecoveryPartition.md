---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# Mount-RecoveryPartition

## SYNOPSIS
Mounts a recovery partition at a folder mount point.

## SYNTAX

```
Mount-RecoveryPartition -DiskNumber <Int32> -PartitionNumber <Int32> -AccessPath <String> [-PassThru] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Assigns a folder mount point so the recovery partition can be inspected from Explorer or other tooling.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Mount-RecoveryPartition -DiskNumber 0 -PartitionNumber 5 -MountPath 'C:\Mounts\Recovery'
```

Mounts the recovery partition on disk 0 partition 5 at the specified folder.

## PARAMETERS

### -AccessPath
Folder mount path (for example C:\Mounts\Recovery) used as an access path for the recovery partition.
```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
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

### -DiskNumber
Number of the physical disk to operate on (matches `Get-RecoveryPartition.DiskNumber` and the `\\.\PhysicalDriveN` device path; enumerated natively by the module without consulting the Storage PowerShell module).
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

### -PartitionNumber
Number of the partition on the target disk (matches `Get-RecoveryPartition.PartitionNumber`; enumerated natively from the disk's partition table via `IOCTL_DISK_GET_DRIVE_LAYOUT_EX`).
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

### -PassThru
Returns the resulting object after the operation completes. By default the cmdlet returns nothing on success.
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Int32
## OUTPUTS

### PSRecoveryPartition.RecoveryPartitionMountResult
## NOTES

## RELATED LINKS

