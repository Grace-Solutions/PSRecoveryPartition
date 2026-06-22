#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll {
    $script:RepoRoot   = Split-Path -Parent $PSScriptRoot
    $script:Manifest   = Join-Path $script:RepoRoot 'Module/PSRecoveryPartition/PSRecoveryPartition.psd1'
    $script:DocsDir    = Join-Path $script:RepoRoot 'docs'
    if (-not (Test-Path $script:Manifest)) {
        throw "Module manifest not found at '$script:Manifest'. Run .\build.ps1 first."
    }
    Import-Module $script:Manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module -Name PSRecoveryPartition -Force -ErrorAction SilentlyContinue
}

Describe 'PSRecoveryPartition module' {
    It 'has a valid module manifest' {
        $info = Test-ModuleManifest -Path $script:Manifest
        $info.Name | Should -Be 'PSRecoveryPartition'
    }

    It 'exports the expected cmdlets' {
        $expected = @(
            'Get-RecoveryPartition','New-RecoveryPartition','Set-RecoveryPartition','Resize-RecoveryPartition',
            'Remove-RecoveryPartition','Mount-RecoveryPartition','Dismount-RecoveryPartition','Test-RecoveryPartition',
            'Get-RecoveryPartitionPlan','Invoke-RecoveryPartitionPlan',
            'Get-WindowsRecoveryImage','Set-WindowsRecoveryImage','Save-RecoveryBootImage',
            'Get-WindowsRecoveryEnvironment','Set-WindowsRecoveryEnvironment',
            'Enable-WindowsRecoveryEnvironment','Disable-WindowsRecoveryEnvironment',
            'Get-WindowsRecoveryBootEntry','New-WindowsRecoveryBootEntry','Remove-WindowsRecoveryBootEntry',
            'Set-WindowsRecoveryEntryPoint'
        )
        $actual = (Get-Command -Module PSRecoveryPartition).Name | Sort-Object
        $missing = $expected | Where-Object { $actual -notcontains $_ }
        $missing | Should -BeNullOrEmpty
    }

    It 'has a help file for every exported cmdlet' {
        foreach ($cmd in (Get-Command -Module PSRecoveryPartition)) {
            $path = Join-Path $script:DocsDir ($cmd.Name + '.md')
            Test-Path -LiteralPath $path | Should -BeTrue -Because "expected docs file: $path"
        }
    }

    It 'does not expose -AllowShellOut on any public cmdlet' {
        foreach ($cmd in (Get-Command -Module PSRecoveryPartition)) {
            $cmd.Parameters.Keys | Should -Not -Contain 'AllowShellOut'
        }
    }
}

Describe 'New-RecoveryPartition parameter sets' {
    It 'exposes ExplicitSize, PercentSize, and DefaultSize parameter sets' {
        $sets = (Get-Command New-RecoveryPartition).ParameterSets.Name | Sort-Object
        $sets | Should -Contain 'ExplicitSize'
        $sets | Should -Contain 'PercentSize'
        $sets | Should -Contain 'DefaultSize'
    }

    It 'fails when -SizeBytes and -SizePercent are supplied together' {
        { New-RecoveryPartition -DiskNumber 0 -SizeBytes 1GB -SizePercent 2 -WhatIf -ErrorAction Stop } |
            Should -Throw
    }

    It 'does not expose -EnablePercentSizing' {
        (Get-Command New-RecoveryPartition).Parameters.Keys | Should -Not -Contain 'EnablePercentSizing'
    }
}

Describe 'Resize-RecoveryPartition parameter sets' {
    It 'exposes ExplicitSize and PercentSize parameter sets' {
        $sets = (Get-Command Resize-RecoveryPartition).ParameterSets.Name | Sort-Object
        $sets | Should -Contain 'ExplicitSize'
        $sets | Should -Contain 'PercentSize'
    }
}

Describe 'PushButtonAction normalization' {
    It 'rejects an unsupported push-button action value' {
        { Set-WindowsRecoveryEntryPoint -EntryPointMode PushButtonReset -PushButtonAction 'NotARealAction' -Force -ErrorAction Stop -WhatIf } |
            Should -Throw
    }
}

Describe 'BootEntryVisibility enum' {
    It 'accepts Visible and Hidden' {
        $param = (Get-Command New-WindowsRecoveryBootEntry).Parameters['BootEntryVisibility']
        $param | Should -Not -BeNullOrEmpty
        [enum]::GetNames($param.ParameterType) | Should -Contain 'Visible'
        [enum]::GetNames($param.ParameterType) | Should -Contain 'Hidden'
    }
}

Describe 'DestinationPath parameter' {
    It 'Set-WindowsRecoveryImage exposes -DestinationPath instead of -DestinationDirectory' {
        $params = (Get-Command Set-WindowsRecoveryImage).Parameters.Keys
        $params | Should -Contain 'DestinationPath'
        $params | Should -Not -Contain 'DestinationDirectory'
        $params | Should -Not -Contain 'DestinationFileName'
    }

    It 'Save-RecoveryBootImage exposes -DestinationPath instead of -DestinationDirectory' {
        $params = (Get-Command Save-RecoveryBootImage).Parameters.Keys
        $params | Should -Contain 'DestinationPath'
        $params | Should -Not -Contain 'DestinationDirectory'
        $params | Should -Not -Contain 'DestinationFileName'
    }
}
