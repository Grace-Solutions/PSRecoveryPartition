# PSRecoveryPartition Specification Updates

## Remove Public Shell-Out Parameter

Do not expose a public `-AllowShellOut` parameter anywhere in `PSRecoveryPartition`.

Remove this parameter from all public cmdlets:

```powershell
-AllowShellOut
```

Shell-out behavior is an implementation detail, not a public module contract.

The module prefers this order for partition operations:

```text
Win32 IOCTL / FSCTL on \\.\PhysicalDriveN and \\?\Volume{GUID}\ handles
fmifs!FormatEx for volume formatting
SetVolumeMountPointW / DeleteVolumeMountPointW for mount management
Internal process fallback (reagentc.exe, bcdedit.exe, mountvol.exe, and diskpart.exe only for the MBR 0x27 exception)
```

The in-box `Storage` module and the `MSFT_Disk` / `MSFT_Partition` / `MSFT_Volume` CIM classes are no longer consulted at runtime, and `PSObject` outputs from `Get-Partition` / `Get-Disk` / `Get-Volume` are not accepted as input to this module's cmdlets.

If the only practical method for a required operation is a Microsoft inbox executable such as `reagentc.exe`, `bcdedit.exe`, or `mountvol.exe`, the module may use the internal process fallback without exposing that decision to the user. `diskpart.exe` is reserved for the single sanctioned exception of writing the MBR `0x27` partition type byte on platforms where `IOCTL_DISK_SET_DRIVE_LAYOUT_EX` rejects that byte for a system disk.

The result object must still disclose the implementation path:

```text
ExecutionMethod = Native
ExecutionMethod = ProcessFallback
```

The `Storage`, `CIM`, and `WMI` enum values are retained on `RecoveryExecutionMethod` for source compatibility, marked with `[Obsolete]` (compiler warning only), and will be removed in a future major version. They are never produced by the engine.

Example result fields:

```csharp
public string ExecutionMethod { get; set; }
public bool ProcessFallbackUsed { get; set; }
public IList<RecoveryProcessExecutionResult> ProcessResults { get; set; }
```

---

## Internal Process Fallback Requirements

The internal process fallback must use `System.Diagnostics.ProcessStartInfo`.

Required behavior:

```text
UseShellExecute = false
RedirectStandardOutput = true
RedirectStandardError = true
CreateNoWindow = true
WindowStyle = Hidden
No flashing windows
No interactive prompts
Timeout support
Acceptable exit code support
Environment variable override support
Sanitized verbose logging
Rich process result object
```

The fallback must never use:

```text
cmd.exe /c
powershell.exe
pwsh.exe
Start-Process without output capture
ShellExecute
Visible windows
Interactive prompts
```

unless a future version explicitly adds an opt-in troubleshooting mode.

Required internal type:

```csharp
public sealed class RecoveryProcessExecutionRequest
{
    public FileInfo FilePath { get; set; }
    public IList<string> ArgumentList { get; set; }
    public IList<int> AcceptableExitCodeList { get; set; }
    public string WindowStyle { get; set; }
    public string Priority { get; set; }
    public bool LogOutput { get; set; }
    public TimeSpan ExecutionTimeout { get; set; }
    public TimeSpan ExecutionTimeoutInterval { get; set; }
    public IDictionary<string, string> EnvironmentVariables { get; set; }
    public bool SecureArgumentList { get; set; }
}
```

Required return type:

```csharp
public sealed class RecoveryProcessExecutionResult
{
    public FileInfo FilePath { get; set; }
    public IList<string> ArgumentList { get; set; }
    public int ExitCode { get; set; }
    public bool ExitCodeAccepted { get; set; }
    public string StandardOutput { get; set; }
    public string StandardError { get; set; }
    public bool TimedOut { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
}
```

If a fallback process fails, the error message must plainly identify the failed tool, exit code, and sanitized standard error.

Example:

```text
The recovery boot entry could not be created because bcdedit.exe returned exit code 1. Standard error: The boot configuration data store could not be opened.
```

Do not expose sensitive arguments in logs.

---

## Public Shell-Out Language

Do not document shell-out as a user-facing feature.

Do not say:

```text
Use -AllowShellOut.
```

Say:

```text
PSRecoveryPartition uses native Windows APIs, Storage objects, CIM, WMI, and controlled Microsoft inbox process fallback where required.
```

The fallback is part of the supported implementation.

---

## Entry Point Modes

Recovery entry-point support must allow three modes:

```csharp
public enum RecoveryEntryPointMode
{
    PushButtonReset,
    BootEntry,
    Both
}
```

Public parameter:

```powershell
-EntryPointMode <PushButtonReset|BootEntry|Both>
```

Default:

```text
BootEntry
```

When `-EntryPointMode PushButtonReset` is used, the module should configure Windows Recovery Environment / push-button reset registration where supported.

When `-EntryPointMode BootEntry` is used, the module should create or update a boot entry that points to the configured recovery image.

When `-EntryPointMode Both` is used, the module should configure both paths idempotently.

---

## Push-Button Reset Support

Push-button reset must be treated as a first-class entry-point mode separate from boot-menu entries.

Initial push-button behavior:

```text
Detect Windows RE status.
Detect registered WinRE image location.
Set or update Windows RE image path.
Enable Windows RE where required.
Optionally configure push-button reset customizations when supported.
Return rich status.
```

Microsoft’s supported configuration surface for this path is `REAgentC.exe`, so internal process fallback may be required.

Do not expose that implementation detail as a public switch.

Required cmdlets involved:

```powershell
Get-WindowsRecoveryEnvironment
Set-WindowsRecoveryEnvironment
Enable-WindowsRecoveryEnvironment
Disable-WindowsRecoveryEnvironment
```

These can be added to the initial cmdlet list instead of being reserved future commands.

Required object:

```csharp
public sealed class WindowsRecoveryEnvironmentInfo
{
    public bool Enabled { get; set; }
    public DirectoryInfo WindowsRELocation { get; set; }
    public FileInfo WindowsREImagePath { get; set; }
    public string BootConfigurationDataIdentifier { get; set; }
    public string RecoveryImageLocation { get; set; }
    public string RecoveryImageIndex { get; set; }
    public string CustomImageLocation { get; set; }
    public string CustomImageIndex { get; set; }
    public string StatusText { get; set; }
    public bool ProcessFallbackUsed { get; set; }
}
```

---

## Push-Button Reset Action Conversion

If the user supplies a simple string describing the push-button action, the module must convert it to the correct internal action model.

Public parameter:

```powershell
-PushButtonAction <string>
```

Supported initial values:

```text
Reset
Refresh
FactoryReset
AdvancedStartup
BootToRE
```

The module must normalize friendly values case-insensitively.

Examples:

```text
"reset" -> Reset
"factory reset" -> FactoryReset
"advanced startup" -> AdvancedStartup
"boot to re" -> BootToRE
"winre" -> BootToRE
```

Internal enum:

```csharp
public enum WindowsRecoveryPushButtonAction
{
    Reset,
    Refresh,
    FactoryReset,
    AdvancedStartup,
    BootToRE
}
```

If a requested action is not supported by the active Windows version or available tooling, the module must fail with a plain English error.

Example:

```text
The requested push-button action FactoryReset could not be configured because the current Windows installation does not expose a supported configuration path for that action.
```

Do not guess silently.

Do not create undocumented registry hacks without an explicit future design decision.

---

## Boot Entry Support

Boot-entry support must remain separate from push-button reset support.

Boot entry modes:

```csharp
public enum RecoveryBootEntryVisibility
{
    Visible,
    Hidden
}
```

Public parameter:

```powershell
-BootEntryVisibility <Visible|Hidden>
```

Default:

```text
Visible
```

A hidden boot entry should be created only when the supported BCD configuration path is understood and tested.

If the boot entry cannot be hidden safely, the module must create a visible entry or fail depending on the requested mode.

Required public parameter:

```powershell
-BootTimeout <TimeSpan>
```

Default:

```text
00:00:10
```

Required public parameter:

```powershell
-BootImagePath <FileInfo>
```

Supported image inputs:

```text
Winre.wim
Boot.wim
Custom Windows PE WIM
```

Required return object:

```csharp
public sealed class WindowsRecoveryBootEntryInfo
{
    public string Identifier { get; set; }
    public string Name { get; set; }
    public FileInfo BootImagePath { get; set; }
    public TimeSpan BootTimeout { get; set; }
    public RecoveryBootEntryVisibility Visibility { get; set; }
    public bool IsDefault { get; set; }
    public bool IsRecoveryEntry { get; set; }
    public bool ProcessFallbackUsed { get; set; }
    public DateTimeOffset DiscoveredAtUtc { get; set; }
}
```

---

## Boot Entry Cmdlet Updates

Update `New-WindowsRecoveryBootEntry`:

```powershell
New-WindowsRecoveryBootEntry `
    -BootImagePath <FileInfo> `
    [-Name <string>] `
    [-BootTimeout <TimeSpan>] `
    [-BootEntryVisibility <Visible|Hidden>] `
    [-SetDefault] `
    [-AddLast] `
    [-InputObject <object>] `
    [-Force] `
    [-PassThru] `
    [-WhatIf] `
    [-Confirm]
```

No `-AllowShellOut`.

Update `Get-WindowsRecoveryBootEntry`:

```powershell
Get-WindowsRecoveryBootEntry `
    [-Name <string>] `
    [-BootImagePath <FileInfo>] `
    [-IncludeHidden] `
    [-IncludeAll]
```

No `-AllowShellOut`.

Update `Remove-WindowsRecoveryBootEntry`:

```powershell
Remove-WindowsRecoveryBootEntry `
    [-InputObject <WindowsRecoveryBootEntryInfo>] `
    [-Identifier <string>] `
    [-Name <string>] `
    [-PassThru] `
    [-WhatIf] `
    [-Confirm]
```

No `-AllowShellOut`.

---

## Combined Recovery Entry Point Cmdlet

Add a high-level cmdlet for configuring recovery entry points.

Cmdlet:

```powershell
Set-WindowsRecoveryEntryPoint
```

Purpose:

```text
Configure push-button reset, a boot entry, or both as recovery entry points.
```

Parameters:

```powershell
Set-WindowsRecoveryEntryPoint `
    -EntryPointMode <PushButtonReset|BootEntry|Both> `
    [-BootImagePath <FileInfo>] `
    [-WindowsREImagePath <FileInfo>] `
    [-PushButtonAction <string>] `
    [-Name <string>] `
    [-BootTimeout <TimeSpan>] `
    [-BootEntryVisibility <Visible|Hidden>] `
    [-SetDefault] `
    [-InputObject <object>] `
    [-Force] `
    [-PassThru] `
    [-WhatIf] `
    [-Confirm]
```

Behavior:

```text
Validate the requested entry-point mode.
Configure push-button reset when requested.
Configure boot entry when requested.
Avoid duplicate boot entries.
Avoid unnecessary WinRE reconfiguration.
Return a rich combined result.
```

Return type:

```csharp
public sealed class WindowsRecoveryEntryPointResult
{
    public RecoveryEntryPointMode EntryPointMode { get; set; }
    public WindowsRecoveryEnvironmentInfo RecoveryEnvironment { get; set; }
    public WindowsRecoveryBootEntryInfo BootEntry { get; set; }
    public bool Changed { get; set; }
    public bool Success { get; set; }
    public bool ProcessFallbackUsed { get; set; }
    public IList<string> ActionsTaken { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
}
```

---

## Size Percent Parameter Set Rule

Remove `-EnablePercentSizing`.

The presence of `-SizePercent` is enough to imply percentage-based sizing.

`-SizeBytes` and `-SizePercent` must be mutually exclusive parameter sets.

Parameter sets:

```text
ExplicitSize
PercentSize
DefaultSize
```

Example:

```powershell
New-RecoveryPartition -DiskNumber 0 -SizeBytes 1073741824
```

Example:

```powershell
New-RecoveryPartition -DiskNumber 0 -SizePercent 2
```

Validation:

```text
SizePercent must be greater than 0.
SizePercent must be less than or equal to a safe maximum.
SizeBytes and SizePercent cannot be used together.
If neither is supplied, use the default size.
```

Recommended safe maximum:

```text
10
```

Default size:

```text
1 GiB
```

Update all affected cmdlets:

```text
New-RecoveryPartition
Resize-RecoveryPartition
Get-RecoveryPartitionPlan
```

Remove:

```powershell
-EnablePercentSizing
```

from all public cmdlets, docs, examples, and tests.

---

## Documentation Generation

Help must be generated with PlatyPS.

Required docs source:

```text
docs/
```

Generated help output:

```text
Module/PSRecoveryPartition/en-US/
```

Required build behavior:

```text
Generate markdown help with PlatyPS.
Update markdown help when cmdlet signatures change.
Generate external help from markdown.
Fail the build if public cmdlets are missing help.
Fail the build if README cmdlet links point to missing files.
```

Build script switches:

```powershell
-GenerateHelp
-UpdateHelp
-ValidateHelp
```

Example build:

```powershell
.\build.ps1 -Clean -Restore -RunTests -GenerateHelp -ValidateHelp -CreateRelease
```

---

## README Cmdlet Documentation Index

The README must include a generated command index linking to each cmdlet help document.

Required section:

```markdown
## Cmdlet Documentation

| Cmdlet | Description |
| --- | --- |
| [Get-RecoveryPartition](docs/Get-RecoveryPartition.md) | Discovers recovery partitions and returns rich recovery partition objects. |
| [New-RecoveryPartition](docs/New-RecoveryPartition.md) | Creates a recovery partition idempotently. |
| [Set-RecoveryPartition](docs/Set-RecoveryPartition.md) | Updates recovery partition metadata idempotently. |
| [Resize-RecoveryPartition](docs/Resize-RecoveryPartition.md) | Resizes a recovery partition idempotently. |
| [Remove-RecoveryPartition](docs/Remove-RecoveryPartition.md) | Removes a recovery partition safely. |
| [Mount-RecoveryPartition](docs/Mount-RecoveryPartition.md) | Adds a temporary access path to a recovery partition. |
| [Dismount-RecoveryPartition](docs/Dismount-RecoveryPartition.md) | Removes a temporary access path from a recovery partition. |
| [New-RecoveryPartitionPlan](docs/New-RecoveryPartitionPlan.md) | Builds an idempotent end-to-end recovery partition plan. |
| [Invoke-RecoveryPartitionPlan](docs/Invoke-RecoveryPartitionPlan.md) | Applies a recovery partition plan idempotently. |
| [Get-WindowsRecoveryImage](docs/Get-WindowsRecoveryImage.md) | Finds Windows RE or Windows PE images. |
| [Set-WindowsRecoveryImage](docs/Set-WindowsRecoveryImage.md) | Copies or updates a Windows RE or Windows PE image. |
| [Get-WindowsRecoveryEnvironment](docs/Get-WindowsRecoveryEnvironment.md) | Gets Windows Recovery Environment configuration. |
| [Set-WindowsRecoveryEnvironment](docs/Set-WindowsRecoveryEnvironment.md) | Sets Windows Recovery Environment configuration. |
| [Enable-WindowsRecoveryEnvironment](docs/Enable-WindowsRecoveryEnvironment.md) | Enables Windows Recovery Environment. |
| [Disable-WindowsRecoveryEnvironment](docs/Disable-WindowsRecoveryEnvironment.md) | Disables Windows Recovery Environment. |
| [Get-WindowsRecoveryBootEntry](docs/Get-WindowsRecoveryBootEntry.md) | Discovers recovery boot entries. |
| [New-WindowsRecoveryBootEntry](docs/New-WindowsRecoveryBootEntry.md) | Creates a recovery boot entry idempotently. |
| [Remove-WindowsRecoveryBootEntry](docs/Remove-WindowsRecoveryBootEntry.md) | Removes a recovery boot entry idempotently. |
| [Set-WindowsRecoveryEntryPoint](docs/Set-WindowsRecoveryEntryPoint.md) | Configures push-button reset, boot entry, or both. |
| [Save-RecoveryBootImage](docs/Save-RecoveryBootImage.md) | Downloads a recovery boot image locally. |
```

The command index must be generated or validated during build.

---

## Public Example Requirements

Every cmdlet help file must include:

```text
One single-line example.
One expanded OrderedDictionary-style splatted example where appropriate.
```

PowerShell examples must use `New-Object` for object construction.

Do not use:

```powershell
[Some.Type]::new()
```

Use:

```powershell
New-Object -TypeName 'Some.Type'
```

---

## Single-Line Example Requirement

Each cmdlet must have at least one practical one-line example.

Example:

```powershell
New-RecoveryPartition -DiskNumber 0 -SizeBytes 1073741824 -WindowsREImagePath 'C:\RecoveryImages\Winre.wim' -PassThru
```

Example:

```powershell
Set-WindowsRecoveryEntryPoint -EntryPointMode Both -BootImagePath 'C:\RecoveryImages\boot.wim' -WindowsREImagePath 'C:\RecoveryImages\winre.wim' -BootTimeout ([timespan]'00:00:10') -PassThru
```

---

## Expanded Example Formatting Requirement

Expanded examples must preserve indentation style like this:

```powershell
$StartProcessWithOutputParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $StartProcessWithOutputParameters.FilePath = "git.exe"
    $StartProcessWithOutputParameters.ArgumentList = New-Object -TypeName 'System.Collections.Generic.List[String]'
        $StartProcessWithOutputParameters.ArgumentList.Add('clone')
        $StartProcessWithOutputParameters.ArgumentList.Add('https://github.com/user/repo.git')
    $StartProcessWithOutputParameters.AcceptableExitCodeList = New-Object -TypeName 'System.Collections.Generic.List[System.String]'
        $StartProcessWithOutputParameters.AcceptableExitCodeList.Add('0')
    $StartProcessWithOutputParameters.WindowStyle = "Hidden"
    $StartProcessWithOutputParameters.Priority = "Normal"
    $StartProcessWithOutputParameters.LogOutput = $True
    $StartProcessWithOutputParameters.ExecutionTimeout = [Timespan]::FromMinutes(5)
    $StartProcessWithOutputParameters.ExecutionTimeoutInterval = [Timespan]::FromSeconds(5)
    $StartProcessWithOutputParameters.EnvironmentVariables = New-Object -TypeName 'System.Collections.Generic.Dictionary[System.String,System.String]' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
        $StartProcessWithOutputParameters.EnvironmentVariables['GIT_TERMINAL_PROMPT'] = 0
        $StartProcessWithOutputParameters.EnvironmentVariables['GCM_INTERACTIVE'] = 'Never'
    $StartProcessWithOutputParameters.SecureArgumentList = $False
    $StartProcessWithOutputParameters.Verbose = $True

$StartProcessWithOutputResult = Start-ProcessWithOutput @StartProcessWithOutputParameters

Write-Output -InputObject ($StartProcessWithOutputResult)
```

Module documentation should use this same formatting style for advanced examples.

Example for `Set-WindowsRecoveryEntryPoint`:

```powershell
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

Example for `New-RecoveryPartition`:

```powershell
$NewRecoveryPartitionParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $NewRecoveryPartitionParameters.DiskNumber = 0
    $NewRecoveryPartitionParameters.SizePercent = 2
    $NewRecoveryPartitionParameters.Label = 'Windows RE tools'
    $NewRecoveryPartitionParameters.FileSystem = 'NTFS'
    $NewRecoveryPartitionParameters.WindowsREImagePath = 'C:\RecoveryImages\winre.wim'
    $NewRecoveryPartitionParameters.PassThru = $True
    $NewRecoveryPartitionParameters.Verbose = $True

$NewRecoveryPartitionResult = New-RecoveryPartition @NewRecoveryPartitionParameters

Write-Output -InputObject ($NewRecoveryPartitionResult)
```

---

## Start-ProcessWithOutput Helper

The module should include an internal process execution helper modeled after `Start-ProcessWithOutput`.

This should be internal/private unless reused in a separate utility module.

Do not expose it as a public cmdlet in `PSRecoveryPartition` unless a future design decision makes it a shared public utility.

Required internal behavior:

```text
Accept ordered parameter object internally.
Support argument lists.
Support acceptable exit codes.
Support hidden windows.
Support redirected output.
Support redirected error.
Support timeouts.
Support environment variables.
Support secure argument suppression in logs.
Return rich result object.
```

Internal C# implementation must use `System.Diagnostics.ProcessStartInfo`.

Public documentation may include the Start-ProcessWithOutput example only as a design reference or advanced appendix, not as a required public command unless the helper is intentionally exported.

---

## Updated Public Cmdlet List

Initial public cmdlets:

```powershell
Get-RecoveryPartition
New-RecoveryPartition
Set-RecoveryPartition
Resize-RecoveryPartition
Remove-RecoveryPartition
Mount-RecoveryPartition
Dismount-RecoveryPartition
Test-RecoveryPartition
New-RecoveryPartitionPlan
Invoke-RecoveryPartitionPlan
Get-WindowsRecoveryImage
Set-WindowsRecoveryImage
Get-WindowsRecoveryEnvironment
Set-WindowsRecoveryEnvironment
Enable-WindowsRecoveryEnvironment
Disable-WindowsRecoveryEnvironment
Get-WindowsRecoveryBootEntry
New-WindowsRecoveryBootEntry
Remove-WindowsRecoveryBootEntry
Set-WindowsRecoveryEntryPoint
Save-RecoveryBootImage
```

No public cmdlet should include:

```powershell
-AllowShellOut
```

---

## Updated Acceptance Criteria

```text
No public cmdlet exposes -AllowShellOut.
Partition operations are driven through Win32 IOCTLs and FSCTLs on \\.\PhysicalDriveN and \\?\Volume{GUID}\ handles; the Storage PowerShell module and MSFT_* CIM classes are not consulted at runtime.
Volume formatting is performed through fmifs!FormatEx.
Mount management is performed through SetVolumeMountPointW and DeleteVolumeMountPointW.
diskpart.exe is invoked only as a sanctioned fallback for writing the MBR 0x27 partition type byte.
Internal process fallback is allowed when no native or PInvoke method is available.
Internal process fallback uses ProcessStartInfo.
Internal process fallback redirects standard output and standard error.
Internal process fallback uses CreateNoWindow.
Internal process fallback uses hidden windows.
Internal process fallback supports timeout handling.
Internal process fallback returns rich process result objects.
Public result objects disclose ProcessFallbackUsed.
EntryPointMode supports PushButtonReset, BootEntry, and Both.
Push-button reset configuration is supported as a first-class entry-point mode.
Boot entry configuration is supported as a first-class entry-point mode.
Push-button reset and boot entry configuration can be used together.
PushButtonAction accepts friendly string values and converts them to internal action values.
BootEntryVisibility supports Visible and Hidden.
Hidden boot entries are used only when safely supported.
SizePercent implies percentage sizing.
EnablePercentSizing does not exist.
SizeBytes and SizePercent are mutually exclusive.
Help is generated with PlatyPS.
README includes a generated cmdlet documentation index.
README links to every cmdlet help file.
Each cmdlet has one single-line example.
Each major cmdlet has one expanded OrderedDictionary-style splatted example.
PowerShell documentation examples use New-Object.
PowerShell documentation examples do not use ::new().
Expanded examples preserve the requested indentation style.
```

---

## Partition Layout Anticipation

The module inspects on-disk partition geometry around a recovery partition (existing or proposed) before any destructive or growth-style operation and surfaces a `RecoveryPartitionLayoutAnalysis` object so callers can see whether the change is safe in place.

The analyzer reports:

```text
DiskNumber
PartitionNumber
Position (Unknown | Standalone | BeforeOs | AfterOs | SameAsOs)
OsPartitionNumber / OsPartitionOffset / OsPartitionSizeBytes
PrecedingPartitionNumber / PrecedingPartitionType / PrecedingPartitionIsOs
FollowingPartitionNumber / FollowingPartitionType / FollowingPartitionIsOs
LeadingFreeSpaceBytes
TrailingFreeSpaceBytes
CanGrowInPlace
CanShrinkInPlace
CanRemoveSafely
Warnings
```

The OS partition is identified by a drive letter that matches `$env:SystemDrive` once the volume → partition mapping has been resolved by `Win32VolumeMapper` against the disk extents reported through `IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS`.

Behaviour rules:

- `New-RecoveryPartitionPlan` attaches the analysis to the plan and emits each warning via `Write-Warning`. A `ResizePartition` step is converted to a `Skip` step when `CanGrowInPlace` is false.
- `Invoke-RecoveryPartitionPlan` re-emits the warnings before executing the plan.
- `Resize-RecoveryPartition` throws when `CanGrowInPlace` is false unless `-Force` is supplied.
- `Remove-RecoveryPartition` throws when `CanRemoveSafely` is false (immediately followed by the OS partition) unless `-Force` is supplied.

Warning catalogue (non-exhaustive):

```text
Recovery partition is immediately followed by the OS partition; growing or removing it cannot be done in place without first relocating the OS.
Requested size (X bytes) exceeds the available trailing free space (Y bytes); the resize would fail without shrinking a neighbour first.
Recovery partition is located before the OS partition; any size change will shift the OS partition offset and is not supported in place.
```

