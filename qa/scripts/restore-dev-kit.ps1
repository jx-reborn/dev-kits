<#
.SYNOPSIS
    Verifies and restores a portable JXReborn Dev-Kit bundle onto a new machine,
    compiles & provisions the headless game.exe client via build-native.ps1, and
    optionally bootstraps REA MCP, Spec Kit CLI, and global AI agent skills.
#>
[CmdletBinding()]
param(
    [string]$BundleDir = "",

    [string]$ClientSeedDir = "",

    [string]$LegacyServerDir = "",

    [string]$OutHeadlessDir = "",

    [string]$GhidraInstallDir = "",

    [string]$JavaHome = "",

    [string]$DownloadRelease = "",

    [string]$ReleaseRepo = "",

    [switch]$ClientOnly,

    [switch]$SkipProvision,

    [switch]$SkipSmokeTest,

    [switch]$UsePrebuilt,

    [switch]$Force,

    [switch]$SetupAgentTools
)

$ErrorActionPreference = "Stop"
$QaRoot = Split-Path -Parent $PSScriptRoot
$RepoRoot = Split-Path -Parent $QaRoot
$ParentRoot = Split-Path -Parent $RepoRoot

function Resolve-ToolCmd([string]$Name, [string[]]$Fallbacks) {
    $found = Get-Command $Name -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    foreach ($candidate in $Fallbacks) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }
    return $null
}

if ([string]::IsNullOrWhiteSpace($ReleaseRepo)) {
    $ReleaseRepo = if (-not [string]::IsNullOrWhiteSpace($env:LEGACY_DEVKIT_REPO)) {
        $env:LEGACY_DEVKIT_REPO
    } else {
        "Cutect/jx-reborn-dev-kits"
    }
}

if ([string]::IsNullOrWhiteSpace($BundleDir)) {
    $BundleDir = Join-Path $RepoRoot "artifacts\dev-kit"
}
if ([string]::IsNullOrWhiteSpace($ClientSeedDir)) {
    $ClientSeedDir = if (-not [string]::IsNullOrWhiteSpace($env:LEGACY_CLIENT_SEED_DIR)) {
        $env:LEGACY_CLIENT_SEED_DIR
    } else {
        ($(if (Test-Path (Join-Path $ParentRoot "legacy-client-seed")) { Join-Path $ParentRoot "legacy-client-seed" } else { Join-Path (Split-Path -Parent $ParentRoot) "legacy-client-seed" }))
    }
}
if ([string]::IsNullOrWhiteSpace($LegacyServerDir)) {
    $LegacyServerDir = if (-not [string]::IsNullOrWhiteSpace($env:LEGACY_SERVER_DIR)) {
        $env:LEGACY_SERVER_DIR
    } else {
        ($(if (Test-Path (Join-Path $ParentRoot "legacy-server")) { Join-Path $ParentRoot "legacy-server" } else { Join-Path (Split-Path -Parent $ParentRoot) "legacy-server" }))
    }
}

if (-not [string]::IsNullOrWhiteSpace($DownloadRelease)) {
    $ghExe = Resolve-ToolCmd "gh.exe" @(
        (Join-Path $env:ProgramFiles "GitHub CLI\gh.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\GitHub CLI\gh.exe")
    )
    if (-not $ghExe) {
        throw "GitHub CLI (gh.exe) not found. Install GitHub CLI and run 'gh auth login' first."
    }
    New-Item -ItemType Directory -Force -Path $BundleDir | Out-Null
    Write-Host "==> Downloading release '$DownloadRelease' from '$ReleaseRepo' -> '$BundleDir'..." -ForegroundColor Cyan
    & $ghExe release download $DownloadRelease --repo $ReleaseRepo --dir $BundleDir --clobber
    if ($LASTEXITCODE -ne 0) {
        throw "gh release download failed with exit code $LASTEXITCODE."
    }
}

$pyScript = Join-Path $PSScriptRoot "dev_kit_packager.py"
$pyArgs = @(
    $pyScript,
    "unpack",
    "--bundle-dir", $BundleDir,
    "--client-seed-dir", $ClientSeedDir
)

if ($ClientOnly) {
    $pyArgs += "--client-only"
} else {
    $ghidraProjectDir = Join-Path $RepoRoot "artifacts\ghidra-jxclient"
    $ghidraLinuxProjectDir = Join-Path $RepoRoot "artifacts\ghidra-jxlinux"
    $pyArgs += @(
        "--linux-bin-dir", $LegacyServerDir,
        "--ghidra-project-dir", $ghidraProjectDir,
        "--ghidra-linux-project-dir", $ghidraLinuxProjectDir
    )
}

if ($Force) {
    $pyArgs += "--force"
}

& python @pyArgs
if ($LASTEXITCODE -ne 0) {
    throw "dev_kit_packager.py unpack failed with exit code $LASTEXITCODE."
}

if (-not ($SkipProvision -and $SkipSmokeTest)) {
    $buildNative = Join-Path $QaRoot "JXReborn.ClientDriver.Native\build-native.ps1"
    $nativeArgs = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $buildNative,
        "-SourceClientDir", $ClientSeedDir
    )
    if (-not [string]::IsNullOrWhiteSpace($OutHeadlessDir)) {
        $nativeArgs += @("-OutArtifactDir", $OutHeadlessDir)
    }
    if ($UsePrebuilt) {
        $nativeArgs += "-UsePrebuilt"
    }
    if ($SkipProvision) {
        $nativeArgs += "-SkipProvision"
    }
    if ($SkipSmokeTest) {
        $nativeArgs += "-SkipSmokeTest"
    }

    & powershell @nativeArgs
    if ($LASTEXITCODE -ne 0) {
        throw "build-native.ps1 failed with exit code $LASTEXITCODE."
    }
}

if ($SetupAgentTools) {
    Write-Host "==> Bootstrapping REA MCP, Spec Kit CLI, and Global Skills..." -ForegroundColor Cyan

    $npmCmd = Resolve-ToolCmd "npm.cmd" @((Join-Path $env:ProgramFiles "nodejs\npm.cmd"))
    $npxCmd = Resolve-ToolCmd "npx.cmd" @((Join-Path $env:ProgramFiles "nodejs\npx.cmd"))

    if ($npmCmd) {
        New-Item -ItemType Directory -Force -Path "$env:APPDATA\npm" | Out-Null
        & $npmCmd install -g rea-agents@latest
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to install rea-agents globally."
        }

        if (-not [string]::IsNullOrWhiteSpace($GhidraInstallDir)) {
            $env:GHIDRA_INSTALL_DIR = [System.IO.Path]::GetFullPath($GhidraInstallDir)
        } elseif ([string]::IsNullOrWhiteSpace($env:GHIDRA_INSTALL_DIR)) {
            $siblingGhidra = Join-Path $ParentRoot "ghidra_12.2_PUBLIC"
            if (Test-Path -LiteralPath $siblingGhidra) {
                $env:GHIDRA_INSTALL_DIR = $siblingGhidra
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($JavaHome)) {
            $env:JAVA_HOME = [System.IO.Path]::GetFullPath($JavaHome)
        } elseif ([string]::IsNullOrWhiteSpace($env:JAVA_HOME)) {
            $jdkRoots = @(
                (Join-Path $env:ProgramFiles "Eclipse Adoptium\jdk-25*"),
                (Join-Path $env:ProgramFiles "Java\jdk-25*"),
                (Join-Path $env:ProgramFiles "Microsoft\jdk-25*")
            )
            $jdk25 = Get-ChildItem -Path $jdkRoots -Directory -ErrorAction SilentlyContinue |
                Sort-Object Name -Descending | Select-Object -First 1 -ExpandProperty FullName
            if ($jdk25) { $env:JAVA_HOME = $jdk25 }
        }

        $reaCmd = Resolve-ToolCmd "rea.cmd" @((Join-Path "$env:APPDATA\npm" "rea.cmd"))
        if ($reaCmd -and -not [string]::IsNullOrWhiteSpace($env:GHIDRA_INSTALL_DIR)) {
            & $reaCmd setup --client antigravity --yes
            if ($LASTEXITCODE -ne 0) {
                throw "rea setup failed with exit code $LASTEXITCODE."
            }
        } elseif (-not $reaCmd) {
            Write-Warning "rea.cmd not found under $env:APPDATA\npm."
        } else {
            Write-Warning "Skipping 'rea setup': pass -GhidraInstallDir <path> or set `$env:GHIDRA_INSTALL_DIR and `$env:JAVA_HOME, then run 'rea setup --client antigravity --yes'."
        }
    } else {
        Write-Warning "Node.js npm.cmd not found; skipping rea-agents and global skills."
    }

    $uvExe = Resolve-ToolCmd "uv.exe" @((Join-Path "$env:USERPROFILE\.local\bin" "uv.exe"))
    if ($uvExe) {
        & $uvExe tool install specify-cli --force --from git+https://github.com/github/spec-kit.git@v1.4.1
        if ($LASTEXITCODE -ne 0) {
            throw "specify-cli installation failed."
        }
    } else {
        Write-Warning "uv.exe not found; install uv and run 'uv tool install specify-cli --force --from git+https://github.com/github/spec-kit.git@v1.4.1'."
    }

    if ($npxCmd) {
        & $npxCmd -y skills add vercel-labs/skills -g -s find-skills -a gemini-cli github-copilot antigravity --copy -y
        & $npxCmd -y skills add github/awesome-copilot -g -s create-readme documentation-writer nuget-manager -a gemini-cli github-copilot antigravity --copy -y
        & $npxCmd -y skills add cloudflare/skills -g -s agents-sdk cloudflare cloudflare-email-service cloudflare-one cloudflare-one-migrations durable-objects turnstile-spin web-perf workers-best-practices wrangler -a gemini-cli github-copilot antigravity --copy -y
    }
}

Write-Host "==> restore-dev-kit.ps1 completed cleanly." -ForegroundColor Green
