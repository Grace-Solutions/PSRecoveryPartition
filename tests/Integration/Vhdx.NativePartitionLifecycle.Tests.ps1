#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Opt-in integration test that drives the native Win32 IOCTL engine through
# a scratch VHDX. Skipped unless all of the following hold:
#   * $env:PSRECOVERY_RUN_INTEGRATION is set to a truthy value.
#   * The session is elevated (admin).
#   * The Hyper-V PowerShell module is available (New-VHD / Mount-VHD).
#   * The in-box Storage module is available for test setup (Initialize-Disk).
# The module under test does not depend on the Storage module at runtime;
# the test rig uses it only to initialize the scratch disk's partition table.

BeforeDiscovery {
    $script:OptIn = $env:PSRECOVERY_RUN_INTEGRATION -and
        ($env:PSRECOVERY_RUN_INTEGRATION -ne '0') -and
        ($env:PSRECOVERY_RUN_INTEGRATION -ne 'false')

    $id  = [Security.Principal.WindowsIdentity]::GetCurrent()
    $isAdmin = ([Security.Principal.WindowsPrincipal]$id).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)

    $hasHyperV  = $null -ne (Get-Module -ListAvailable -Name Hyper-V)
    $hasStorage = $null -ne (Get-Module -ListAvailable -Name Storage)

    $script:SkipReason = if (-not $script:OptIn) {
        'PSRECOVERY_RUN_INTEGRATION is not set; skipping VHDX integration test.'
    } elseif (-not $isAdmin) {
        'Session is not elevated; skipping VHDX integration test.'
    } elseif (-not $hasHyperV) {
        'Hyper-V PowerShell module is not available; skipping VHDX integration test.'
    } elseif (-not $hasStorage) {
        'Storage PowerShell module is not available for test setup; skipping VHDX integration test.'
    } else { $null }

    $script:Skip = $null -ne $script:SkipReason
}

BeforeAll {
    if ($script:Skip) { Write-Host "  [skip] $script:SkipReason"; return }

    $script:RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $script:Manifest = Join-Path $script:RepoRoot 'Module/PSRecoveryPartition/PSRecoveryPartition.psd1'
    Import-Module $script:Manifest -Force -ErrorAction Stop

    $script:VhdPath = Join-Path ([IO.Path]::GetTempPath()) ("PSRecoveryPartition-" + [Guid]::NewGuid().ToString('N') + '.vhdx')
    $script:VhdSize = 256MB

    $vhd  = New-VHD -Path $script:VhdPath -SizeBytes $script:VhdSize -Dynamic -ErrorAction Stop
    $disk = Mount-VHD -Path $script:VhdPath -PassThru -ErrorAction Stop |
        Get-Disk -ErrorAction Stop
    $script:DiskNumber = $disk.Number
    Initialize-Disk -Number $script:DiskNumber -PartitionStyle GPT -Confirm:$false -ErrorAction Stop | Out-Null
}

AfterAll {
    if ($script:Skip) { return }
    try { Dismount-VHD -Path $script:VhdPath -ErrorAction SilentlyContinue } catch { }
    try { Remove-Item -LiteralPath $script:VhdPath -Force -ErrorAction SilentlyContinue } catch { }
    Remove-Module -Name PSRecoveryPartition -Force -ErrorAction SilentlyContinue
}

Describe 'Native partition lifecycle on a scratch VHDX' -Skip:$script:Skip {
    It 'creates a recovery partition through the native engine' {
        $info = New-RecoveryPartition -DiskNumber $script:DiskNumber -SizeBytes 64MB -Force -PassThru -Confirm:$false
        $info                          | Should -Not -BeNullOrEmpty
        $info.DiskNumber               | Should -Be $script:DiskNumber
        $info.PartitionNumber          | Should -BeGreaterThan 0
        $info.SizeBytes                | Should -BeGreaterThan 0
        $info.ExecutionMethod.ToString() | Should -Be 'Native'
        $info.IsRecoveryPartition      | Should -BeTrue
        $script:PartitionNumber = $info.PartitionNumber
    }

    It 'discovers the new partition via Get-RecoveryPartition' {
        $found = Get-RecoveryPartition -DiskNumber $script:DiskNumber
        $found                                 | Should -Not -BeNullOrEmpty
        ($found | Where-Object { $_.PartitionNumber -eq $script:PartitionNumber }) |
            Should -Not -BeNullOrEmpty
    }

    It 'grows the partition through IOCTL_DISK_GROW_PARTITION + FSCTL_EXTEND_VOLUME' {
        $info = Resize-RecoveryPartition -DiskNumber $script:DiskNumber `
            -PartitionNumber $script:PartitionNumber -SizeBytes 128MB -Force -PassThru -Confirm:$false
        $info.SizeBytes                | Should -BeGreaterOrEqual 120MB
        $info.ExecutionMethod.ToString() | Should -Be 'Native'
    }

    It 'shrinks the partition through FSCTL_SHRINK_VOLUME prepare/commit' {
        $info = Resize-RecoveryPartition -DiskNumber $script:DiskNumber `
            -PartitionNumber $script:PartitionNumber -SizeBytes 96MB -Force -PassThru -Confirm:$false
        $info.SizeBytes                | Should -BeLessOrEqual 120MB
        $info.ExecutionMethod.ToString() | Should -Be 'Native'
    }

    It 'removes the partition through IOCTL_DISK_SET_DRIVE_LAYOUT_EX' {
        Remove-RecoveryPartition -DiskNumber $script:DiskNumber `
            -PartitionNumber $script:PartitionNumber -Force -Confirm:$false
        $remaining = Get-RecoveryPartition -DiskNumber $script:DiskNumber -ErrorAction SilentlyContinue
        ($remaining | Where-Object { $_.PartitionNumber -eq $script:PartitionNumber }) |
            Should -BeNullOrEmpty
    }
}
