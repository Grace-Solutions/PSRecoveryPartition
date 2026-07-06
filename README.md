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

Every mutating cmdlet supports `-Verbose` for step-by-step logging and `-WhatIf`. The examples below use a splatted `OrderedDictionary` so each parameter is explicit and easy to diff.

### Create a recovery partition

This only creates and formats the partition; chain the image and boot-entry cmdlets below to populate it. The partition is always placed **after** the existing partitions (never before the OS, which would require moving the OS partition). `-CreationMode` controls how trailing free space is obtained: `UseTrailingFreeSpace` (default) appends into existing end-of-disk free space and never touches existing data; `ShrinkToFit` shrinks the last partition (typically the OS) by the shortfall first; `RequireEmptyDisk` only creates on an empty disk.

```powershell
$NewRecoveryPartitionParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $NewRecoveryPartitionParameters.DiskNumber  = 0
    $NewRecoveryPartitionParameters.SizePercent = 2
    $NewRecoveryPartitionParameters.Label       = 'RECOVERY'
    $NewRecoveryPartitionParameters.FileSystem  = 'NTFS'
    $NewRecoveryPartitionParameters.PassThru    = $True
    $NewRecoveryPartitionParameters.Verbose     = $True

$NewRecoveryPartitionResult = New-RecoveryPartition @NewRecoveryPartitionParameters

Write-Output -InputObject ($NewRecoveryPartitionResult)
```

### Download a recovery image (conditional, ETag/Last-Modified aware)

`Save-RecoveryBootImage` issues a conditional `If-Modified-Since` request derived from the local file's timestamp: an unchanged remote image returns HTTP 304 and is **not** re-downloaded, and each successful download stamps the file with the server's `Last-Modified` so the next run can skip it. Set `-Force $True` to download unconditionally. `-DestinationPath` may be a folder (the source/URL leaf name is kept) or a full file path (the download is saved and renamed to that name — useful when the URL has no filename).

```powershell
$SaveRecoveryBootImageParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $SaveRecoveryBootImageParameters.SourceUri       = 'https://images.example.com/winre/download'
    $SaveRecoveryBootImageParameters.DestinationPath = 'C:\RecoveryImages\winre.wim'
    $SaveRecoveryBootImageParameters.Headers         = @{ Authorization = 'Bearer <token>' }
    $SaveRecoveryBootImageParameters.Force           = $False
    $SaveRecoveryBootImageParameters.PassThru        = $True
    $SaveRecoveryBootImageParameters.Verbose         = $True

$SaveRecoveryBootImageResult = Save-RecoveryBootImage @SaveRecoveryBootImageParameters

Write-Output -InputObject ($SaveRecoveryBootImageResult)
```

### Create a custom recovery boot entry

Each variant is self-contained — the single-liners gather the inputs and the splat consumes them. `Get-RecoveryPartition` defaults to `-DetectionMode CurrentOSDisk`, so it returns the recovery partition on the running OS disk rather than fanning out across every disk on a dual-disk or dual-boot machine.

**Variant A — WIM (ramdisk) boot, staged at the volume root (`StagingRelativePath` of `''` or `'\'`) of a mounted volume (`Volume:\`).** The image is staged as-is and RAM-booted; `boot.sdi` is resolved automatically.

```powershell
$BootImage = Save-RecoveryBootImage -SourceUri 'https://images.example.com/winpe/boot.wim' -DestinationPath 'C:\RecoveryImages\' -PassThru -Verbose

$NewWindowsRecoveryBootEntryParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $NewWindowsRecoveryBootEntryParameters.BootImagePath       = $BootImage.ImagePath
    $NewWindowsRecoveryBootEntryParameters.TargetPath          = 'R:\'
    $NewWindowsRecoveryBootEntryParameters.StagingRelativePath = ''
    $NewWindowsRecoveryBootEntryParameters.Name                = 'Grace Solutions WinPE'
    $NewWindowsRecoveryBootEntryParameters.BootTimeout         = [TimeSpan]::FromSeconds(10)
    $NewWindowsRecoveryBootEntryParameters.BootEntryVisibility = 'Visible'
    $NewWindowsRecoveryBootEntryParameters.PassThru            = $True
    $NewWindowsRecoveryBootEntryParameters.Verbose             = $True

$NewWindowsRecoveryBootEntryResult = New-WindowsRecoveryBootEntry @NewWindowsRecoveryBootEntryParameters

Write-Output -InputObject ($NewWindowsRecoveryBootEntryResult)
```

**Variant B — expanded (flat / non-RAM) boot on the recovery partition, targeted by its `\\?\Volume{GUID}\` / `\\?\GLOBALROOT` device path (no drive letter, no mount).** `-ExpandBootImage` applies the selected image index flat onto the **partition root** (Microsoft flat-boot layout: `systemroot=\Windows`), so `-StagingRelativePath` is ignored. Add `-FormatTargetPartition` to format the partition (NTFS, quick) first so the image lands on a clean volume — this **destroys all existing content on that partition**, so only use it on a partition dedicated to the flat image. Flat boot is best-effort; the ramdisk WIM boot (Variant A) is the supported default.

```powershell
$OSRecoveryPartition = Get-RecoveryPartition -DetectionMode CurrentOSDisk -Verbose
$BootImage           = Save-RecoveryBootImage -SourceUri 'https://images.example.com/winpe/boot.wim' -DestinationPath 'C:\RecoveryImages\' -PassThru -Verbose

$NewWindowsRecoveryBootEntryParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $NewWindowsRecoveryBootEntryParameters.BootImagePath       = $BootImage.ImagePath
    $NewWindowsRecoveryBootEntryParameters.RecoveryPartition   = $OSRecoveryPartition
    $NewWindowsRecoveryBootEntryParameters.ExpandBootImage     = $True
    $NewWindowsRecoveryBootEntryParameters.ImageIndex          = 1
    $NewWindowsRecoveryBootEntryParameters.FormatTargetPartition = $True
    $NewWindowsRecoveryBootEntryParameters.Name                = 'Grace Solutions Recovery'
    $NewWindowsRecoveryBootEntryParameters.BootTimeout         = [TimeSpan]::FromSeconds(10)
    $NewWindowsRecoveryBootEntryParameters.BootEntryVisibility = 'Visible'
    $NewWindowsRecoveryBootEntryParameters.PassThru            = $True
    $NewWindowsRecoveryBootEntryParameters.Verbose             = $True

$NewWindowsRecoveryBootEntryResult = New-WindowsRecoveryBootEntry @NewWindowsRecoveryBootEntryParameters

Write-Output -InputObject ($NewWindowsRecoveryBootEntryResult)
```

**Variant C — WIM (ramdisk) boot of a network image staged into a named subfolder on the recovery partition, appended last (`-AddLast`) and replacing any existing entry of the same name (`-Force`).** The image is copied straight onto the partition through its `\\?\Volume{GUID}\` path with no drive letter or mount; a `boot.sdi` is resolved automatically.

```powershell
$RecoveryPartition = Get-RecoveryPartition -DetectionMode CurrentOSDisk

$NewWindowsRecoveryBootEntryParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $NewWindowsRecoveryBootEntryParameters.BootImagePath       = '\\fileserver\Images\Generic_x64.wim'
    $NewWindowsRecoveryBootEntryParameters.ImageIndex          = 1
    $NewWindowsRecoveryBootEntryParameters.RecoveryPartition   = $RecoveryPartition
    $NewWindowsRecoveryBootEntryParameters.StagingRelativePath = '\Generic_x64'
    $NewWindowsRecoveryBootEntryParameters.Name                = 'Remote Recovery'
    $NewWindowsRecoveryBootEntryParameters.BootTimeout         = [TimeSpan]::FromSeconds(5)
    $NewWindowsRecoveryBootEntryParameters.BootEntryVisibility = 'Visible'
    $NewWindowsRecoveryBootEntryParameters.AddLast             = $True
    $NewWindowsRecoveryBootEntryParameters.PassThru            = $True
    $NewWindowsRecoveryBootEntryParameters.Verbose             = $True
    $NewWindowsRecoveryBootEntryParameters.Force               = $True

$NewWindowsRecoveryBootEntryResult = New-WindowsRecoveryBootEntry @NewWindowsRecoveryBootEntryParameters

Write-Output -InputObject ($NewWindowsRecoveryBootEntryResult)
```

## Architecture

Cmdlets are thin C# shells over a small internal engine layer:

- `RecoveryPartitionEngine` owns partition lifecycle (create, resize, remove, mount).
- `WinReEngine` wraps `reagentc.exe` for WindowsRE registration and `BootToRE` scheduling.
- `BcdEditEngine` wraps `bcdedit.exe` for BCD boot entry management.
- `RecoveryPartitionLayoutAnalyzer` inspects disk geometry around the target partition and emits a `RecoveryPartitionLayoutAnalysis` with `CanGrowInPlace`, `CanShrinkInPlace`, `CanRemoveSafely`, neighbour information, leading/trailing free space, and human-readable warnings that `Resize-RecoveryPartition` and `Remove-RecoveryPartition` gate on.

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
| [Get-WindowsRecoveryImage](docs/Get-WindowsRecoveryImage.md) | Discovers Windows RE or Windows PE image files. |
| [Set-WindowsRecoveryImage](docs/Set-WindowsRecoveryImage.md) | Copies or updates a Windows RE or Windows PE image. |
| [Save-RecoveryBootImage](docs/Save-RecoveryBootImage.md) | Downloads or copies a recovery boot image to a local destination. |
| [Get-WindowsRecoveryEnvironment](docs/Get-WindowsRecoveryEnvironment.md) | Returns the current Windows Recovery Environment configuration. |
| [Set-WindowsRecoveryEnvironment](docs/Set-WindowsRecoveryEnvironment.md) | Registers a WindowsRE image and/or schedules a boot to RE. |
| [Enable-WindowsRecoveryEnvironment](docs/Enable-WindowsRecoveryEnvironment.md) | Enables Windows Recovery Environment. |
| [Disable-WindowsRecoveryEnvironment](docs/Disable-WindowsRecoveryEnvironment.md) | Disables Windows Recovery Environment. |
| [Get-WindowsRecoveryBootEntry](docs/Get-WindowsRecoveryBootEntry.md) | Discovers recovery boot entries. |
| [New-WindowsRecoveryBootEntry](docs/New-WindowsRecoveryBootEntry.md) | Creates a custom recovery boot entry from a boot image (WIM ramdisk or flat expand). |
| [Set-WindowsRecoveryBootImage](docs/Set-WindowsRecoveryBootImage.md) | Replaces the boot image on an existing recovery boot entry. |
| [Remove-WindowsRecoveryBootEntry](docs/Remove-WindowsRecoveryBootEntry.md) | Removes a recovery boot entry idempotently. |
| [Set-WindowsRecoveryEntryPoint](docs/Set-WindowsRecoveryEntryPoint.md) | Configures push-button reset / Windows RE as a recovery entry point. |

See [docs/DesignSpecification.md](docs/DesignSpecification.md) for the full design.

## License

Released under the [GNU Affero General Public License v3](https://www.gnu.org/licenses/agpl-3.0.html). Copyright (c) 2026 Grace Solutions.
