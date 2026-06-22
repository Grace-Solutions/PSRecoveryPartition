# PSRecoveryPartition

PowerShell module for managing Windows recovery partitions, Windows Recovery Environment (WinRE), and recovery boot entries on Windows 10, Windows 11, and Windows Server. Partition operations are driven directly against `\\.\PhysicalDriveN` and `\\?\Volume{...}` handles using Win32 IOCTLs (`IOCTL_DISK_GET_DRIVE_LAYOUT_EX`, `IOCTL_DISK_SET_DRIVE_LAYOUT_EX`, `IOCTL_DISK_GROW_PARTITION`, `FSCTL_EXTEND_VOLUME`, `FSCTL_SHRINK_VOLUME`) and `fmifs!FormatEx`; the module falls back to controlled execution of Microsoft inbox tools (`reagentc.exe`, `bcdedit.exe`, and the sanctioned `diskpart.exe` exception for the MBR `0x27` type byte) only where no first-class API is available.

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

Every result object discloses `ExecutionMethod` (Native, PInvoke, or ProcessFallback) and `ProcessFallbackUsed` so callers can audit which path was taken. The default for partition operations is `Native`.

### Native IOCTL architecture

Partition operations are driven directly against Win32 device handles. There is no dependency on the in-box `Storage` module, on CIM (`MSFT_Disk`, `MSFT_Partition`, `MSFT_Volume`), or on `Get-Partition` / `New-Partition` / `Resize-Partition` / `Remove-Partition` / `Format-Volume` cmdlets at runtime — those cmdlets are not consulted and their `PSObject` outputs are not accepted as input. The native layer lives under `src/PSRecoveryPartition/Native/`:

- `Win32DiskInfoReader` enumerates disks and reads partition tables through `IOCTL_DISK_GET_DRIVE_LAYOUT_EX`, `IOCTL_DISK_GET_DRIVE_GEOMETRY_EX`, and `IOCTL_DISK_GET_LENGTH_INFO`.
- `Win32VolumeMapper` walks `FindFirstVolumeW` / `FindNextVolumeW`, calls `IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS`, and associates each `\\?\Volume{GUID}\` with its hosting `(disk, starting offset)` partition.
- `Win32PartitionWriter` mutates the partition table via `IOCTL_DISK_SET_DRIVE_LAYOUT_EX`, grows partitions via `IOCTL_DISK_GROW_PARTITION`, and extends or shrinks file systems via `FSCTL_EXTEND_VOLUME` and the `FSCTL_SHRINK_VOLUME` prepare / commit / abort sequence.
- `Win32VolumeFormatter` performs synchronous formatting through `fmifs!FormatEx`.
- `DeviceHandleFactory` opens `\\.\PhysicalDriveN` and `\\?\Volume{GUID}` handles with the correct sharing and FSCTL access masks.

All partition offsets and lengths are aligned to a 1 MiB (1,048,576-byte) grid. The preferred call order is:

```text
Win32 IOCTL / FSCTL on \\.\PhysicalDriveN and \\?\Volume{GUID}\ handles
fmifs!FormatEx (volume formatting)
SetVolumeMountPointW / DeleteVolumeMountPointW (mount management)
Internal process fallback (reagentc.exe, bcdedit.exe, mountvol.exe, and diskpart.exe only for the MBR 0x27 exception)
```

`diskpart.exe` is otherwise avoided: it is script-file driven, has no machine-readable output, no structured error propagation, and is difficult to invoke deterministically from a service context.

### MBR partition type 0x27

On GPT disks the recovery partition is created with the well-known GPT type GUID `{de94bba4-06d1-4d40-a16a-bfd50179d6ac}` written directly into the partition entry through `IOCTL_DISK_SET_DRIVE_LAYOUT_EX`. The `GPT_BASIC_DATA_ATTRIBUTE_NO_DRIVE_LETTER`, `GPT_BASIC_DATA_ATTRIBUTE_HIDDEN`, and `GPT_BASIC_DATA_ATTRIBUTE_NO_AUTOMOUNT` attribute bits are set in the same call, so the Windows volume manager does not auto-assign a drive letter to the partition.

On MBR disks the module writes the partition type byte `0x27` (Windows Recovery) directly into the MBR entry via `IOCTL_DISK_SET_DRIVE_LAYOUT_EX`. If the kernel rejects the IOCTL on a given platform (older builds reject non-recognized MBR type bytes for slot 1 on system disks), `DiskpartMbrTypeSetter` is invoked as the single sanctioned `diskpart.exe` fallback. The fallback emits a one-line script (`select disk N` / `select partition M` / `set id=27 override`) through the standard `ProcessExecution` helper and the resulting `RecoveryProcessExecutionResult` is attached to the cmdlet output and surfaces as `ExecutionMethod = ProcessFallback`.

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
