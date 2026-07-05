---
external help file: PSRecoveryPartition.dll-Help.xml
Module Name: PSRecoveryPartition
online version:
schema: 2.0.0
---

# New-WindowsRecoveryBootEntry

## SYNOPSIS
Creates a custom recovery boot entry from a boot image.

## SYNTAX

### ByImagePath (Default)
```
New-WindowsRecoveryBootEntry -BootImagePath <FileInfo> [-StagingRelativePath <String>] [-ExpandBootImage]
 [-ImageIndex <Int32>] [-BootSdiPath <FileInfo>] [-Name <String>] [-BootTimeout <TimeSpan>]
 [-BootEntryVisibility <RecoveryBootEntryVisibility>] [-SetDefault] [-AddLast] [-Force] [-PassThru]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ByRecoveryPartition
```
New-WindowsRecoveryBootEntry -BootImagePath <FileInfo> -RecoveryPartition <RecoveryPartitionInfo>
 [-StagingRelativePath <String>] [-ExpandBootImage] [-ImageIndex <Int32>] [-BootSdiPath <FileInfo>]
 [-Name <String>] [-BootTimeout <TimeSpan>] [-BootEntryVisibility <RecoveryBootEntryVisibility>] [-SetDefault]
 [-AddLast] [-Force] [-PassThru] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ByTargetPath
```
New-WindowsRecoveryBootEntry -BootImagePath <FileInfo> -TargetPath <DirectoryInfo>
 [-StagingRelativePath <String>] [-ExpandBootImage] [-ImageIndex <Int32>] [-BootSdiPath <FileInfo>]
 [-Name <String>] [-BootTimeout <TimeSpan>] [-BootEntryVisibility <RecoveryBootEntryVisibility>] [-SetDefault]
 [-AddLast] [-Force] [-PassThru] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Stages a boot image onto a destination and creates a matching BCD boot entry. The destination is a recovery partition (written through its \\?\GLOBALROOT path with no mount), an already-mounted directory (-TargetPath), or the image's existing location (the default ByImagePath set). By default the WIM is staged as-is and booted as a ramdisk, and a boot.sdi is resolved automatically (or supplied with -BootSdiPath); with -ExpandBootImage the image is expanded flat onto the destination for non-RAM boot and the entry is wired to boot it in place. The staging sub-path defaults to \Recovery\WindowsRE but is fully overridable via -StagingRelativePath. Existing entries with the same name are not duplicated.

## EXAMPLES

### Example 1: Single-line usage
```powershell
Get-RecoveryPartition | New-WindowsRecoveryBootEntry -BootImagePath 'C:\RecoveryImages\winre.wim' -PassThru
```

Stages winre.wim onto the recovery partition and creates a ramdisk recovery boot entry.

## PARAMETERS

### -AddLast
When set, appends the new BCD entry to the end of the boot order instead of the default position.

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
Aliases: SourceImagePath

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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

### -Name
Friendly display name of the target boot entry.

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

### -SetDefault
When set, marks the new boot entry as the default in BCD.

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

### -BootSdiPath
Explicit boot.sdi to stage for ramdisk boot.
When omitted it is resolved from the live OS and then extracted from the boot image.

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

### -ExpandBootImage
Expands the boot image flat onto the destination (non-RAM / flat boot) and wires the entry to boot it in place, instead of staging the WIM for ramdisk boot.
Requires a destination (-RecoveryPartition or -TargetPath).

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

### -ImageIndex
One-based image index inside the WIM to expand when -ExpandBootImage is used.
Defaults to 1.

```yaml
Type: Int32
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

### -RecoveryPartition
Recovery partition (RecoveryPartitionInfo, typically piped from Get-RecoveryPartition) to stage the boot image onto and target with the BCD entry.
Files are written through the partition's \?\GLOBALROOT path without assigning a drive letter or mounting the volume.

```yaml
Type: RecoveryPartitionInfo
Parameter Sets: ByRecoveryPartition
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -StagingRelativePath
Volume-relative folder the image (and boot.sdi) are staged into.
Defaults to \Recovery\WindowsRE; pass an empty string to stage at the volume root.

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

### -TargetPath
An already-mounted directory to stage the boot image into.
Use this when the destination volume has a drive letter or mount point.

```yaml
Type: DirectoryInfo
Parameter Sets: ByTargetPath
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
### System.Object
## OUTPUTS

### PSRecoveryPartition.WindowsRecoveryBootEntryInfo
## NOTES

## RELATED LINKS

