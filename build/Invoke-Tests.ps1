[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Import-Module Pester -ErrorAction Stop
$cfg = New-PesterConfiguration
$cfg.Run.Path = (Join-Path $repoRoot 'tests')
$cfg.Run.Exit = $false
$cfg.Output.Verbosity = 'Detailed'
Invoke-Pester -Configuration $cfg
