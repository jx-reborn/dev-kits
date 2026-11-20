#!/usr/bin/env python3
"""Strip the Authenticode digital signature (IMAGE_DIRECTORY_ENTRY_SECURITY) from a PE32/PE32+ file.

Creates a .bak backup before modifying the target file, zeroes out the Security
DataDirectory entry, truncates trailing WIN_CERTIFICATE data, and recalculates
the PE Optional Header CheckSum if one was originally set.
"""

from __future__ import annotations

import argparse
import shutil
import struct
import sys
from pathlib import Path

IMAGE_DOS_SIGNATURE = 0x5A4D  # "MZ"
IMAGE_NT_SIGNATURE = 0x00004550  # "PE\0\0"
IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10B
IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20B
IMAGE_DIRECTORY_ENTRY_SECURITY = 4


def compute_pe_checksum(data: bytes | bytearray, checksum_offset: int) -> int:
    """Compute the standard Windows PE header checksum."""
    total_len = len(data)
    buf = bytearray(data)
    # Zero out the 4-byte CheckSum field before summing
    buf[checksum_offset : checksum_offset + 4] = b"\x00\x00\x00\x00"
    if len(buf) % 2 != 0:
        buf.append(0)

    checksum = 0
    for i in range(0, len(buf), 2):
        word = buf[i] | (buf[i + 1] << 8)
        checksum += word
        checksum = (checksum & 0xFFFF) + (checksum >> 16)

    checksum = (checksum & 0xFFFF) + (checksum >> 16)
    return (checksum + total_len) & 0xFFFFFFFF


def parse_pe_security_info(data: bytes | bytearray) -> dict[str, int]:
    """Locate the PE Optional Header CheckSum and Security DataDirectory offsets."""
    if len(data) < 0x40:
        raise ValueError("File is too small to be a valid PE executable.")

    dos_magic = struct.unpack_from("<H", data, 0)[0]
    if dos_magic != IMAGE_DOS_SIGNATURE:
        raise ValueError(f"Invalid DOS signature: 0x{dos_magic:04X}")

    e_lfanew = struct.unpack_from("<I", data, 0x3C)[0]
    if e_lfanew + 24 > len(data):
        raise ValueError("Invalid e_lfanew offset.")

    nt_sig = struct.unpack_from("<I", data, e_lfanew)[0]
    if nt_sig != IMAGE_NT_SIGNATURE:
        raise ValueError(f"Invalid PE signature: 0x{nt_sig:08X}")

    opt_off = e_lfanew + 24
    magic = struct.unpack_from("<H", data, opt_off)[0]
    if magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC:
        num_rva_off = opt_off + 92
        data_dir_off = opt_off + 96
    elif magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC:
        num_rva_off = opt_off + 108
        data_dir_off = opt_off + 112
    else:
        raise ValueError(f"Unsupported Optional Header magic: 0x{magic:04X}")

    checksum_off = opt_off + 64
    old_checksum = struct.unpack_from("<I", data, checksum_off)[0]
    num_rva = struct.unpack_from("<I", data, num_rva_off)[0]
    if num_rva <= IMAGE_DIRECTORY_ENTRY_SECURITY:
        raise ValueError(f"NumberOfRvaAndSizes ({num_rva}) does not include Security directory.")

    sec_dir_off = data_dir_off + IMAGE_DIRECTORY_ENTRY_SECURITY * 8
    if sec_dir_off + 8 > len(data):
        raise ValueError("Security DataDirectory entry is out of bounds.")

    cert_offset, cert_size = struct.unpack_from("<II", data, sec_dir_off)
    return {
        "magic": magic,
        "checksum_off": checksum_off,
        "old_checksum": old_checksum,
        "sec_dir_off": sec_dir_off,
        "cert_offset": cert_offset,
        "cert_size": cert_size,
    }


def make_backup(target: Path, overwrite_bak: bool = False) -> Path:
    """Create a .bak backup alongside the target file without clobbering existing backups."""
    bak_path = target.with_name(f"{target.name}.bak")
    if bak_path.exists() and not overwrite_bak:
        idx = 1
        while True:
            candidate = target.with_name(f"{target.name}.bak.{idx}")
            if not candidate.exists():
                bak_path = candidate
                break
            idx += 1

    shutil.copy2(target, bak_path)
    return bak_path


def remove_signature(target: Path, dry_run: bool = False, overwrite_bak: bool = False) -> int:
    """Remove the PE Authenticode signature from target."""
    if not target.is_file():
        print(f"[ERROR] File not found: {target}", file=sys.stderr)
        return 1

    raw = bytearray(target.read_bytes())
    info = parse_pe_security_info(raw)
    cert_offset = info["cert_offset"]
    cert_size = info["cert_size"]

    if cert_offset == 0 and cert_size == 0:
        print(f"[INFO] No Authenticode signature found in {target}.")
        return 0

    print(f"[INFO] Target          : {target}")
    print(f"[INFO] File size       : {len(raw)} bytes")
    print(f"[INFO] Security dir    : offset=0x{info['sec_dir_off']:X}")
    print(f"[INFO] Certificate tbl : offset={cert_offset} (0x{cert_offset:X}), size={cert_size} bytes")
    print(f"[INFO] Old PE CheckSum : 0x{info['old_checksum']:08X}")

    if cert_offset + cert_size > len(raw):
        print("[ERROR] Certificate table extends past EOF; refusing to modify.", file=sys.stderr)
        return 1

    # Zero out IMAGE_DIRECTORY_ENTRY_SECURITY
    struct.pack_into("<II", raw, info["sec_dir_off"], 0, 0)

    if cert_offset + cert_size == len(raw):
        del raw[cert_offset:]
        action = f"truncated trailing certificate ({cert_size} bytes)"
    else:
        raw[cert_offset : cert_offset + cert_size] = b"\x00" * cert_size
        action = f"zeroed embedded certificate ({cert_size} bytes, trailing data preserved)"

    if info["old_checksum"] != 0:
        new_checksum = compute_pe_checksum(raw, info["checksum_off"])
        struct.pack_into("<I", raw, info["checksum_off"], new_checksum)
        print(f"[INFO] New PE CheckSum : 0x{new_checksum:08X}")
    else:
        print("[INFO] New PE CheckSum : 0x00000000 (unchanged)")

    if dry_run:
        print(f"[DRY-RUN] Would create backup and write {len(raw)} bytes ({action}).")
        return 0

    bak_path = make_backup(target, overwrite_bak=overwrite_bak)
    print(f"[OK] Backup saved      : {bak_path}")

    target.write_bytes(raw)
    print(f"[OK] Signature removed : {target} ({len(raw)} bytes, {action})")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Remove an invalid/mismatched Authenticode signature from a PE executable."
    )
    parser.add_argument(
        "target",
        nargs="?",
        default=r"D:\JXOnline\Client\game.exe",
        help=r"Path to the PE executable (default: D:\JXOnline\Client\game.exe)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Inspect and report what would change without modifying any files.",
    )
    parser.add_argument(
        "--overwrite-bak",
        action="store_true",
        help="Overwrite existing <target>.bak instead of creating <target>.bak.N.",
    )
    args = parser.parse_args()
    return remove_signature(Path(args.target), dry_run=args.dry_run, overwrite_bak=args.overwrite_bak)


if __name__ == "__main__":
    raise SystemExit(main())
