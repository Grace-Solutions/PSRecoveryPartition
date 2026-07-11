[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$docsDir = Join-Path $repoRoot 'docs'

# Per-cmdlet content. Each entry supplies: Synopsis, Description, OneLiner, Splat (optional),
# ExtraExamples (optional; array of @{Title;Code;Description}), Notes (optional),
# Parameters (optional). A '__Common' entry supplies cross-cutting parameter descriptions.
$content = . (Join-Path $PSScriptRoot 'HelpContent.ps1')
$common  = if ($content.ContainsKey('__Common')) { $content['__Common'] } else { @{} }

function Update-Section {
    param([string]$Text, [string]$Heading, [string]$Replacement)
    $escHeading = [regex]::Escape($Heading)
    $pattern = "(?ms)^$escHeading\r?\n.*?(?=^##\s|\z)"
    if ($Text -match $pattern) {
        return [regex]::Replace($Text, $pattern, $Replacement, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    }
    return $Text
}

function Update-ParameterDescriptions {
    param([string]$Text, [hashtable]$Common, [hashtable]$Overrides)
    $missing = @()
    $result = [regex]::Replace($Text, '(?m)^\{\{ Fill (\w+) Description \}\}\s*$', {
        param($match)
        $name = $match.Groups[1].Value
        if ($Overrides -and $Overrides.ContainsKey($name)) { return $Overrides[$name] }
        if ($Common.ContainsKey($name)) { return $Common[$name] }
        $script:missing += $name
        return $match.Value
    })
    return ,$result, $missing
}

foreach ($cmd in $content.Keys) {
    if ($cmd -eq '__Common') { continue }
    $path = Join-Path $docsDir "$cmd.md"
    if (-not (Test-Path $path)) { Write-Warning "Missing: $path"; continue }
    $entry = $content[$cmd]
    $text  = Get-Content -LiteralPath $path -Raw

    $syn = "## SYNOPSIS`r`n$($entry.Synopsis)`r`n`r`n"
    $text = Update-Section -Text $text -Heading '## SYNOPSIS' -Replacement $syn

    $desc = "## DESCRIPTION`r`n$($entry.Description)`r`n`r`n"
    $text = Update-Section -Text $text -Heading '## DESCRIPTION' -Replacement $desc

    $examples = "## EXAMPLES`r`n`r`n### Example 1: Single-line usage`r`n``````powershell`r`n$($entry.OneLiner)`r`n```````r`n`r`n$($entry.OneLinerDescription)`r`n`r`n"
    $exampleNumber = 2
    if ($entry.ContainsKey('Splat')) {
        $examples += "### Example ${exampleNumber}: Splatted parameters with OrderedDictionary`r`n``````powershell`r`n$($entry.Splat)`r`n```````r`n`r`n$($entry.SplatDescription)`r`n`r`n"
        $exampleNumber++
    }
    if ($entry.ContainsKey('ExtraExamples')) {
        foreach ($ex in @($entry['ExtraExamples'])) {
            $examples += "### Example ${exampleNumber}: $($ex.Title)`r`n``````powershell`r`n$($ex.Code)`r`n```````r`n`r`n$($ex.Description)`r`n`r`n"
            $exampleNumber++
        }
    }
    $text = Update-Section -Text $text -Heading '## EXAMPLES' -Replacement $examples

    if ($entry.ContainsKey('Notes')) {
        $notes = "## NOTES`r`n$($entry.Notes)`r`n`r`n"
        $text = Update-Section -Text $text -Heading '## NOTES' -Replacement $notes
    }

    $script:missing = @()
    $overrides = if ($entry.ContainsKey('Parameters')) { $entry['Parameters'] } else { @{} }
    $pair = Update-ParameterDescriptions -Text $text -Common $common -Overrides $overrides
    $text = $pair[0]
    if ($pair[1].Count -gt 0) {
        Write-Warning ("  ${cmd}: missing parameter descriptions: " + (($pair[1] | Sort-Object -Unique) -join ', '))
    }

    Set-Content -LiteralPath $path -Value $text -Encoding utf8
    Write-Host "  updated: $cmd.md"
}
