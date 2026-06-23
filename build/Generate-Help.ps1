[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Import-Module (Join-Path $repoRoot 'Module/PSRecoveryPartition/PSRecoveryPartition.psd1') -Force -ErrorAction Stop
Import-Module platyPS -ErrorAction Stop
$outDir = Join-Path $repoRoot 'docs'
New-MarkdownHelp -Module PSRecoveryPartition -OutputFolder $outDir -WithModulePage -Force -ErrorAction Stop |
    Select-Object -ExpandProperty Name
