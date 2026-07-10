---
Module Name: PSRecoveryPartition
Module Guid: d6d6f0b8-2a4b-4a8d-9d2a-7c0f4c2b5e9a
Download Help Link: https://github.com/GraceSolutions/PSRecoveryPartition
Help Version: 2026.7.10.135
Locale: en-US
---

# PSRecoveryPartition Module
## Description
PowerShell module for managing Windows recovery partitions, Windows Recovery Environment, and recovery boot entries. Uses native Win32 IOCTLs and P/Invokes (no dependency on the in-box Storage module / MSFT_* CIM classes); controlled Microsoft inbox process fallback (reagentc, bcdedit, diskpart for the MBR 0x27 type byte) is used only where required.

## PSRecoveryPartition Cmdlets
### [Disable-WindowsRecoveryEnvironment](Disable-WindowsRecoveryEnvironment.md)
Disables Windows Recovery Environment.

### [Dismount-RecoveryPartition](Dismount-RecoveryPartition.md)
Removes a folder mount point from a recovery partition.

### [Enable-WindowsRecoveryEnvironment](Enable-WindowsRecoveryEnvironment.md)
Enables Windows Recovery Environment.

### [Get-RecoveryPartition](Get-RecoveryPartition.md)
Discovers recovery partitions on local disks.

### [Get-WindowsRecoveryBootEntry](Get-WindowsRecoveryBootEntry.md)
Discovers recovery boot entries.

### [Get-WindowsRecoveryEnvironment](Get-WindowsRecoveryEnvironment.md)
Returns the current Windows Recovery Environment configuration.

### [Get-WindowsRecoveryImage](Get-WindowsRecoveryImage.md)
Discovers Windows RE or Windows PE image files.

### [Initialize-RecoveryDisk](Initialize-RecoveryDisk.md)
Erases a disk and lays down a complete partition set.

### [Mount-RecoveryPartition](Mount-RecoveryPartition.md)
Mounts a recovery partition at a folder mount point.

### [New-RecoveryPartition](New-RecoveryPartition.md)
Creates a recovery partition on a target disk.

### [New-WindowsRecoveryBootEntry](New-WindowsRecoveryBootEntry.md)
Creates a custom recovery boot entry from a boot image.

### [Remove-RecoveryPartition](Remove-RecoveryPartition.md)
Removes a recovery partition.

### [Remove-WindowsRecoveryBootEntry](Remove-WindowsRecoveryBootEntry.md)
Removes a recovery boot entry idempotently.

### [Resize-RecoveryPartition](Resize-RecoveryPartition.md)
Resizes a recovery partition to the requested size.

### [Save-RecoveryBootImage](Save-RecoveryBootImage.md)
Downloads or copies a recovery boot image to a local destination.

### [Set-RecoveryPartition](Set-RecoveryPartition.md)
Updates the metadata of an existing recovery partition.

### [Set-WindowsRecoveryBootImage](Set-WindowsRecoveryBootImage.md)
Replaces the boot image on an existing recovery boot entry.

### [Set-WindowsRecoveryEntryPoint](Set-WindowsRecoveryEntryPoint.md)
Configures Windows RE / push-button reset as a recovery entry point.

### [Set-WindowsRecoveryEnvironment](Set-WindowsRecoveryEnvironment.md)
Registers a WindowsRE image and/or schedules a boot to RE.

### [Set-WindowsRecoveryImage](Set-WindowsRecoveryImage.md)
Copies or updates a Windows RE or Windows PE image.

### [Test-RecoveryPartition](Test-RecoveryPartition.md)
Tests a recovery partition for size, layout, and contents.

