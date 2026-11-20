<#
.SYNOPSIS
    Strip the Authenticode digital signature (IMAGE_DIRECTORY_ENTRY_SECURITY) from a PE32/PE32+ file.

.DESCRIPTION
    Creates a .bak backup before modifying the target file, zeroes out the Security
    DataDirectory entry, truncates trailing WIN_CERTIFICATE data, and recalculates
    the PE Optional Header CheckSum if one was originally set.
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Target = "D:\JXOnline\Client\game.exe",

    [switch]$DryRun,

    [switch]$OverwriteBak
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$IMAGE_DOS_SIGNATURE = [uint16]0x5A4D
$IMAGE_NT_SIGNATURE = [uint32]0x00004550
$IMAGE_NT_OPTIONAL_HDR32_MAGIC = [uint16]0x010B
$IMAGE_NT_OPTIONAL_HDR64_MAGIC = [uint16]0x020B
$IMAGE_DIRECTORY_ENTRY_SECURITY = 4

function Get-PeCheckSum {
    param(
        [byte[]]$Data,
        [int]$CheckSumOffset
    )

    $totalLen = $Data.Length
    $paddedLen = if ($totalLen % 2 -ne 0) { $totalLen + 1 } else { $totalLen }
    $buf = [byte[]]::new($paddedLen)
    [System.Buffer]::BlockCopy($Data, 0, $buf, 0, $totalLen)

    # Zero out the 4-byte CheckSum field before summing
    [System.Array]::Clear($buf, $CheckSumOffset, 4)

    [uint32]$sum = 0
    for ($i = 0; $i -lt $paddedLen; $i += 2) {
        [uint32]$word = [uint32]$buf[$i] -bor ([uint32]$buf[$i + 1] -shl 8)
        $sum += $word
        $sum = ($sum -band 0xFFFF) + ($sum -shr 16)
    }

    $sum = ($sum -band 0xFFFF) + ($sum -shr 16)
    return [uint32](($sum + [uint32]$totalLen) -band 0xFFFFFFFF)
}

function Get-PeSecurityInfo {
    param([byte[]]$Data)

    if ($Data.Length -lt 0x40) {
        throw "File is too small to be a valid PE executable."
    }

    $dosMagic = [System.BitConverter]::ToUInt16($Data, 0)
    if ($dosMagic -ne $IMAGE_DOS_SIGNATURE) {
        throw ("Invalid DOS signature: 0x{0:X4}" -f $dosMagic)
    }

    [int]$eLfanew = [System.BitConverter]::ToUInt32($Data, 0x3C)
    if ($eLfanew + 24 -gt $Data.Length) {
        throw "Invalid e_lfanew offset."
    }

    $ntSig = [System.BitConverter]::ToUInt32($Data, $eLfanew)
    if ($ntSig -ne $IMAGE_NT_SIGNATURE) {
        throw ("Invalid PE signature: 0x{0:X8}" -f $ntSig)
    }

    $optOff = $eLfanew + 24
    $magic = [System.BitConverter]::ToUInt16($Data, $optOff)
    if ($magic -eq $IMAGE_NT_OPTIONAL_HDR32_MAGIC) {
        $numRvaOff = $optOff + 92
        $dataDirOff = $optOff + 96
    }
    elseif ($magic -eq $IMAGE_NT_OPTIONAL_HDR64_MAGIC) {
        $numRvaOff = $optOff + 108
        $dataDirOff = $optOff + 112
    }
    else {
        throw ("Unsupported Optional Header magic: 0x{0:X4}" -f $magic)
    }

    $checkSumOff = $optOff + 64
    $oldCheckSum = [System.BitConverter]::ToUInt32($Data, $checkSumOff)
    $numRva = [System.BitConverter]::ToUInt32($Data, $numRvaOff)
    if ($numRva -le $IMAGE_DIRECTORY_ENTRY_SECURITY) {
        throw "NumberOfRvaAndSizes ($numRva) does not include Security directory."
    }

    $secDirOff = $dataDirOff + $IMAGE_DIRECTORY_ENTRY_SECURITY * 8
    if ($secDirOff + 8 -gt $Data.Length) {
        throw "Security DataDirectory entry is out of bounds."
    }

    [int]$certOffset = [System.BitConverter]::ToUInt32($Data, $secDirOff)
    [int]$certSize = [System.BitConverter]::ToUInt32($Data, $secDirOff + 4)

    return [pscustomobject]@{
        Magic       = $magic
        CheckSumOff = $checkSumOff
        OldCheckSum = $oldCheckSum
        SecDirOff   = $secDirOff
        CertOffset  = $certOffset
        CertSize    = $certSize
    }
}

function New-PeBackup {
    param(
        [string]$TargetPath,
        [bool]$Overwrite
    )

    $bakPath = "$TargetPath.bak"
    if ((Test-Path -LiteralPath $bakPath) -and -not $Overwrite) {
        $idx = 1
        while ($true) {
            $candidate = "$TargetPath.bak.$idx"
            if (-not (Test-Path -LiteralPath $candidate)) {
                $bakPath = $candidate
                break
            }
            $idx++
        }
    }

    [System.IO.File]::Copy($TargetPath, $bakPath, $true)
    return $bakPath
}

if (-not (Test-Path -LiteralPath $Target -PathType Leaf)) {
    Write-Error "File not found: $Target"
    exit 1
}

$resolvedTarget = (Resolve-Path -LiteralPath $Target).ProviderPath
$raw = [System.IO.File]::ReadAllBytes($resolvedTarget)
$info = Get-PeSecurityInfo -Data $raw

if ($info.CertOffset -eq 0 -and $info.CertSize -eq 0) {
    Write-Host "[INFO] No Authenticode signature found in $resolvedTarget."
    exit 0
}

Write-Host "[INFO] Target          : $resolvedTarget"
Write-Host "[INFO] File size       : $($raw.Length) bytes"
Write-Host ("[INFO] Security dir    : offset=0x{0:X}" -f $info.SecDirOff)
Write-Host ("[INFO] Certificate tbl : offset={0} (0x{0:X}), size={1} bytes" -f $info.CertOffset, $info.CertSize)
Write-Host ("[INFO] Old PE CheckSum : 0x{0:X8}" -f $info.OldCheckSum)

if ($info.CertOffset + $info.CertSize -gt $raw.Length) {
    Write-Error "Certificate table extends past EOF; refusing to modify."
    exit 1
}

# Zero out IMAGE_DIRECTORY_ENTRY_SECURITY
[System.Array]::Clear($raw, $info.SecDirOff, 8)

if ($info.CertOffset + $info.CertSize -eq $raw.Length) {
    $out = [byte[]]::new($info.CertOffset)
    [System.Buffer]::BlockCopy($raw, 0, $out, 0, $info.CertOffset)
    $action = "truncated trailing certificate ($($info.CertSize) bytes)"
}
else {
    [System.Array]::Clear($raw, $info.CertOffset, $info.CertSize)
    $out = $raw
    $action = "zeroed embedded certificate ($($info.CertSize) bytes, trailing data preserved)"
}

if ($info.OldCheckSum -ne 0) {
    $newCheckSum = Get-PeCheckSum -Data $out -CheckSumOffset $info.CheckSumOff
    $csBytes = [System.BitConverter]::GetBytes([uint32]$newCheckSum)
    [System.Buffer]::BlockCopy($csBytes, 0, $out, $info.CheckSumOff, 4)
    Write-Host ("[INFO] New PE CheckSum : 0x{0:X8}" -f $newCheckSum)
}
else {
    Write-Host "[INFO] New PE CheckSum : 0x00000000 (unchanged)"
}

if ($DryRun) {
    Write-Host "[DRY-RUN] Would create backup and write $($out.Length) bytes ($action)."
    exit 0
}

$bakPath = New-PeBackup -TargetPath $resolvedTarget -Overwrite $OverwriteBak.IsPresent
Write-Host "[OK] Backup saved      : $bakPath"

[System.IO.File]::WriteAllBytes($resolvedTarget, $out)
Write-Host "[OK] Signature removed : $resolvedTarget ($($out.Length) bytes, $action)"
