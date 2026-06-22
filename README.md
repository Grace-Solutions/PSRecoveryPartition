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

## Quickstart

The plan-based workflow is the recommended entry point. Build a plan, review it, and apply it idempotently:

```powershell
$Plan = New-RecoveryPartitionPlan -DiskNumber 0 -SizePercent 2 `
    -WindowsREImagePath 'C:\RecoveryImages\winre.wim' `
    -BootImagePath      'C:\RecoveryImages\boot.wim' `
    -EntryPointMode     Both

$Plan.Steps | Format-Table Action, Target, AlreadySatisfied, Description
$Plan | Invoke-RecoveryPartitionPlan -PassThru
```

The plan inspects the disk layout, surfaces warnings (for example when the recovery partition is in front of the OS partition or there is no trailing free space), and converts unsafe `Resize-Partition` steps to `Skip` unless `-Force` is supplied on `Invoke-RecoveryPartitionPlan`.

## Architecture

Cmdlets are thin C# shells over a small internal engine layer:

- `RecoveryPartitionEngine` owns partition lifecycle (create, resize, remove, mount).
- `WinReEngine` wraps `reagentc.exe` for WindowsRE registration and `BootToRE` scheduling.
- `BcdEditEngine` wraps `bcdedit.exe` for BCD boot entry management.
- `RecoveryPartitionLayoutAnalyzer` inspects disk geometry around the target partition and emits a `RecoveryPartitionLayoutAnalysis` with `CanGrowInPlace`, `CanShrinkInPlace`, `CanRemoveSafely`, neighbour information, leading/trailing free space, and human-readable warnings.
- `RecoveryPlanBuilder` translates the layout, requested size, and image inputs into a `RecoveryPartitionPlan` whose steps are dispatched by `Invoke-RecoveryPartitionPlan`.

Every result object discloses `ExecutionMethod` (Native, Storage, Cim, Wmi, PInvoke, or Process) and `ProcessFallbackUsed` so callers can audit which path was taken.

### Storage module dependency

Partition operations currently flow through the in-box `Storage` module (`Get-Disk`, `New-Partition`, `Format-Volume`, `Set-Partition`, `Resize-Partition`, `Remove-Partition`, `Add-PartitionAccessPath`) invoked from C# via `StorageInvoker`. The `Storage` module ships with every supported Windows version and is itself a thin wrapper around the `MSFT_Disk`, `MSFT_Partition`, and `MSFT_Volume` CIM classes in `ROOT\Microsoft\Windows\Storage`. The module can therefore be hardened over time to call those CIM classes directly through `Microsoft.Management.Infrastructure` and drop the Storage module dependency without changing public behaviour. The preferred call order documented in [docs/DesignSpecification.md](docs/DesignSpecification.md) is:

```text
Native C# API
PowerShell Storage objects (current default for partition operations)
CIM / WMI (MSFT_Partition, MSFT_Disk, MSFT_Volume)
Windows API / PInvoke (IOCTL_DISK_*)
Internal process fallback (reagentc.exe, bcdedit.exe, mountvol.exe)
```

`diskpart.exe` is intentionally **not** in the fallback list: it is script-file driven, has no machine-readable output, no structured error propagation, and is difficult to invoke deterministically from a service context.

### MBR partition type 0x27

On GPT disks the recovery partition is created with the well-known GPT type GUID `{de94bba4-06d1-4d40-a16a-bfd50179d6ac}` directly, and no warning is emitted. On MBR disks the PowerShell `Storage` module's `New-Partition -MbrType` parameter does not include `0x27` (Windows Recovery) in its enum; the valid values are `FAT12`, `FAT16`, `Extended`, `Huge`, `IFS`, `FAT32`, `ExtendedLBA`. The module therefore creates the partition as `IFS` (`0x07`), assigns the `RECOVERY` label, and emits a warning that the on-disk MBR type byte is `0x07` rather than `0x27`. Functionally the partition is still recognised by the module (it matches by label and falls back to GPT type for discovery), but downstream tooling that keys strictly on the `0x27` byte will not see it. A future enhancement will write the type byte directly via CIM (`Set-CimInstance` on `MSFT_Partition`) or `IOCTL_DISK_SET_PARTITION_INFO_EX`; until then the warning documents the gap honestly. The module does not shell out to `diskpart.exe` to set the type for the reasons listed above.

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
