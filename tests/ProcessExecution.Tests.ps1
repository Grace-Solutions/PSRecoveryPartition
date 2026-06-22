#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll {
    $script:RepoRoot = Split-Path -Parent $PSScriptRoot
    $script:Manifest = Join-Path $script:RepoRoot 'Module/PSRecoveryPartition/PSRecoveryPartition.psd1'
    Import-Module $script:Manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module -Name PSRecoveryPartition -Force -ErrorAction SilentlyContinue
}

Describe 'Internal process execution helper' {
    It 'is not exported as a public cmdlet' {
        Get-Command -Module PSRecoveryPartition -Name 'Start-ProcessWithOutput' -ErrorAction SilentlyContinue |
            Should -BeNullOrEmpty
    }

    It 'has an internal ProcessExecution type loaded from the module assembly' {
        $asm = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1
        $asm | Should -Not -BeNullOrEmpty

        $type = $asm.GetType('PSRecoveryPartition.ProcessExecution', $false)
        $type | Should -Not -BeNullOrEmpty
        $type.IsNotPublic | Should -BeTrue
    }

    It 'exposes the rich RecoveryProcessExecutionRequest contract' {
        $asm = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1
        $type = $asm.GetType('PSRecoveryPartition.RecoveryProcessExecutionRequest', $false)
        $type | Should -Not -BeNullOrEmpty
        foreach ($name in 'FilePath','ArgumentList','AcceptableExitCodeList','LogOutput','ExecutionTimeout','SecureArgumentList') {
            $type.GetProperty($name) | Should -Not -BeNullOrEmpty -Because "property '$name' must exist on RecoveryProcessExecutionRequest"
        }
    }
}

Describe 'Result base contract' {
    It 'discloses ExecutionMethod and ProcessFallbackUsed on result objects' {
        $asm = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1
        $base = $asm.GetType('PSRecoveryPartition.RecoveryResultBase', $false)
        $base | Should -Not -BeNullOrEmpty
        $base.GetProperty('ExecutionMethod')     | Should -Not -BeNullOrEmpty
        $base.GetProperty('ProcessFallbackUsed') | Should -Not -BeNullOrEmpty
        $base.GetProperty('ProcessResults')      | Should -Not -BeNullOrEmpty
    }
}
