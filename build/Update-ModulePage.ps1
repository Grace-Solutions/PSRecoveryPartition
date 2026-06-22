[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$docsDir = Join-Path $repoRoot 'docs'
$modulePath = Join-Path $docsDir 'PSRecoveryPartition.md'
$helpContent = & (Join-Path $PSScriptRoot 'HelpContent.ps1')
$manifestPath = Join-Path $repoRoot 'Module/PSRecoveryPartition/PSRecoveryPartition.psd1'
$manifest = Import-PowerShellDataFile -LiteralPath $manifestPath

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('---')
$lines.Add('Module Name: PSRecoveryPartition')
$lines.Add("Module Guid: $($manifest.GUID)")
$lines.Add('Download Help Link: https://github.com/GraceSolutions/PSRecoveryPartition')
$lines.Add("Help Version: $($manifest.ModuleVersion)")
$lines.Add('Locale: en-US')
$lines.Add('---')
$lines.Add('')
$lines.Add('# PSRecoveryPartition Module')
$lines.Add('## Description')
$lines.Add($manifest.Description)
$lines.Add('')
$lines.Add('## PSRecoveryPartition Cmdlets')
foreach ($name in ($helpContent.Keys | Sort-Object)) {
    if ($name -eq '__Common') { continue }
    $entry = $helpContent[$name]
    if ($entry -isnot [hashtable] -or -not $entry.ContainsKey('Synopsis')) { continue }
    $lines.Add("### [$name]($name.md)")
    $lines.Add($entry.Synopsis)
    $lines.Add('')
}
Set-Content -LiteralPath $modulePath -Value ($lines -join "`r`n") -Encoding utf8
Write-Host "  updated: PSRecoveryPartition.md"
