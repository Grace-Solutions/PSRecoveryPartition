[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$docsDir = Join-Path $repoRoot 'docs'

# Per-cmdlet content. Each entry supplies: Synopsis, Description, OneLiner, Splat (optional).
$content = . (Join-Path $PSScriptRoot 'HelpContent.ps1')

function Update-Section {
    param([string]$Text, [string]$Heading, [string]$Replacement)
    $escHeading = [regex]::Escape($Heading)
    $pattern = "(?ms)^$escHeading\r?\n.*?(?=^##\s|\z)"
    if ($Text -match $pattern) {
        return [regex]::Replace($Text, $pattern, $Replacement, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    }
    return $Text
}

foreach ($cmd in $content.Keys) {
    $path = Join-Path $docsDir "$cmd.md"
    if (-not (Test-Path $path)) { Write-Warning "Missing: $path"; continue }
    $entry = $content[$cmd]
    $text  = Get-Content -LiteralPath $path -Raw

    $syn = "## SYNOPSIS`r`n$($entry.Synopsis)`r`n`r`n"
    $text = Update-Section -Text $text -Heading '## SYNOPSIS' -Replacement $syn

    $desc = "## DESCRIPTION`r`n$($entry.Description)`r`n`r`n"
    $text = Update-Section -Text $text -Heading '## DESCRIPTION' -Replacement $desc

    $examples = "## EXAMPLES`r`n`r`n### Example 1: Single-line usage`r`n``````powershell`r`n$($entry.OneLiner)`r`n```````r`n`r`n$($entry.OneLinerDescription)`r`n`r`n"
    if ($entry.ContainsKey('Splat')) {
        $examples += "### Example 2: Splatted parameters with OrderedDictionary`r`n``````powershell`r`n$($entry.Splat)`r`n```````r`n`r`n$($entry.SplatDescription)`r`n`r`n"
    }
    $text = Update-Section -Text $text -Heading '## EXAMPLES' -Replacement $examples

    Set-Content -LiteralPath $path -Value $text -Encoding utf8
    Write-Host "  updated: $cmd.md"
}
