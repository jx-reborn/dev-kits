[CmdletBinding()]
param(
    [string]$SourceClientDir = "",
    [string]$OutArtifactDir = "",
    [switch]$SkipProvision,
    [switch]$SkipSmokeTest,
    [switch]$UsePrebuilt
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot "..\..")).Path
$buildDir = Join-Path $scriptRoot "build"
$outArtifactDir = if ([string]::IsNullOrWhiteSpace($OutArtifactDir)) {
    Join-Path $repoRoot "artifacts\headless-client"
} else {
    [System.IO.Path]::GetFullPath($OutArtifactDir)
}

if (-not $SkipProvision) {
    if ([string]::IsNullOrWhiteSpace($SourceClientDir)) {
        if (-not [string]::IsNullOrWhiteSpace($env:LEGACY_CLIENT_DIR)) {
            $SourceClientDir = $env:LEGACY_CLIENT_DIR
        } else {
            $siblingSeed = Join-Path (Split-Path -Parent $repoRoot) "legacy-client-seed"
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
        throw "Read-only source client directory not found: $SourceClientDir"
    }
}

$sdkRoot = "C:\Program Files (x86)\Windows Kits\10"
$sdkIncludeRoot = Join-Path $sdkRoot "Include"
$sdkLibRoot = Join-Path $sdkRoot "Lib"
$sdkVer = $null
$msvcCl = $null

if (-not $UsePrebuilt) {
    $msvcCl = Get-ChildItem -Path "C:\Program Files*\Microsoft Visual Studio\*\*\VC\Tools\MSVC\*\bin\Hostx64\x86\cl.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if (Test-Path -LiteralPath $sdkIncludeRoot) {
        $sdkVer = Get-ChildItem -Path $sdkIncludeRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match "^10\.0\." } |
            Sort-Object Name -Descending |
            Select-Object -First 1 -ExpandProperty Name
    }
}

$nullRepOut = $null
$winmmOut = $null

if ((-not $UsePrebuilt) -and $msvcCl -and $sdkVer) {
    Write-Host "==> Locating x86 MSVC compiler and Windows 10 SDK..."
    $msvcBinDir = $msvcCl.Directory.FullName
    $msvcVerDir = (Resolve-Path (Join-Path $msvcBinDir "..\..\..")).Path
    $msvcInclude = Join-Path $msvcVerDir "include"
    if (-not (Test-Path $msvcInclude)) {
        $msvcInclude = Join-Path $scriptRoot "msvc_compat"
    }
    $msvcLibX86 = Join-Path $msvcVerDir "lib\x86"
    if (-not (Test-Path $msvcLibX86)) {
        $msvcLibX86 = Join-Path $msvcVerDir "lib\onecore\x86"
    }

    Write-Host "    MSVC x86 : $msvcVerDir"
    Write-Host "    MSVC Inc : $msvcInclude"
    Write-Host "    MSVC Lib : $msvcLibX86"
    Write-Host "    WinSDK   : $sdkVer"

    $env:PATH = "$msvcBinDir;$env:PATH"
    $env:INCLUDE = "$msvcInclude;$sdkIncludeRoot\$sdkVer\ucrt;$sdkIncludeRoot\$sdkVer\um;$sdkIncludeRoot\$sdkVer\shared;$sdkIncludeRoot\$sdkVer\winrt"
    $env:LIB = "$msvcLibX86;$sdkLibRoot\$sdkVer\ucrt\x86;$sdkLibRoot\$sdkVer\um\x86"

    New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

    Write-Host "==> Compiling represent2.dll (NullRepresent2)..."
    $nullRepCpp = Join-Path $scriptRoot "NullRepresent2\NullRepresent2.cpp"
    $nullRepDef = Join-Path $scriptRoot "NullRepresent2\represent2.def"
    $nullRepOut = Join-Path $buildDir "represent2.dll"

    & $msvcCl.FullName /nologo /LD /MT /O2 /EHs-c- /GR- /std:c++17 /D_CRT_SECURE_NO_WARNINGS /W3 `
        "/Fo$buildDir\NullRepresent2.obj" `
        "/Fe$nullRepOut" `
        $nullRepCpp `
        /link "/DEF:$nullRepDef" "/IMPLIB:$buildDir\represent2.lib" "/PDB:$buildDir\represent2.pdb" kernel32.lib user32.lib gdi32.lib legacy_stdio_definitions.lib
    if ($LASTEXITCODE -ne 0) {
        throw "Compilation of represent2.dll failed with exit code $LASTEXITCODE."
    }

    Write-Host "==> Compiling winmm.dll (JxHeadlessAgent)..."
    $agentDir = Join-Path $scriptRoot "JxHeadlessAgent"
    $winmmDef = Join-Path $agentDir "winmm.def"
    $winmmOut = Join-Path $buildDir "winmm.dll"
    $agentSources = @(
        (Join-Path $agentDir "WinmmProxy.cpp"),
        (Join-Path $agentDir "CrashHandler.cpp"),
        (Join-Path $agentDir "Hooks.cpp"),
        (Join-Path $agentDir "JxMemory.cpp"),
        (Join-Path $agentDir "PipeServer.cpp")
    )

    & $msvcCl.FullName /nologo /LD /MT /O2 /EHs-c- /GR- /std:c++17 /D_CRT_SECURE_NO_WARNINGS /W3 `
        "/I$(Join-Path $scriptRoot 'include')" `
        "/I$agentDir" `
        "/Fo$buildDir\\" `
        "/Fe$winmmOut" `
        @agentSources `
        /link "/DEF:$winmmDef" "/IMPLIB:$buildDir\winmm.lib" "/PDB:$buildDir\winmm.pdb" kernel32.lib user32.lib advapi32.lib shell32.lib legacy_stdio_definitions.lib
    if ($LASTEXITCODE -ne 0) {
        throw "Compilation of winmm.dll failed with exit code $LASTEXITCODE."
    }
} else {
    $prebuiltCandidates = @()
    if (-not [string]::IsNullOrWhiteSpace($SourceClientDir)) {
        $prebuiltCandidates += (Join-Path $SourceClientDir "prebuilt")
    }
    $prebuiltCandidates += $buildDir

    foreach ($cand in $prebuiltCandidates) {
        $candRep = Join-Path $cand "represent2.dll"
        $candWinmm = Join-Path $cand "winmm.dll"
        if ((Test-Path -LiteralPath $candRep) -and (Test-Path -LiteralPath $candWinmm)) {
            $nullRepOut = $candRep
            $winmmOut = $candWinmm
            $reason = if ($UsePrebuilt) { "(-UsePrebuilt requested)" } else { "(MSVC x86 or Windows 10 SDK not found)" }
            Write-Host "==> Using prebuilt represent2.dll and winmm.dll from $cand $reason..."
            break
        }
    }

    if (-not $nullRepOut -or -not $winmmOut) {
        throw "Could not find MSVC x86 / Windows 10 SDK to compile native DLLs, and no prebuilt represent2.dll + winmm.dll were found under '$SourceClientDir\prebuilt' or '$buildDir'. Install Visual Studio C++ Build Tools (MSVC x86 + Windows 10/11 SDK) or restore jx-headless-client-seed.tar.gz containing prebuilt\."
    }
}

if (-not $SkipProvision) {
    Write-Host "==> Provisioning headless client directory (read-only from $SourceClientDir -> $outArtifactDir)..."

    # First safely unlink any pre-existing directory junctions WITHOUT touching $SourceClientDir!
    if (Test-Path $outArtifactDir) {
        Get-ChildItem -Path $outArtifactDir -Force | ForEach-Object {
            if ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
                cmd /c rmdir "$($_.FullName)" | Out-Null
            }
        }
    }
    New-Item -ItemType Directory -Force -Path $outArtifactDir | Out-Null

    $copyBinaries = @(
        "game.exe",
        "engine.dll",
        "rainbow.dll",
        "lualibdll.dll",
        "zlib.dll",
        "package.ini"
    )
    foreach ($f in $copyBinaries) {
        $src = Join-Path $SourceClientDir $f
        if (-not (Test-Path $src)) {
            throw "Required client file missing from source: $src"
        }
        Copy-Item -Path $src -Destination (Join-Path $outArtifactDir $f) -Force
    }

    # Strip corrupted (HashMismatch) Authenticode signature from the provisioned game.exe copy (leaving SourceClientDir untouched).
    $copiedGameExe = Join-Path $outArtifactDir "game.exe"
    $gameSig = Get-AuthenticodeSignature -FilePath $copiedGameExe
    if ($gameSig.Status -eq [System.Management.Automation.SignatureStatus]::HashMismatch) {
        $signtoolExe = $null
        if ($sdkVer) {
            $signtoolCandidates = @(
                (Join-Path $sdkRoot "bin\$sdkVer\x64\signtool.exe"),
                (Join-Path $sdkRoot "bin\$sdkVer\x86\signtool.exe")
            )
            $signtoolExe = $signtoolCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
        }
        $sdkBinRoot = Join-Path $sdkRoot "bin"
        if (-not $signtoolExe -and (Test-Path -LiteralPath $sdkBinRoot)) {
            $signtoolExe = Get-ChildItem -Path (Join-Path $sdkBinRoot "*\x64\signtool.exe") -ErrorAction SilentlyContinue |
                Sort-Object FullName -Descending |
                Select-Object -First 1 -ExpandProperty FullName
        }

        if ($signtoolExe) {
            Write-Host "    Stripping broken Authenticode signature (HashMismatch) from provisioned game.exe via signtool.exe..."
            & $signtoolExe remove /s $copiedGameExe | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "signtool remove /s failed with exit code $LASTEXITCODE."
            }
        } else {
            Write-Host "    Stripping broken Authenticode signature (HashMismatch) from provisioned game.exe via Win32 imagehlp.dll..."
            if (-not ([System.Management.Automation.PSTypeName]'JxNativeImageHlp').Type) {
                Add-Type -TypeDefinition @"
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class JxNativeImageHlp
{
    private const ushort CERT_SECTION_TYPE_ANY = 255;

    [DllImport("imagehlp.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImageEnumerateCertificates(
        SafeFileHandle FileHandle,
        ushort TypeFilter,
        out uint CertificateCount,
        IntPtr Indices,
        uint IndexCount);

    [DllImport("imagehlp.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImageRemoveCertificate(
        SafeFileHandle FileHandle,
        uint Index);

    public static void StripAllCertificates(string filePath)
    {
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            uint count;
            if (!ImageEnumerateCertificates(fs.SafeFileHandle, CERT_SECTION_TYPE_ANY, out count, IntPtr.Zero, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "ImageEnumerateCertificates failed.");
            }
            for (uint i = 0; i < count; i++)
            {
                if (!ImageRemoveCertificate(fs.SafeFileHandle, 0))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "ImageRemoveCertificate failed.");
                }
            }
        }
    }
}
"@
            }
            [JxNativeImageHlp]::StripAllCertificates($copiedGameExe)
        }

        if ((Get-AuthenticodeSignature -FilePath $copiedGameExe).Status -eq [System.Management.Automation.SignatureStatus]::HashMismatch) {
            throw "Failed to clear HashMismatch Authenticode signature from '$copiedGameExe'."
        }
    }

    # Normalize package.ini so KPakList mounts whichever of data/*.pak or data1/*.pak exists (without modifying SourceClientDir).
    $pkgIniPath = Join-Path $outArtifactDir "package.ini"
    $hasDataMaps = Test-Path (Join-Path $SourceClientDir "data\maps.pak")
    $hasData1Maps = Test-Path (Join-Path $SourceClientDir "data1\maps.pak")
    if (-not $hasDataMaps -and $hasData1Maps) {
        $latin1 = [System.Text.Encoding]::GetEncoding(28591)
        $pkgText = [System.IO.File]::ReadAllText($pkgIniPath, $latin1)
        $updatedPkgText = [regex]::Replace($pkgText, '(?m)^(Path\s*=\s*)\\\\?data(?=[ \t]*\r?$)', '${1}\\data1')
        if ($updatedPkgText -ne $pkgText) {
            [System.IO.File]::WriteAllText($pkgIniPath, $updatedPkgText, $latin1)
        }
    } elseif ($hasDataMaps -and -not $hasData1Maps) {
        $latin1 = [System.Text.Encoding]::GetEncoding(28591)
        $pkgText = [System.IO.File]::ReadAllText($pkgIniPath, $latin1)
        $updatedPkgText = [regex]::Replace($pkgText, '(?m)^(Path\s*=\s*)\\\\?data1(?=[ \t]*\r?$)', '${1}\\data')
        if ($updatedPkgText -ne $pkgText) {
            [System.IO.File]::WriteAllText($pkgIniPath, $updatedPkgText, $latin1)
        }
    }

    $filterOrig = Join-Path $SourceClientDir "filtertext_orig.dll"
    $filterFallback = Join-Path $SourceClientDir "filtertext.dll"
    if (Test-Path $filterOrig) {
        Copy-Item -Path $filterOrig -Destination (Join-Path $outArtifactDir "filtertext.dll") -Force
    } elseif (Test-Path $filterFallback) {
        Copy-Item -Path $filterFallback -Destination (Join-Path $outArtifactDir "filtertext.dll") -Force
    } else {
        throw "Neither filtertext_orig.dll nor filtertext.dll found in $SourceClientDir"
    }

    Copy-Item -Path $nullRepOut -Destination (Join-Path $outArtifactDir "represent2.dll") -Force
    Copy-Item -Path $winmmOut -Destination (Join-Path $outArtifactDir "winmm.dll") -Force

    # Read-only asset junctions
    $junctionDirs = @("data", "data1", "music", "spr", "lang", "script", "ui", "pic", "game", "assets", "Login")
    foreach ($d in $junctionDirs) {
        $srcDir = Join-Path $SourceClientDir $d
        $dstDir = Join-Path $outArtifactDir $d
        if (Test-Path $srcDir) {
            if (Test-Path $dstDir) {
                cmd /c rmdir "$dstDir" | Out-Null
            }
            cmd /c mklink /J "`"$dstDir`"" "`"$srcDir`"" | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to create directory junction $dstDir -> $srcDir"
            }
        }
    }

    # Writable per-client config/state folders copied (not junctioned) so $SourceClientDir stays untouched
    foreach ($d in @("settings", "UserData")) {
        $srcDir = Join-Path $SourceClientDir $d
        $dstDir = Join-Path $outArtifactDir $d
        if (Test-Path $srcDir) {
            if (Test-Path $dstDir) {
                Remove-Item -Path $dstDir -Recurse -Force
            }
            Copy-Item -Path $srcDir -Destination $dstDir -Recurse -Force
        }
    }

    New-Item -ItemType Directory -Force -Path (Join-Path $outArtifactDir "Logs") | Out-Null

    # Copy any serverlist / represent / ui ini if present at root
    foreach ($optIni in @("resolution.ini", "serverlist.ini")) {
        $optSrc = Join-Path $SourceClientDir $optIni
        if (Test-Path $optSrc) {
            Copy-Item -Path $optSrc -Destination (Join-Path $outArtifactDir $optIni) -Force
        }
    }

    $cleanConfig = @"
[Server]
ServerOn=0
GameServPort=5622
DenialPort=5623

[Client]
CanOpenUrlWhenExit=0
FullScreen=0
Represent=2
FPS=0
DynamicLight=0
CapPath=.\pic
KeepOutSide=1
AwardPosition=-1
Bil_StartAuto=0
ShowMiniskill=1
Plugin=0
Theme=CTC
"@
    Set-Content -Path (Join-Path $outArtifactDir "config.ini") -Value $cleanConfig -Encoding ASCII
}

if (-not $SkipSmokeTest) {
    Write-Host "==> Running headless game.exe smoke test..."
    $pipeName = "jx-smoke-" + [Guid]::NewGuid().ToString("N").Substring(0, 8)
    $crashPath = Join-Path $env:TEMP "$pipeName-crash.json"
    if (Test-Path $crashPath) { Remove-Item $crashPath -Force }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = Join-Path $outArtifactDir "game.exe"
    $psi.WorkingDirectory = $outArtifactDir
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.EnvironmentVariables["LEGACY_HEADLESS"] = "1"
    $psi.EnvironmentVariables["LEGACY_PIPE_NAME"] = $pipeName
    $psi.EnvironmentVariables["LEGACY_CRASH_DUMP_PATH"] = $crashPath

    $proc = [System.Diagnostics.Process]::Start($psi)
    if (-not $proc) {
        throw "Failed to launch game.exe"
    }

    try {
        $pipeClient = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, [System.IO.Pipes.PipeDirection]::InOut)
        $connected = $false
        $deadline = [DateTime]::UtcNow.AddSeconds(15)
        while ([DateTime]::UtcNow -lt $deadline) {
            if ($proc.HasExited) {
                $crashInfo = if (Test-Path $crashPath) { Get-Content $crashPath -Raw } else { "(no crash dump)" }
                throw "game.exe exited prematurely with code $($proc.ExitCode). Crash dump: $crashInfo"
            }
            try {
                $pipeClient.Connect(250)
                $connected = $true
                break
            } catch [System.TimeoutException] {
                Start-Sleep -Milliseconds 100
            }
        }
        if (-not $connected) {
            throw "Timed out waiting for Named Pipe \\.\pipe\$pipeName"
        }

        $writer = New-Object System.IO.StreamWriter($pipeClient, [System.Text.Encoding]::ASCII, 4096, $true)
        $writer.AutoFlush = $true
        $reader = New-Object System.IO.StreamReader($pipeClient, [System.Text.Encoding]::UTF8, $false, 4096, $true)

        function Invoke-SmokeRpc([string]$id, [string]$method) {
            $writer.WriteLine("{`"id`":`"$id`",`"method`":`"$method`",`"params`":{}}")
            $line = $reader.ReadLine()
            if ([string]::IsNullOrWhiteSpace($line)) {
                [void]$proc.WaitForExit(250)
                $exitStr = if ($proc.HasExited) { ("0x{0:X8} ({1})" -f $proc.ExitCode, $proc.ExitCode) } else { "still running" }
                $crashInfo = if (Test-Path $crashPath) { Get-Content $crashPath -Raw } else { "none" }
                throw "Empty RPC response for $method (ExitCode=$exitStr, CrashDump=$crashInfo)"
            }
            $obj = $line | ConvertFrom-Json
            if (-not $obj.ok) {
                throw "RPC $method failed: $($obj.error)"
            }
            return $obj.result
        }

        $ping = Invoke-SmokeRpc "1" "ping"
        $state1 = Invoke-SmokeRpc "2" "get_state"
        Start-Sleep -Milliseconds 350
        $state2 = Invoke-SmokeRpc "3" "get_state"

        $proc.Refresh()
        if ($state2.window_visible) {
            throw "Expected game.exe window_visible == false in Zero-GUI mode, got true"
        }
        if ($proc.MainWindowHandle -ne [IntPtr]::Zero -and [int64]$proc.MainWindowHandle -eq [int64]$state2.hwnd) {
            throw "game.exe main HWND $($state2.hwnd) is visible!"
        }
        if ([int64]$state2.tick_count -le [int64]$state1.tick_count) {
            throw "GameLoop tick_count did not advance: $($state1.tick_count) -> $($state2.tick_count)"
        }

        Write-Host "    Smoke PING : pid=$($ping.pid), tick=$($ping.tick_count)"
        Write-Host "    Smoke TICKS: $($state1.tick_count) -> $($state2.tick_count) (HWND=$($proc.MainWindowHandle), visible=$($state2.window_visible))"

        $quit = Invoke-SmokeRpc "4" "quit"
        if (-not $proc.WaitForExit(10000)) {
            throw "game.exe did not exit within 10s after quit"
        }
        if ($proc.ExitCode -ne 0) {
            $crashInfo = if (Test-Path $crashPath) { Get-Content $crashPath -Raw } else { "(no crash dump)" }
            throw "game.exe exited with code $($proc.ExitCode) instead of 0. Crash: $crashInfo"
        }

        Write-Host "    Smoke EXIT : clean exit code 0!"
        $pipeClient.Dispose()
    }
    finally {
        if (-not $proc.HasExited) {
            try { $proc.Kill($true) } catch {}
        }
        $proc.Dispose()
        if (Test-Path $crashPath) { Remove-Item $crashPath -Force -ErrorAction SilentlyContinue }
    }

    $expectedHeadlessExe = Join-Path $outArtifactDir "game.exe"
    $leaked = Get-Process -Name "game" -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $expectedHeadlessExe }
    if ($leaked) {
        throw "Found leaked headless game.exe process(es): $($leaked.Id -join ', ')"
    }
}

Write-Host "==> build-native.ps1 completed cleanly."
