<#
.SYNOPSIS
    Builds the Zero-Sim Legacy reference package (`v1`) from an external legacy archive or server directory
    for local differential parity testing.
#>
[CmdletBinding()]
param(
    [ValidateSet("v1")]
    [string]$Profile = "v1",

    [string]$SourceTarGz = "",

    [string]$SourceDir = "",

    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"
$QaRoot = Split-Path -Parent $PSScriptRoot
$RepoRoot = Split-Path -Parent $QaRoot

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "artifacts\packages"
}

if ([string]::IsNullOrWhiteSpace($SourceTarGz)) {
    if (-not [string]::IsNullOrWhiteSpace($env:LEGACY_SERVER_ARCHIVE) -and (Test-Path -LiteralPath $env:LEGACY_SERVER_ARCHIVE)) {
        $SourceTarGz = $env:LEGACY_SERVER_ARCHIVE
    }
}

if ([string]::IsNullOrWhiteSpace($SourceDir)) {
    if (-not [string]::IsNullOrWhiteSpace($env:LEGACY_SERVER_DIR) -and (Test-Path -LiteralPath $env:LEGACY_SERVER_DIR)) {
        $SourceDir = $env:LEGACY_SERVER_DIR
    } else {
        $workspaceParent = Split-Path -Parent $RepoRoot
        $grandParent = Split-Path -Parent $workspaceParent
        $candidates = @(
            (Join-Path $workspaceParent "legacy-server\bin"),
            (Join-Path $workspaceParent "legacy-server"),
            (Join-Path $grandParent "legacy-server\bin"),
            (Join-Path $grandParent "legacy-server")
        )
        foreach ($cand in $candidates) {
            if ((Test-Path -LiteralPath (Join-Path $cand "gateway")) -and (Test-Path -LiteralPath (Join-Path $cand "server1"))) {
                $SourceDir = $cand
                break
            }
        }
    }
}

$pyScript = Join-Path $PSScriptRoot "pack_legacy_profiles.py"
$pyArgs = @($pyScript, "--profile", $Profile, "--output-dir", $OutputDir)
if (-not [string]::IsNullOrWhiteSpace($SourceTarGz) -and (Test-Path -LiteralPath $SourceTarGz)) {
    $pyArgs += @("--source-tar", $SourceTarGz)
}
if (-not [string]::IsNullOrWhiteSpace($SourceDir) -and (Test-Path -LiteralPath $SourceDir)) {
    $pyArgs += @("--source-dir", $SourceDir)
}

& python @pyArgs
if ($LASTEXITCODE -ne 0) {
    throw "pack_legacy_profiles.py failed with exit code $LASTEXITCODE."
}
