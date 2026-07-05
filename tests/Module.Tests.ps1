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

Describe 'Get-RecoveryPartition -DetectionMode' {
    It 'exposes -DetectionMode typed as RecoveryPartitionDetectionMode' {
        $param = (Get-Command Get-RecoveryPartition).Parameters['DetectionMode']
        $param | Should -Not -BeNullOrEmpty
        $param.ParameterType.FullName | Should -Be 'PSRecoveryPartition.RecoveryPartitionDetectionMode'
    }

    It 'defines CurrentOSDisk, AllDisks and SecondaryDisksOnly with CurrentOSDisk as the default (0)' {
        $t = (Get-Command Get-RecoveryPartition).Parameters['DetectionMode'].ParameterType
        foreach ($name in @('CurrentOSDisk','AllDisks','SecondaryDisksOnly')) {
            [enum]::GetNames($t) | Should -Contain $name
        }
        [int][enum]::Parse($t, 'CurrentOSDisk') | Should -Be 0
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

Describe 'Plan cmdlets removed' {
    It 'no longer exports New-RecoveryPartitionPlan or Invoke-RecoveryPartitionPlan' {
        Get-Command -Module PSRecoveryPartition -Name 'New-RecoveryPartitionPlan' -ErrorAction SilentlyContinue |
            Should -BeNullOrEmpty
        Get-Command -Module PSRecoveryPartition -Name 'Invoke-RecoveryPartitionPlan' -ErrorAction SilentlyContinue |
            Should -BeNullOrEmpty
    }
}

Describe 'Recovery partition layout analysis' {
    BeforeAll {
        $script:RecoveryAssembly = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1
    }

    It 'exposes the RecoveryPartitionLayoutAnalysis result type' {
        $type = $RecoveryAssembly.GetType('PSRecoveryPartition.RecoveryPartitionLayoutAnalysis', $false)
        $type | Should -Not -BeNullOrEmpty
        foreach ($name in @('DiskNumber','PartitionNumber','Position','OsPartitionNumber','PrecedingPartitionNumber','FollowingPartitionNumber','LeadingFreeSpaceBytes','TrailingFreeSpaceBytes','CanGrowInPlace','CanShrinkInPlace','CanRemoveSafely','Warnings')) {
            $type.GetProperty($name) | Should -Not -BeNullOrEmpty -Because "expected property '$name'"
        }
    }

    It 'exposes the RecoveryPartitionLayoutPosition enum values' {
        $type = $RecoveryAssembly.GetType('PSRecoveryPartition.RecoveryPartitionLayoutPosition', $false)
        $type | Should -Not -BeNullOrEmpty
        foreach ($value in @('Unknown','Standalone','BeforeOs','AfterOs','SameAsOs')) {
            [System.Enum]::IsDefined($type, $value) | Should -BeTrue -Because "expected enum value '$value'"
        }
    }

    It 'exposes -Force on Resize-RecoveryPartition for risky operations' {
        $params = (Get-Command Resize-RecoveryPartition).Parameters.Keys
        $params | Should -Contain 'Force'
    }
}

Describe 'Native IOCTL / FSCTL constants' {
    BeforeAll {
        $script:NativeAssembly = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1
        $script:NativeConstants = $script:NativeAssembly.GetType('PSRecoveryPartition.Native.NativeConstants', $false)

        function script:Get-CtlCode {
            param(
                [Parameter(Mandatory)][uint32]$DeviceType,
                [Parameter(Mandatory)][uint32]$Access,
                [Parameter(Mandatory)][uint32]$Function,
                [Parameter(Mandatory)][uint32]$Method
            )
            return [uint32](($DeviceType -shl 16) -bor ($Access -shl 14) -bor ($Function -shl 2) -bor $Method)
        }

        function script:Get-ConstValue {
            param([Parameter(Mandatory)][string]$Name)
            $field = $script:NativeConstants.GetField($Name, [Reflection.BindingFlags]'Public,Static,NonPublic')
            if (-not $field) { throw "NativeConstants.$Name not found" }
            return [uint32]$field.GetValue($null)
        }
    }

    It 'exposes the internal NativeConstants type' {
        $script:NativeConstants | Should -Not -BeNullOrEmpty
    }

    # FILE_DEVICE_DISK = 0x07, FILE_READ_ACCESS = 1, FILE_WRITE_ACCESS = 2, METHOD_BUFFERED = 0
    It 'encodes IOCTL_DISK_GET_DRIVE_GEOMETRY correctly' {
        Get-ConstValue 'IOCTL_DISK_GET_DRIVE_GEOMETRY' | Should -Be (Get-CtlCode 0x07 0 0x000 0)
    }
    It 'encodes IOCTL_DISK_GET_DRIVE_GEOMETRY_EX correctly' {
        Get-ConstValue 'IOCTL_DISK_GET_DRIVE_GEOMETRY_EX' | Should -Be (Get-CtlCode 0x07 0 0x028 0)
    }
    It 'encodes IOCTL_DISK_GET_LENGTH_INFO correctly' {
        Get-ConstValue 'IOCTL_DISK_GET_LENGTH_INFO' | Should -Be (Get-CtlCode 0x07 1 0x017 0)
    }
    It 'encodes IOCTL_DISK_GET_PARTITION_INFO_EX correctly' {
        Get-ConstValue 'IOCTL_DISK_GET_PARTITION_INFO_EX' | Should -Be (Get-CtlCode 0x07 0 0x012 0)
    }
    It 'encodes IOCTL_DISK_SET_PARTITION_INFO_EX correctly' {
        Get-ConstValue 'IOCTL_DISK_SET_PARTITION_INFO_EX' | Should -Be (Get-CtlCode 0x07 3 0x013 0)
    }
    It 'encodes IOCTL_DISK_GET_DRIVE_LAYOUT_EX correctly' {
        Get-ConstValue 'IOCTL_DISK_GET_DRIVE_LAYOUT_EX' | Should -Be (Get-CtlCode 0x07 0 0x014 0)
    }
    It 'encodes IOCTL_DISK_SET_DRIVE_LAYOUT_EX correctly' {
        Get-ConstValue 'IOCTL_DISK_SET_DRIVE_LAYOUT_EX' | Should -Be (Get-CtlCode 0x07 3 0x015 0)
    }
    It 'encodes IOCTL_DISK_UPDATE_PROPERTIES correctly' {
        Get-ConstValue 'IOCTL_DISK_UPDATE_PROPERTIES' | Should -Be (Get-CtlCode 0x07 0 0x050 0)
    }
    It 'encodes IOCTL_DISK_GROW_PARTITION correctly' {
        Get-ConstValue 'IOCTL_DISK_GROW_PARTITION' | Should -Be (Get-CtlCode 0x07 3 0x034 0)
    }

    # FILE_DEVICE_MASS_STORAGE / VOLUME = 0x56
    It 'encodes IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS correctly' {
        Get-ConstValue 'IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS' | Should -Be (Get-CtlCode 0x56 0 0x000 0)
    }

    # FILE_DEVICE_FILE_SYSTEM = 0x09 - the constants that caused the resize regression
    It 'encodes FSCTL_EXTEND_VOLUME correctly (regression: was 0x0009C334)' {
        Get-ConstValue 'FSCTL_EXTEND_VOLUME' | Should -Be (Get-CtlCode 0x09 0 60  0)
        Get-ConstValue 'FSCTL_EXTEND_VOLUME' | Should -Be ([uint32]0x000900F0)
    }
    It 'encodes FSCTL_SHRINK_VOLUME correctly (regression: was 0x0009C340)' {
        Get-ConstValue 'FSCTL_SHRINK_VOLUME' | Should -Be (Get-CtlCode 0x09 0 108 0)
        Get-ConstValue 'FSCTL_SHRINK_VOLUME' | Should -Be ([uint32]0x000901B0)
    }
    It 'encodes FSCTL_LOCK_VOLUME correctly' {
        Get-ConstValue 'FSCTL_LOCK_VOLUME' | Should -Be (Get-CtlCode 0x09 0 6 0)
    }
    It 'encodes FSCTL_UNLOCK_VOLUME correctly' {
        Get-ConstValue 'FSCTL_UNLOCK_VOLUME' | Should -Be (Get-CtlCode 0x09 0 7 0)
    }
    It 'encodes FSCTL_DISMOUNT_VOLUME correctly' {
        Get-ConstValue 'FSCTL_DISMOUNT_VOLUME' | Should -Be (Get-CtlCode 0x09 0 8 0)
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

Describe 'RecoveryPartitionInfo path projections' {
    BeforeAll {
        $script:InfoAssembly = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1
    }

    It 'exposes VolumePath, DevicePath, and GlobalRootPath on RecoveryPartitionInfo' {
        $type = $InfoAssembly.GetType('PSRecoveryPartition.RecoveryPartitionInfo', $false)
        $type | Should -Not -BeNullOrEmpty
        foreach ($name in @('VolumePath','DevicePath','GlobalRootPath')) {
            $type.GetProperty($name) | Should -Not -BeNullOrEmpty -Because "expected property '$name'"
        }
    }

    It 'exposes Source, DiskNumber, PartitionNumber, and VolumePath on WindowsRecoveryImageInfo' {
        $type = $InfoAssembly.GetType('PSRecoveryPartition.WindowsRecoveryImageInfo', $false)
        $type | Should -Not -BeNullOrEmpty
        foreach ($name in @('Source','DiskNumber','PartitionNumber','VolumePath')) {
            $type.GetProperty($name) | Should -Not -BeNullOrEmpty -Because "expected property '$name'"
        }
    }
}

Describe 'Image cmdlet HTTP and attribute surface' {
    It 'Save-RecoveryBootImage exposes ByUri and ByPath parameter sets' {
        $sets = (Get-Command Save-RecoveryBootImage).ParameterSets.Name | Sort-Object
        $sets | Should -Contain 'ByUri'
        $sets | Should -Contain 'ByPath'
    }

    It 'Save-RecoveryBootImage exposes -SourceUri, -Headers, -Hidden, -System' {
        $params = (Get-Command Save-RecoveryBootImage).Parameters
        $params.Keys | Should -Contain 'SourceUri'
        $params.Keys | Should -Contain 'Headers'
        $params.Keys | Should -Contain 'Hidden'
        $params.Keys | Should -Contain 'System'
        $params['Headers'].ParameterType.FullName | Should -Be 'System.Collections.IDictionary'
        $params['SourceUri'].ParameterType.FullName | Should -Be 'System.Uri'
    }

    It 'Set-WindowsRecoveryImage exposes -SourceUri, -Headers, -Hidden, -System' {
        $params = (Get-Command Set-WindowsRecoveryImage).Parameters
        $params.Keys | Should -Contain 'SourceUri'
        $params.Keys | Should -Contain 'Headers'
        $params.Keys | Should -Contain 'Hidden'
        $params.Keys | Should -Contain 'System'
        $params['Headers'].ParameterType.FullName | Should -Be 'System.Collections.IDictionary'
    }
}

Describe 'Boot entry / entry point -RecoveryPartition parameter' {
    It 'New-WindowsRecoveryBootEntry exposes a ByRecoveryPartition parameter set' {
        $sets = (Get-Command New-WindowsRecoveryBootEntry).ParameterSets.Name | Sort-Object
        $sets | Should -Contain 'ByRecoveryPartition'
        $sets | Should -Contain 'ByImagePath'
    }

    It 'New-WindowsRecoveryBootEntry exposes -RecoveryPartition typed as RecoveryPartitionInfo' {
        $param = (Get-Command New-WindowsRecoveryBootEntry).Parameters['RecoveryPartition']
        $param | Should -Not -BeNullOrEmpty
        $param.ParameterType.FullName | Should -Be 'PSRecoveryPartition.RecoveryPartitionInfo'
    }

    It 'New-WindowsRecoveryBootEntry exposes ByTargetPath and defaults to ByImagePath' {
        $cmd = Get-Command New-WindowsRecoveryBootEntry
        $cmd.ParameterSets.Name | Should -Contain 'ByTargetPath'
        ($cmd.ParameterSets | Where-Object IsDefault).Name | Should -Be 'ByImagePath'
    }

    It 'New-WindowsRecoveryBootEntry exposes -ExpandBootImage and -ImageIndex' {
        $params = (Get-Command New-WindowsRecoveryBootEntry).Parameters.Keys
        $params | Should -Contain 'ExpandBootImage'
        $params | Should -Contain 'ImageIndex'
        $params | Should -Contain 'BootSdiPath'
        $params | Should -Contain 'StagingRelativePath'
    }

    It 'Set-WindowsRecoveryEntryPoint no longer creates boot entries (-RecoveryPartition removed)' {
        # Boot-entry creation was consolidated into New-WindowsRecoveryBootEntry.
        (Get-Command Set-WindowsRecoveryEntryPoint).Parameters.ContainsKey('RecoveryPartition') |
            Should -BeFalse
    }
}

Describe 'Get-WindowsRecoveryImage -Source filter' {
    It 'exposes -Source with the documented ValidateSet values' {
        $param = (Get-Command Get-WindowsRecoveryImage).Parameters['Source']
        $param | Should -Not -BeNullOrEmpty
        $validate = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } | Select-Object -First 1
        $validate | Should -Not -BeNullOrEmpty
        foreach ($value in @('UserPath','SystemRoot','Reagent','RecoveryPartition')) {
            $validate.ValidValues | Should -Contain $value
        }
    }
}

Describe 'DestinationPathResolver UNC rejection' {
    BeforeAll {
        $script:ResolverType = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1 |
            ForEach-Object { $_.GetType('PSRecoveryPartition.DestinationPathResolver', $false) }
        $script:RejectUnc = $ResolverType.GetMethod('RejectUncShare',
            [Reflection.BindingFlags]'NonPublic,Static')
    }

    It 'rejects \\server\share style destination paths' {
        { $script:RejectUnc.Invoke($null, @([string]'\\fileserver\share\boot.wim')) } | Should -Throw
    }

    It 'rejects the \\?\UNC\server\share form' {
        { $script:RejectUnc.Invoke($null, @([string]'\\?\UNC\fileserver\share\boot.wim')) } | Should -Throw
    }

    It 'allows \\?\Volume{guid}\ destination paths' {
        { $script:RejectUnc.Invoke($null, @([string]'\\?\Volume{00000000-0000-0000-0000-000000000000}\Recovery\WindowsRE\Winre.wim')) } |
            Should -Not -Throw
    }

    It 'allows \\?\GLOBALROOT\Device\HarddiskN\PartitionM paths' {
        { $script:RejectUnc.Invoke($null, @([string]'\\?\GLOBALROOT\Device\Harddisk0\Partition4\Recovery\WindowsRE\Winre.wim')) } |
            Should -Not -Throw
    }

    It 'allows local drive-letter destination paths' {
        { $script:RejectUnc.Invoke($null, @([string]'C:\Recovery\WindowsRE\Winre.wim')) } |
            Should -Not -Throw
    }
}

Describe 'Native attribute constants' {
    It 'defines FILE_ATTRIBUTE_HIDDEN = 0x2 and FILE_ATTRIBUTE_SYSTEM = 0x4' {
        $type = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'PSRecoveryPartition' } |
            Select-Object -First 1 |
            ForEach-Object { $_.GetType('PSRecoveryPartition.Native.NativeConstants', $false) }
        $hidden = $type.GetField('FILE_ATTRIBUTE_HIDDEN', [Reflection.BindingFlags]'Public,Static,NonPublic')
        $system = $type.GetField('FILE_ATTRIBUTE_SYSTEM', [Reflection.BindingFlags]'Public,Static,NonPublic')
        $hidden | Should -Not -BeNullOrEmpty
        $system | Should -Not -BeNullOrEmpty
        [uint32]$hidden.GetValue($null) | Should -Be ([uint32]0x2)
        [uint32]$system.GetValue($null) | Should -Be ([uint32]0x4)
    }
}

