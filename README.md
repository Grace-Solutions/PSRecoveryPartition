# PSRecoveryPartition

PowerShell module for managing Windows recovery partitions, Windows Recovery Environment (WinRE), and recovery boot entries on Windows 10, Windows 11, and Windows Server. The module prefers native Windows APIs, the Storage module, CIM, and WMI and falls back to controlled execution of Microsoft inbox tools (`reagentc.exe`, `bcdedit.exe`) only where no first-class API is available.

## Requirements

- Windows 10 or later (client) / Windows Server 2016 or later
- PowerShell 5.1 (Desktop) or PowerShell 7 (Core)
- Administrative privileges for any cmdlet that mutates state

## Installation (from source)

```powershell
.\build.ps1 -Configuration Release
Import-Module .\Module\PSRecoveryPartition\PSRecoveryPartition.psd1
```

## Cmdlet index

| Cmdlet | Synopsis |
|--------|----------|
| [Get-RecoveryPartition](docs/Get-RecoveryPartition.md) | Discovers recovery partitions on local disks. |
| [New-RecoveryPartition](docs/New-RecoveryPartition.md) | Creates a recovery partition on a target disk. |
| [Set-RecoveryPartition](docs/Set-RecoveryPartition.md) | Updates the metadata of an existing recovery partition. |
| [Resize-RecoveryPartition](docs/Resize-RecoveryPartition.md) | Resizes a recovery partition to the requested size. |
| [Remove-RecoveryPartition](docs/Remove-RecoveryPartition.md) | Removes a recovery partition. |
| [Mount-RecoveryPartition](docs/Mount-RecoveryPartition.md) | Mounts a recovery partition at a folder mount point. |
| [Dismount-RecoveryPartition](docs/Dismount-RecoveryPartition.md) | Removes a folder mount point from a recovery partition. |
| [Test-RecoveryPartition](docs/Test-RecoveryPartition.md) | Tests a recovery partition for size, layout, and contents. |
| [New-RecoveryPartitionPlan](docs/New-RecoveryPartitionPlan.md) | Builds an idempotent end-to-end recovery partition plan. |
| [Invoke-RecoveryPartitionPlan](docs/Invoke-RecoveryPartitionPlan.md) | Executes a recovery partition plan idempotently. |
| [Get-WindowsRecoveryImage](docs/Get-WindowsRecoveryImage.md) | Discovers Windows RE or Windows PE image files. |
| [Set-WindowsRecoveryImage](docs/Set-WindowsRecoveryImage.md) | Copies or updates a Windows RE or Windows PE image. |
| [Save-RecoveryBootImage](docs/Save-RecoveryBootImage.md) | Downloads or copies a recovery boot image to a local destination. |
| [Get-WindowsRecoveryEnvironment](docs/Get-WindowsRecoveryEnvironment.md) | Returns the current Windows Recovery Environment configuration. |
| [Set-WindowsRecoveryEnvironment](docs/Set-WindowsRecoveryEnvironment.md) | Registers a WindowsRE image and/or schedules a boot to RE. |
| [Enable-WindowsRecoveryEnvironment](docs/Enable-WindowsRecoveryEnvironment.md) | Enables Windows Recovery Environment. |
| [Disable-WindowsRecoveryEnvironment](docs/Disable-WindowsRecoveryEnvironment.md) | Disables Windows Recovery Environment. |
| [Get-WindowsRecoveryBootEntry](docs/Get-WindowsRecoveryBootEntry.md) | Discovers recovery boot entries. |
| [New-WindowsRecoveryBootEntry](docs/New-WindowsRecoveryBootEntry.md) | Creates a recovery boot entry idempotently. |
| [Remove-WindowsRecoveryBootEntry](docs/Remove-WindowsRecoveryBootEntry.md) | Removes a recovery boot entry idempotently. |
| [Set-WindowsRecoveryEntryPoint](docs/Set-WindowsRecoveryEntryPoint.md) | Configures push-button reset, a boot entry, or both. |

See [docs/DesignSpecification.md](docs/DesignSpecification.md) for the full design.

## License

Released under the [GNU Affero General Public License v3](https://www.gnu.org/licenses/agpl-3.0.html). Copyright (c) 2026 Grace Solutions.
