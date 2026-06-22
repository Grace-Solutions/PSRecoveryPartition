[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$docsDir = Join-Path $repoRoot 'docs'
$modulePath = Join-Path $docsDir 'PSRecoveryPartition.md'
$helpContent = & (Join-Path $PSScriptRoot 'HelpContent.ps1')

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('---')
$lines.Add('Module Name: PSRecoveryPartition')
$lines.Add('Module Guid: d6d6f0b8-2a4b-4a8d-9d2a-7c0f4c2b5e9a')
$lines.Add('Download Help Link: https://github.com/GraceSolutions/PSRecoveryPartition')
$lines.Add('Help Version: 0.1.0')
$lines.Add('Locale: en-US')
$lines.Add('---')
$lines.Add('')
$lines.Add('# PSRecoveryPartition Module')
$lines.Add('## Description')
$lines.Add('PowerShell module for managing Windows recovery partitions, Windows Recovery Environment, and recovery boot entries. Uses native Windows APIs, Storage objects, CIM, WMI, and controlled Microsoft inbox process fallback where required.')
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
