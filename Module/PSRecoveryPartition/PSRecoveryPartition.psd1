@{
    RootModule           = 'PSRecoveryPartition.dll'
    FormatsToProcess     = @('PSRecoveryPartition.format.ps1xml')
    ModuleVersion        = '2026.6.22.1713'
    GUID                 = 'd6d6f0b8-2a4b-4a8d-9d2a-7c0f4c2b5e9a'
    Author               = 'Grace Solutions'
    CompanyName          = 'Grace Solutions'
    Copyright            = 'Copyright (c) 2026 Grace Solutions. Licensed under GNU AGPL v3.'
    Description          = 'PowerShell module for managing Windows recovery partitions, Windows Recovery Environment, and recovery boot entries. Uses native Win32 IOCTLs and P/Invokes (no dependency on the in-box Storage module / MSFT_* CIM classes); controlled Microsoft inbox process fallback (reagentc, bcdedit, diskpart for the MBR 0x27 type byte) is used only where required.'
    PowerShellVersion    = '5.1'
    CompatiblePSEditions = @('Desktop', 'Core')
    DotNetFrameworkVersion = '4.7.2'
    ProcessorArchitecture = 'None'

    CmdletsToExport = @(
        'Get-RecoveryPartition'
        'New-RecoveryPartition'
        'Set-RecoveryPartition'
        'Resize-RecoveryPartition'
        'Remove-RecoveryPartition'
        'Mount-RecoveryPartition'
        'Dismount-RecoveryPartition'
        'Test-RecoveryPartition'
        'New-RecoveryPartitionPlan'
        'Invoke-RecoveryPartitionPlan'
        'Get-WindowsRecoveryImage'
        'Set-WindowsRecoveryImage'
        'Get-WindowsRecoveryEnvironment'
        'Set-WindowsRecoveryEnvironment'
        'Enable-WindowsRecoveryEnvironment'
        'Disable-WindowsRecoveryEnvironment'
        'Get-WindowsRecoveryBootEntry'
        'New-WindowsRecoveryBootEntry'
        'Remove-WindowsRecoveryBootEntry'
        'Set-WindowsRecoveryEntryPoint'
        'Save-RecoveryBootImage'
    )
    FunctionsToExport = @()
    AliasesToExport   = @()
    VariablesToExport = @()

    PrivateData = @{
        PSData = @{
            Tags         = @('Windows', 'Recovery', 'Partition', 'WinRE', 'BCD', 'PushButtonReset', 'BootEntry')
            LicenseUri   = 'https://www.gnu.org/licenses/agpl-3.0.html'
            ProjectUri   = 'https://github.com/GraceSolutions/PSRecoveryPartition'
            ReleaseNotes = 'Initial release.'
        }
    }
}
