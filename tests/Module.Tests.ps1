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
            'New-RecoveryPartitionPlan','Invoke-RecoveryPartitionPlan',
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

Describe 'Recovery partition defaults and discovery' {
    It 'defaults the recovery partition label to RECOVERY' {
        $asm = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1
        $type = $asm.GetType('PSRecoveryPartition.RecoveryPartitionConstants', $false)
        $type | Should -Not -BeNullOrEmpty
        $field = $type.GetField('DefaultLabel', [Reflection.BindingFlags]'Public,Static,NonPublic')
        $field | Should -Not -BeNullOrEmpty
        $field.GetValue($null) | Should -BeExactly 'RECOVERY'
    }

    It 'matches MBR type 0x27 as a recovery partition' {
        $asm = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1
        $type = $asm.GetType('PSRecoveryPartition.PartitionMapper', $false)
        $type | Should -Not -BeNullOrEmpty
        $method = $type.GetMethod('IsRecoveryMbrType', [Reflection.BindingFlags]'Public,Static', $null, @([int64]), $null)
        $method | Should -Not -BeNullOrEmpty
        $method.Invoke($null, @([int64]0x27)) | Should -BeTrue
        $method.Invoke($null, @([int64]0x07)) | Should -BeFalse
    }

    It 'matches the recovery GPT type GUID case-insensitively' {
        $asm = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1
        $type = $asm.GetType('PSRecoveryPartition.PartitionMapper', $false)
        $method = $type.GetMethod('IsRecoveryGptType')
        $method.Invoke($null, @('{DE94BBA4-06D1-4D40-A16A-BFD50179D6AC}')) | Should -BeTrue
        $method.Invoke($null, @('de94bba4-06d1-4d40-a16a-bfd50179d6ac')) | Should -BeTrue
        $method.Invoke($null, @('{00000000-0000-0000-0000-000000000000}')) | Should -BeFalse
    }
}

Describe 'New-RecoveryPartitionPlan surface' {
    It 'replaces Get-RecoveryPartitionPlan with New-RecoveryPartitionPlan' {
        Get-Command -Module PSRecoveryPartition -Name 'Get-RecoveryPartitionPlan' -ErrorAction SilentlyContinue |
            Should -BeNullOrEmpty
        Get-Command -Module PSRecoveryPartition -Name 'New-RecoveryPartitionPlan' -ErrorAction Stop |
            Should -Not -BeNullOrEmpty
    }

    It 'exposes the expanded plan parameters' {
        $params = (Get-Command New-RecoveryPartitionPlan).Parameters.Keys
        $params | Should -Contain 'BootImagePath'
        $params | Should -Contain 'WindowsREImagePath'
        $params | Should -Contain 'EntryPointMode'
        $params | Should -Contain 'BootEntryName'
        $params | Should -Contain 'PushButtonAction'
    }

    It 'surfaces the new plan-step action constants on the assembly' {
        $asm = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1
        $type = $asm.GetType('PSRecoveryPartition.RecoveryPartitionPlanActions', $false)
        $type | Should -Not -BeNullOrEmpty
        foreach ($name in @('CreatePartition','ResizePartition','CopyWinREImage','CopyBootImage','RegisterWinRE','EnableWinRE','CreateBootEntry','ConfigurePushButton','Skip')) {
            $type.GetField($name) | Should -Not -BeNullOrEmpty -Because "expected plan action constant '$name'"
        }
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
