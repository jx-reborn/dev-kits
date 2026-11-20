<#
.SYNOPSIS
    Packs the non-Git JXReborn QA/development prerequisites (deduplicated sprite-stubbed
    headless client seed, clean legacy server tree, and optional Ghidra project) into
    a portable bundle with SHA-256 manifest for transferring to a new machine.
#>
[CmdletBinding()]
param(
    [string]$OutputDir = "",

    [string]$SourceClientDir = "",

    [string]$SourceLinuxDir = "",

    [string]$PrebuiltHeadlessDir = "",

    [switch]$ClientOnly,

    [switch]$IncludeGhidraProject
)

$ErrorActionPreference = "Stop"
$QaRoot = Split-Path -Parent $PSScriptRoot
$RepoRoot = Split-Path -Parent $QaRoot
$ParentRoot = Split-Path -Parent $RepoRoot

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "artifacts\dev-kit"
}

if ([string]::IsNullOrWhiteSpace($SourceClientDir)) {
    if (-not [string]::IsNullOrWhiteSpace($env:LEGACY_CLIENT_DIR)) {
        $SourceClientDir = $env:LEGACY_CLIENT_DIR
    } else {
        $siblingSeed = ($(if (Test-Path (Join-Path $ParentRoot "legacy-client-seed")) { Join-Path $ParentRoot "legacy-client-seed" } else { Join-Path (Split-Path -Parent $ParentRoot) "legacy-client-seed" }))
        if (Test-Path -LiteralPath $siblingSeed) {
            $SourceClientDir = $siblingSeed
        } elseif ([Environment]::UserInteractive -and -not [Console]::IsInputRedirected) {
            $SourceClientDir = (Read-Host "Enter path to Legacy Client or legacy-client-seed directory").Trim().Trim('"')
        }
    }
}

if ([string]::IsNullOrWhiteSpace($SourceClientDir)) {
    throw "SourceClientDir is required. Pass -SourceClientDir <path> or set `$env:LEGACY_CLIENT_DIR."
}

$SourceClientDir = [System.IO.Path]::GetFullPath($SourceClientDir)
if (-not (Test-Path -LiteralPath $SourceClientDir)) {
    throw "Source client directory not found: $SourceClientDir"
}

if (-not $ClientOnly) {
    if ([string]::IsNullOrWhiteSpace($SourceLinuxDir)) {
        if (-not [string]::IsNullOrWhiteSpace($env:LEGACY_SERVER_DIR)) {
            $SourceLinuxDir = $env:LEGACY_SERVER_DIR
        } else {
            $SourceLinuxDir = ($(if (Test-Path (Join-Path $ParentRoot "legacy-server")) { Join-Path $ParentRoot "legacy-server" } else { Join-Path (Split-Path -Parent $ParentRoot) "legacy-server" }))
        }
    }
    $SourceLinuxDir = [System.IO.Path]::GetFullPath($SourceLinuxDir)
}

$pyScript = Join-Path $PSScriptRoot "dev_kit_packager.py"
$pyArgs = @(
    $pyScript,
    "pack",
    "--output-dir", $OutputDir,
    "--source-client-dir", $SourceClientDir
)

if ([string]::IsNullOrWhiteSpace($PrebuiltHeadlessDir)) {
    $candidates = @(
        (Join-Path $RepoRoot "artifacts\headless-client"),
        (Join-Path $QaRoot "JXReborn.ClientDriver.Native\build")
    )
    foreach ($cand in $candidates) {
        if ((Test-Path -LiteralPath (Join-Path $cand "represent2.dll")) -and (Test-Path -LiteralPath (Join-Path $cand "winmm.dll"))) {
            $PrebuiltHeadlessDir = $cand
            break
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($PrebuiltHeadlessDir)) {
    $PrebuiltHeadlessDir = [System.IO.Path]::GetFullPath($PrebuiltHeadlessDir)
    if (Test-Path -LiteralPath $PrebuiltHeadlessDir) {
        $pyArgs += @("--prebuilt-headless-dir", $PrebuiltHeadlessDir)
    }
}

if ($ClientOnly) {
    $pyArgs += "--client-only"
} else {
    $pyArgs += @("--source-linux-dir", $SourceLinuxDir)
}

if ($IncludeGhidraProject) {
    $ghidraDir = Join-Path $RepoRoot "artifacts\ghidra-jxclient"
    $pyArgs += @("--include-ghidra-project", "--ghidra-project-dir", $ghidraDir)
    $ghidraLinuxDir = Join-Path $RepoRoot "artifacts\ghidra-jxlinux"
    if (Test-Path -LiteralPath $ghidraLinuxDir) {
        $pyArgs += @("--ghidra-linux-project-dir", $ghidraLinuxDir)
    }
}

& python @pyArgs
if ($LASTEXITCODE -ne 0) {
    throw "dev_kit_packager.py pack failed with exit code $LASTEXITCODE."
}
