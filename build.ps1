[CmdletBinding()]
param(
    [switch] $Clean,
    [switch] $Restore,
    [switch] $RunTests,
    [switch] $GenerateHelp,
    [switch] $UpdateHelp,
    [switch] $ValidateHelp,
    [switch] $CreateRelease,
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot       = $PSScriptRoot
$ProjectPath    = Join-Path $RepoRoot 'src/PSRecoveryPartition/PSRecoveryPartition.csproj'
$ModuleStageDir = Join-Path $RepoRoot 'Module/PSRecoveryPartition'
$ModuleName     = 'PSRecoveryPartition'

function Write-Section { param([string]$Text) Write-Host "`n==> $Text" -ForegroundColor Cyan }

if ($Clean) {
    Write-Section 'Clean'
    foreach ($p in @('bin','obj','artifacts','publish')) {
        Get-ChildItem -Path $RepoRoot -Directory -Recurse -Filter $p -ErrorAction SilentlyContinue |
            ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }
    }
    Get-ChildItem -Path $ModuleStageDir -File -Filter '*.dll' -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue
}

if ($Restore) {
    Write-Section 'Restore'
    dotnet restore $ProjectPath --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed ($LASTEXITCODE)." }
}

Write-Section "Build ($Configuration)"
dotnet build $ProjectPath --configuration $Configuration --nologo --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)." }

Write-Section 'Stage module binaries'
$BuildOutDir = Join-Path $RepoRoot "src/PSRecoveryPartition/bin/$Configuration"
if (-not (Test-Path $BuildOutDir)) { throw "Build output not found: $BuildOutDir" }
if (-not (Test-Path $ModuleStageDir)) { New-Item -ItemType Directory -Path $ModuleStageDir -Force | Out-Null }

$RuntimeAssembliesToCopy = @(
    'PSRecoveryPartition.dll'
    'PSRecoveryPartition.pdb'
)
foreach ($name in $RuntimeAssembliesToCopy) {
    $src = Join-Path $BuildOutDir $name
    if (Test-Path $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $ModuleStageDir $name) -Force
        Write-Host "  staged: $name"
    }
}

Write-Section 'Validate module manifest'
$ManifestPath = Join-Path $ModuleStageDir "$ModuleName.psd1"
$Manifest = Test-ModuleManifest -Path $ManifestPath -ErrorAction Stop
Write-Host "  manifest: $($Manifest.Name) $($Manifest.Version)"

if ($GenerateHelp -or $UpdateHelp -or $ValidateHelp) {
    Write-Section 'PlatyPS help'
    if (-not (Get-Module -ListAvailable -Name platyPS)) {
        Write-Host '  installing platyPS for CurrentUser'
        Install-Module -Name platyPS -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
    }
    Import-Module platyPS -ErrorAction Stop

    $DocsSource = Join-Path $RepoRoot 'docs'
    $HelpOutput = Join-Path $ModuleStageDir 'en-US'
    if (-not (Test-Path $HelpOutput)) { New-Item -ItemType Directory -Path $HelpOutput -Force | Out-Null }

    Import-Module $ManifestPath -Force -ErrorAction Stop

    if ($UpdateHelp -and (Test-Path $DocsSource)) {
        Update-MarkdownHelpModule -Path $DocsSource -RefreshModulePage -AlphabeticParamsOrder -ErrorAction Stop | Out-Null
    }
    if ($GenerateHelp) {
        New-MarkdownHelp -Module $ModuleName -OutputFolder $DocsSource -WithModulePage -Force -ErrorAction Stop | Out-Null
        & (Join-Path $RepoRoot 'build/Apply-HelpContent.ps1')
        & (Join-Path $RepoRoot 'build/Update-ModulePage.ps1')
    }
    if ($ValidateHelp) {
        $ReadmePath = Join-Path $RepoRoot 'README.md'
        if (Test-Path $ReadmePath) {
            $readme = Get-Content -LiteralPath $ReadmePath -Raw
            $missing = @()
            foreach ($cmd in $Manifest.ExportedCmdlets.Keys) {
                $expected = "docs/$cmd.md"
                if ($readme -notmatch [regex]::Escape($expected)) { $missing += $expected }
                if (-not (Test-Path (Join-Path $RepoRoot $expected))) { $missing += "$expected (file)" }
            }
            if ($missing.Count -gt 0) { throw "README/help validation failed. Missing: $($missing -join ', ')" }
        }
    }
    New-ExternalHelp -Path $DocsSource -OutputPath $HelpOutput -Force -ErrorAction SilentlyContinue | Out-Null
}

if ($RunTests) {
    Write-Section 'Run Pester tests'
    if (-not (Get-Module -ListAvailable -Name Pester)) {
        Install-Module -Name Pester -Scope CurrentUser -Force -SkipPublisherCheck -ErrorAction Stop
    }
    Import-Module Pester -ErrorAction Stop
    $TestsPath = Join-Path $RepoRoot 'tests'
    if (Test-Path $TestsPath) {
        $cfg = New-PesterConfiguration
        $cfg.Run.Path = $TestsPath
        $cfg.Run.Exit = $true
        $cfg.Output.Verbosity = 'Detailed'
        Invoke-Pester -Configuration $cfg
    }
}

if ($CreateRelease) {
    Write-Section 'Package release zip'
    $version = $Manifest.Version.ToString()
    $zipPath = Join-Path $RepoRoot "$ModuleName-$version.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path (Join-Path $ModuleStageDir '*') -DestinationPath $zipPath -Force
    Write-Host "  package: $zipPath"
}

Write-Section 'Done'
