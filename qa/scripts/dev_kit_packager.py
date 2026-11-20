#!/usr/bin/env python3
"""
Portable Dev-Kit Packager & Restorer for JXReborn QA Harness.

Packs the minimal non-Git artifacts needed to bootstrap a brand-new QA machine:
  1. `jx-headless-client-seed.tar.gz` : Clean runtime DLLs/EXE, loose config/UI/script trees,
     and a single deduplicated `data/maps.pak` with all 64k+ `.spr` sprites replaced by a
     shared 53-byte valid uncompressed `SPR\\0` stub and all audio stripped (~200 MB vs 14.87 GB).
  2. `jx-linux-bin-clean.tar.gz`      : Clean `gateway/` and `server1/` from `LEGACY_SERVER_DIR`
     (excluding Logs, Backup, roleback, dulieu, *.db*, *.obj, .agents) with 0755 modes intact.
  3. `ghidra-jxclient-project.tar.gz` : Optional pre-analyzed Ghidra 12.2 project.
  4. `dev-kit-manifest.json`          : SHA-256 digests, sizes, and metadata for verification.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import struct
import sys
import tarfile
from datetime import datetime, timezone
from pathlib import Path

REQUIRED_CLIENT_BINARIES: tuple[str, ...] = (
    "game.exe",
    "engine.dll",
    "rainbow.dll",
    "lualibdll.dll",
    "zlib.dll",
)

OPTIONAL_CLIENT_INIS: tuple[str, ...] = (
    "resolution.ini",
    "serverlist.ini",
)

CLIENT_COPY_DIRS: tuple[str, ...] = (
    "settings",
    "script",
    "ui",
    "UserData",
    "lang",
)

CLIENT_EMPTY_DIRS: tuple[str, ...] = (
    "pic",
    "Login",
    "game",
    "Logs",
    "data",
)

CLEAN_CLIENT_CONFIG_INI = (
    "[Server]\r\n"
    "ServerOn=0\r\n"
    "GameServPort=5622\r\n"
    "DenialPort=5623\r\n"
    "\r\n"
    "[Client]\r\n"
    "CanOpenUrlWhenExit=0\r\n"
    "FullScreen=0\r\n"
    "Represent=2\r\n"
    "FPS=0\r\n"
    "DynamicLight=0\r\n"
    "CapPath=.\\pic\r\n"
    "KeepOutSide=1\r\n"
    "AwardPosition=-1\r\n"
    "Bil_StartAuto=0\r\n"
    "ShowMiniskill=1\r\n"
    "Plugin=0\r\n"
    "Theme=CTC\r\n"
).encode("ascii")

CLEAN_PACKAGE_INI = b"[Package]\r\nPath=\\\\data\r\n0=maps.pak\r\n"

# Valid 53-byte uncompressed 32x32 1-frame SPR\0 stub (no palette color table bytes;
# palette=0 colors, 1 frame offset table entry at frame offset 0, 1x1 span header).
DUMMY_SPR_BLOB: bytes = (
    struct.pack("<4s8H12x", b"SPR\x00", 32, 32, 0, 0, 1, 1, 1, 1)
    + b"\x00\x00\x00"
    + struct.pack("<II", 0, 10)
    + struct.pack("<HHhhBB", 1, 1, 0, 0, 1, 0)
)
assert len(DUMMY_SPR_BLOB) == 53

LINUX_DROPPED_PREFIXES: tuple[str, ...] = (
    "backup/",
    "gateway/Logs/",
    "gateway/Backup/",
    "gateway/roleback/",
    "server1/Logs/",
    "server1/dulieu/",
    ".agents/",
)

LINUX_DROPPED_EXACT: set[str] = {
    "gateway/bishop_livetime.dat",
    "gateway/statdata.dat",
}

LINUX_DROPPED_SUFFIXES: tuple[str, ...] = (
    ".db",
    ".db-shm",
    ".db-wal",
    ".obj",
    ".pyc",
)

LINUX_REQUIRED_EMPTY_DIRS: tuple[str, ...] = (
    "gateway/Logs",
    "gateway/Backup",
    "gateway/roleback",
    "server1/Logs",
    "server1/dulieu",
)

LINUX_EXECUTABLE_REL_PATHS: set[str] = {
    "gateway/goddess_y",
    "gateway/bishop_y",
    "gateway/backupdaemon",
    "gateway/s3relay/s3relay_y",
    "gateway/libfiltertext.so",
    "gateway/libgoddesscfg.so",
    "gateway/libheaven.so",
    "gateway/librainbow.so",
    "server1/jx_linux_y",
    "server1/libheaven.so",
    "server1/librainbow.so",
}

FIXED_MTIME = 1730000000


def segment_to_gbk_bytes(seg: str) -> bytes:
    """Recovers raw GBK/CP936 archive bytes from a Windows NTFS filename segment."""
    if all(ord(c) < 128 for c in seg):
        return seg.encode("ascii")
    try:
        b1 = seg.encode("cp1252")
    except UnicodeEncodeError:
        try:
            b1 = seg.encode("latin1")
        except UnicodeEncodeError:
            return seg.encode("gbk", errors="replace")

    try:
        u = b1.decode("utf-8")
        if all(ord(ch) <= 0xFF for ch in u):
            return u.encode("latin1")
    except UnicodeDecodeError:
        pass

    return b1


def hash_pak_path(raw_path_bytes: bytes) -> int:
    """Computes legacy `g_FileName2Id` 32-bit hash matching `PakFilePathHasher.cs`."""
    h = 0
    for i, c in enumerate(raw_path_bytes):
        if c == 0:
            break
        if 65 <= c <= 90:
            c += 32
        elif c == 47:
            c = 92
        sc = c - 256 if c >= 128 else c
        h = (((h + (i + 1) * sc) & 0xFFFFFFFF) % 0x8000000B * 0xFFFFFFEF) & 0xFFFFFFFF
    return (h ^ 0x12345678) & 0xFFFFFFFF


def parse_package_ini(source_client_dir: Path) -> tuple[Path, list[str]]:
    pkg_path = source_client_dir / "package.ini"
    if not pkg_path.is_file():
        raise FileNotFoundError(f"Missing package.ini in {source_client_dir}")

    pak_subdir = "data"
    ordered: list[tuple[int, str]] = []
    for raw_line in pkg_path.read_text(encoding="latin1", errors="replace").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("[") or line.startswith(";") or line.startswith("#"):
            continue
        if "=" not in line:
            continue
        key, val = line.split("=", 1)
        key_clean = key.strip()
        val_clean = val.strip()
        if key_clean.lower() == "path" and val_clean:
            pak_subdir = val_clean.lstrip("\\/") or "data"
        elif key_clean.isdigit() and val_clean:
            ordered.append((int(key_clean), val_clean))

    pak_dir = source_client_dir / pak_subdir
    if not pak_dir.is_dir():
        for fallback in ("data", "data1"):
            candidate = source_client_dir / fallback
            if candidate.is_dir() and (candidate / "maps.pak").is_file():
                pak_dir = candidate
                break
    if not pak_dir.is_dir():
        raise FileNotFoundError(f"Package data directory not found under {source_client_dir}")

    ordered.sort(key=lambda item: item[0])
    return pak_dir, [name for _, name in ordered]


def build_merged_headless_pak(source_client_dir: Path, out_pak_path: Path) -> dict[str, int]:
    pak_dir, ordered_paks = parse_package_ini(source_client_dir)
    out_pak_path.parent.mkdir(parents=True, exist_ok=True)

    seen_ids: set[int] = set()
    index_entries: list[tuple[int, int, int, int]] = []
    kept_entries = 0
    stubbed_pak_sprites = 0
    stubbed_loose_sprites = 0
    stripped_audio_entries = 0

    with out_pak_path.open("wb") as f_out:
        f_out.write(b"\x00" * 32)
        f_out.write(DUMMY_SPR_BLOB)
        cur_offset = 32 + len(DUMMY_SPR_BLOB)

        for pak_name in ordered_paks:
            pak_file = pak_dir / pak_name
            if not pak_file.is_file():
                continue

            is_sound_pak = pak_name.lower() == "sound.pak"
            with pak_file.open("rb") as f_in:
                hdr = f_in.read(32)
                if len(hdr) < 32 or hdr[:4] != b"PACK":
                    continue
                count, idx_off, _ = struct.unpack("<III", hdr[4:16])
                f_in.seek(idx_off)
                idx_bytes = f_in.read(count * 16)
                actual_count = len(idx_bytes) // 16

                for i in range(actual_count):
                    fid, off, usize, cflag = struct.unpack_from("<IIII", idx_bytes, i * 16)
                    if fid in seen_ids:
                        continue
                    seen_ids.add(fid)

                    csize = cflag & 0x00FFFFFF
                    flag = (cflag >> 24) & 0xFF
                    if csize > 0:
                        f_in.seek(off)
                        head = f_in.read(min(16, csize))
                    else:
                        head = b""

                    is_spr = (
                        ((flag & 0x10) != 0)
                        or head[:4] == b"SPR\x00"
                        or head[1:5] == b"SPR\x00"
                    )
                    is_aud = (
                        is_sound_pak
                        or head[:4] in (b"RIFF", b"OggS")
                        or head[1:5] in (b"RIFF", b"OggS")
                        or head[:3] == b"ID3"
                        or head[1:4] == b"ID3"
                    )

                    if is_spr:
                        index_entries.append((fid, 32, len(DUMMY_SPR_BLOB), len(DUMMY_SPR_BLOB)))
                        stubbed_pak_sprites += 1
                    elif is_aud:
                        stripped_audio_entries += 1
                    else:
                        if csize > 0:
                            f_in.seek(off)
                            remaining = csize
                            while remaining > 0:
                                chunk = f_in.read(min(1048576, remaining))
                                if not chunk:
                                    raise EOFError(
                                        f"Unexpected EOF reading entry 0x{fid:08x} from {pak_file}"
                                    )
                                f_out.write(chunk)
                                remaining -= len(chunk)
                        index_entries.append((fid, cur_offset, usize, cflag))
                        cur_offset += csize
                        kept_entries += 1

        spr_dir = source_client_dir / "spr"
        if spr_dir.is_dir():
            for root, _, files in os.walk(spr_dir):
                rel_dir = Path(root).relative_to(spr_dir)
                dir_parts = [segment_to_gbk_bytes(p) for p in rel_dir.parts]
                for fname in files:
                    if not fname.lower().endswith(".spr"):
                        continue
                    raw_path = b"\\spr\\" + b"\\".join([*dir_parts, segment_to_gbk_bytes(fname)])
                    fid = hash_pak_path(raw_path)
                    if fid not in seen_ids:
                        seen_ids.add(fid)
                        index_entries.append((fid, 32, len(DUMMY_SPR_BLOB), len(DUMMY_SPR_BLOB)))
                        stubbed_loose_sprites += 1

        index_entries.sort(key=lambda item: item[0])
        index_offset = cur_offset
        if index_offset + len(index_entries) * 16 >= 0xFFFFFFFF:
            raise OverflowError(
                f"Merged maps.pak exceeds 32-bit XPack offset limit ({index_offset} bytes)."
            )

        for entry in index_entries:
            f_out.write(struct.pack("<IIII", *entry))

        f_out.seek(0)
        f_out.write(
            struct.pack("<4sIIIIIII", b"PACK", len(index_entries), index_offset, 32, 0, 0, 0, 0)
        )

    return {
        "pakBytes": out_pak_path.stat().st_size,
        "totalEntries": len(index_entries),
        "keptEntries": kept_entries,
        "stubbedPakSprites": stubbed_pak_sprites,
        "stubbedLooseSprites": stubbed_loose_sprites,
        "strippedAudioEntries": stripped_audio_entries,
    }


def add_dir_entry(tf_out: tarfile.TarFile, arcname: str, mtime: int = FIXED_MTIME) -> None:
    ti = tarfile.TarInfo(name=arcname.rstrip("/"))
    ti.type = tarfile.DIRTYPE
    ti.mode = 0o755
    ti.mtime = mtime
    tf_out.addfile(ti)


def add_bytes_entry(
    tf_out: tarfile.TarFile,
    arcname: str,
    payload: bytes,
    mode: int = 0o644,
    mtime: int = FIXED_MTIME,
) -> None:
    ti = tarfile.TarInfo(name=arcname)
    ti.type = tarfile.REGTYPE
    ti.size = len(payload)
    ti.mode = mode
    ti.mtime = mtime
    tf_out.addfile(ti, io_bytes(payload))


def io_bytes(payload: bytes):
    import io

    return io.BytesIO(payload)


def add_file_entry(
    tf_out: tarfile.TarFile,
    src_path: Path,
    arcname: str,
    mode: int | None = None,
) -> None:
    st = src_path.stat()
    ti = tarfile.TarInfo(name=arcname)
    ti.type = tarfile.REGTYPE
    ti.size = st.st_size
    ti.mode = mode if mode is not None else 0o644
    ti.mtime = int(st.st_mtime)
    with src_path.open("rb") as f_src:
        tf_out.addfile(ti, f_src)


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        while chunk := f.read(1048576):
            h.update(chunk)
    return h.hexdigest()


def pack_client_seed(
    source_client_dir: Path,
    output_dir: Path,
    prebuilt_headless_dir: Path | None = None,
) -> dict:
    for fname in REQUIRED_CLIENT_BINARIES:
        if not (source_client_dir / fname).is_file():
            raise FileNotFoundError(f"Required client file missing: {source_client_dir / fname}")

    filter_orig = source_client_dir / "filtertext_orig.dll"
    filter_fallback = source_client_dir / "filtertext.dll"
    if not filter_orig.is_file() and not filter_fallback.is_file():
        raise FileNotFoundError(f"Neither filtertext_orig.dll nor filtertext.dll found in {source_client_dir}")

    temp_pak = output_dir / ".tmp_merged_maps.pak"
    archive_path = output_dir / "jx-headless-client-seed.tar.gz"
    tmp_archive = archive_path.with_suffix(".tmp.tar.gz")
    has_prebuilt_dlls = (
        prebuilt_headless_dir is not None
        and (prebuilt_headless_dir / "represent2.dll").is_file()
        and (prebuilt_headless_dir / "winmm.dll").is_file()
    )

    try:
        print(f"[DEV-KIT] Building deduplicated sprite-stubbed maps.pak from '{source_client_dir}'...")
        pak_stats = build_merged_headless_pak(source_client_dir, temp_pak)
        print(
            f"          maps.pak = {pak_stats['pakBytes'] / (1024 * 1024):.2f} MB | "
            f"kept={pak_stats['keptEntries']}, stubbed_spr={pak_stats['stubbedPakSprites'] + pak_stats['stubbedLooseSprites']}, "
            f"stripped_audio={pak_stats['strippedAudioEntries']}"
        )

        print(f"[DEV-KIT] Writing '{archive_path.name}'...")
        file_count = 0
        with tmp_archive.open("wb") as raw_out:
            with tarfile.open(
                fileobj=raw_out,
                mode="w:gz",
                format=tarfile.PAX_FORMAT,
                encoding="utf-8",
                compresslevel=3,
            ) as tf_out:
                for fname in REQUIRED_CLIENT_BINARIES:
                    src_bin = source_client_dir / fname
                    if (
                        fname == "game.exe"
                        and prebuilt_headless_dir is not None
                        and (prebuilt_headless_dir / "game.exe").is_file()
                    ):
                        src_bin = prebuilt_headless_dir / "game.exe"
                    add_file_entry(tf_out, src_bin, fname)
                    file_count += 1

                if has_prebuilt_dlls:
                    assert prebuilt_headless_dir is not None
                    add_dir_entry(tf_out, "prebuilt")
                    add_file_entry(tf_out, prebuilt_headless_dir / "represent2.dll", "prebuilt/represent2.dll")
                    add_file_entry(tf_out, prebuilt_headless_dir / "winmm.dll", "prebuilt/winmm.dll")
                    file_count += 2

                if filter_orig.is_file():
                    add_file_entry(tf_out, filter_orig, "filtertext_orig.dll")
                    add_file_entry(tf_out, filter_orig, "filtertext.dll")
                    file_count += 2
                else:
                    add_file_entry(tf_out, filter_fallback, "filtertext.dll")
                    file_count += 1

                for opt_ini in OPTIONAL_CLIENT_INIS:
                    p = source_client_dir / opt_ini
                    if p.is_file():
                        add_file_entry(tf_out, p, opt_ini)
                        file_count += 1

                add_bytes_entry(tf_out, "package.ini", CLEAN_PACKAGE_INI)
                add_bytes_entry(tf_out, "config.ini", CLEAN_CLIENT_CONFIG_INI)
                file_count += 2

                for d in CLIENT_EMPTY_DIRS:
                    add_dir_entry(tf_out, d)

                add_file_entry(tf_out, temp_pak, "data/maps.pak")
                file_count += 1

                for rel_dir in CLIENT_COPY_DIRS:
                    src_sub = source_client_dir / rel_dir
                    if not src_sub.is_dir():
                        continue
                    add_dir_entry(tf_out, rel_dir)
                    for root, dirs, files in os.walk(src_sub):
                        dirs.sort()
                        rel_root = Path(root).relative_to(source_client_dir).as_posix()
                        if rel_root != rel_dir:
                            add_dir_entry(tf_out, rel_root)
                        for fn in sorted(files):
                            if fn.lower().endswith(".tmp"):
                                continue
                            fp = Path(root) / fn
                            add_file_entry(tf_out, fp, f"{rel_root}/{fn}")
                            file_count += 1

        tmp_archive.replace(archive_path)
    finally:
        if temp_pak.exists():
            temp_pak.unlink()
        if tmp_archive.exists():
            tmp_archive.unlink()

    return {
        "file": archive_path.name,
        "bytes": archive_path.stat().st_size,
        "sha256": sha256_file(archive_path),
        "fileCount": file_count,
        "prebuiltNativeDlls": has_prebuilt_dlls,
        "mergedPak": pak_stats,
    }


def should_skip_linux_rel(rel: str) -> bool:
    if rel in LINUX_DROPPED_EXACT:
        return True
    rel_slash = rel.rstrip("/") + "/"
    for prefix in LINUX_DROPPED_PREFIXES:
        if rel == prefix.rstrip("/") or rel.startswith(prefix) or rel_slash.startswith(prefix):
            return True
    if any(rel.lower().endswith(sfx) for sfx in LINUX_DROPPED_SUFFIXES):
        return True
    return False


def pack_linux_bin(source_linux_dir: Path, output_dir: Path) -> dict:
    for sub in ("gateway", "server1"):
        if not (source_linux_dir / sub).is_dir():
            raise FileNotFoundError(f"Missing required jx-linux-bin directory: {source_linux_dir / sub}")

    archive_path = output_dir / "jx-linux-bin-clean.tar.gz"
    tmp_archive = archive_path.with_suffix(".tmp.tar.gz")
    print(f"[DEV-KIT] Packing clean jx-linux-bin from '{source_linux_dir}' -> '{archive_path.name}'...")

    file_count = 0
    written_dirs: set[str] = set()

    try:
        with tmp_archive.open("wb") as raw_out:
            with tarfile.open(
                fileobj=raw_out,
                mode="w:gz",
                format=tarfile.PAX_FORMAT,
                encoding="utf-8",
                compresslevel=6,
            ) as tf_out:
                version_file = source_linux_dir / "VERSION"
                if version_file.is_file():
                    add_file_entry(tf_out, version_file, "VERSION")
                    file_count += 1

                for sub_root in ("gateway", "server1"):
                    base = source_linux_dir / sub_root
                    for root, dirs, files in os.walk(base):
                        rel_dir = Path(root).relative_to(source_linux_dir).as_posix()
                        if should_skip_linux_rel(rel_dir):
                            dirs[:] = []
                            continue
                        dirs[:] = sorted(d for d in dirs if not should_skip_linux_rel(f"{rel_dir}/{d}"))
                        add_dir_entry(tf_out, rel_dir)
                        written_dirs.add(rel_dir)

                        for fn in sorted(files):
                            rel_file = f"{rel_dir}/{fn}"
                            if should_skip_linux_rel(rel_file):
                                continue
                            mode = 0o755 if rel_file in LINUX_EXECUTABLE_REL_PATHS else 0o644
                            add_file_entry(tf_out, Path(root) / fn, rel_file, mode=mode)
                            file_count += 1

                for empty_dir in LINUX_REQUIRED_EMPTY_DIRS:
                    if empty_dir not in written_dirs:
                        add_dir_entry(tf_out, empty_dir)

        tmp_archive.replace(archive_path)
    finally:
        if tmp_archive.exists():
            tmp_archive.unlink()

    return {
        "file": archive_path.name,
        "bytes": archive_path.stat().st_size,
        "sha256": sha256_file(archive_path),
        "fileCount": file_count,
    }


def pack_ghidra_project(
    ghidra_dir: Path,
    output_dir: Path,
    archive_name: str = "ghidra-jxclient-project.tar.gz",
) -> dict:
    if not ghidra_dir.is_dir():
        raise FileNotFoundError(f"Ghidra project directory not found: {ghidra_dir}")

    archive_path = output_dir / archive_name
    tmp_archive = archive_path.with_suffix(".tmp.tar.gz")
    print(f"[DEV-KIT] Packing Ghidra project from '{ghidra_dir}' -> '{archive_path.name}'...")

    file_count = 0
    try:
        with tmp_archive.open("wb") as raw_out:
            with tarfile.open(
                fileobj=raw_out,
                mode="w:gz",
                format=tarfile.PAX_FORMAT,
                encoding="utf-8",
                compresslevel=6,
            ) as tf_out:
                for root, dirs, files in os.walk(ghidra_dir):
                    dirs.sort()
                    rel_dir = Path(root).relative_to(ghidra_dir).as_posix()
                    if rel_dir != ".":
                        add_dir_entry(tf_out, rel_dir)
                    for fn in sorted(files):
                        if fn.endswith(".lock") or fn.endswith(".lock~"):
                            continue
                        rel_file = fn if rel_dir == "." else f"{rel_dir}/{fn}"
                        add_file_entry(tf_out, Path(root) / fn, rel_file)
                        file_count += 1

        tmp_archive.replace(archive_path)
    finally:
        if tmp_archive.exists():
            tmp_archive.unlink()

    return {
        "file": archive_path.name,
        "bytes": archive_path.stat().st_size,
        "sha256": sha256_file(archive_path),
        "fileCount": file_count,
    }


def safe_extract_tar(archive_path: Path, dest_dir: Path) -> int:
    dest_root = dest_dir.resolve()
    dest_root.mkdir(parents=True, exist_ok=True)
    extracted = 0

    with tarfile.open(archive_path, "r:gz", encoding="utf-8") as tf_in:
        for member in tf_in.getmembers():
            rel_name = member.name.lstrip("/")
            if not rel_name or rel_name == ".":
                continue
            target_path = (dest_root / rel_name).resolve()
            if not target_path.is_relative_to(dest_root):
                raise ValueError(f"Unsafe archive entry path rejected: {member.name}")
            if member.issym() or member.islnk() or member.isdev():
                raise ValueError(f"Unsupported link/device archive entry rejected: {member.name}")

            if member.isdir():
                target_path.mkdir(parents=True, exist_ok=True)
            elif member.isfile():
                target_path.parent.mkdir(parents=True, exist_ok=True)
                src_stream = tf_in.extractfile(member)
                if src_stream is None:
                    continue
                if target_path.exists():
                    try:
                        os.chmod(target_path, 0o666)
                    except OSError:
                        pass
                with src_stream, target_path.open("wb") as dst_stream:
                    shutil.copyfileobj(src_stream, dst_stream, length=1048576)
                extracted += 1

    return extracted


def verify_artifact(bundle_dir: Path, entry: dict) -> Path:
    archive_path = bundle_dir / entry["file"]
    if not archive_path.is_file():
        raise FileNotFoundError(f"Missing bundle archive: {archive_path}")

    actual_bytes = archive_path.stat().st_size
    if actual_bytes != entry["bytes"]:
        raise ValueError(
            f"Size mismatch for {archive_path.name}: expected {entry['bytes']}, got {actual_bytes}"
        )

    actual_sha = sha256_file(archive_path)
    if actual_sha.lower() != entry["sha256"].lower():
        raise ValueError(
            f"SHA-256 mismatch for {archive_path.name}: expected {entry['sha256']}, got {actual_sha}"
        )

    return archive_path


def cmd_pack(args: argparse.Namespace) -> int:
    output_dir = Path(args.output_dir).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    manifest: dict = {
        "schemaVersion": 1,
        "generatedAtUtc": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "artifacts": {},
    }

    prebuilt_dir = Path(args.prebuilt_headless_dir).resolve() if args.prebuilt_headless_dir else None
    client_meta = pack_client_seed(
        Path(args.source_client_dir).resolve(),
        output_dir,
        prebuilt_headless_dir=prebuilt_dir,
    )
    manifest["artifacts"]["clientSeed"] = client_meta

    if not args.client_only:
        linux_meta = pack_linux_bin(Path(args.source_linux_dir).resolve(), output_dir)
        manifest["artifacts"]["linuxBin"] = linux_meta

    if args.include_ghidra_project:
        ghidra_meta = pack_ghidra_project(
            Path(args.ghidra_project_dir).resolve(),
            output_dir,
            archive_name="ghidra-jxclient-project.tar.gz",
        )
        manifest["artifacts"]["ghidraProject"] = ghidra_meta
        if args.ghidra_linux_project_dir:
            linux_ghidra_dir = Path(args.ghidra_linux_project_dir).resolve()
            if linux_ghidra_dir.is_dir():
                manifest["artifacts"]["ghidraLinuxProject"] = pack_ghidra_project(
                    linux_ghidra_dir,
                    output_dir,
                    archive_name="ghidra-jxlinux-project.tar.gz",
                )

    manifest_path = output_dir / "dev-kit-manifest.json"
    if manifest_path.is_file():
        try:
            prev_artifacts = json.loads(manifest_path.read_text(encoding="utf-8")).get("artifacts", {})
            for optional_key in ("linuxBin", "ghidraProject", "ghidraLinuxProject"):
                if optional_key not in manifest["artifacts"] and optional_key in prev_artifacts:
                    try:
                        verify_artifact(output_dir, prev_artifacts[optional_key])
                        manifest["artifacts"][optional_key] = prev_artifacts[optional_key]
                    except (FileNotFoundError, ValueError, KeyError):
                        pass
        except (OSError, json.JSONDecodeError):
            pass

    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")

    print(f"[DEV-KIT] Manifest written -> {manifest_path}")
    for key, info in manifest["artifacts"].items():
        print(f"  - {key:14s}: {info['file']} ({info['bytes'] / (1024 * 1024):.2f} MB) sha256={info['sha256'][:16]}...")
    return 0


def _extract_optional_archive(
    bundle_dir: Path,
    filename: str,
    dest_raw: str,
    label: str,
    force: bool,
    manifest_entry: dict | None = None,
) -> None:
    if not dest_raw:
        return
    candidate = bundle_dir / filename
    if not candidate.is_file():
        if manifest_entry is not None:
            print(
                f"[RESTORE] Optional archive '{filename}' not found in bundle directory; skipping."
            )
        return

    dest = Path(dest_raw).resolve()
    if dest.is_dir() and any(dest.iterdir()) and not force:
        print(
            f"[RESTORE] Skipping {label} extraction because '{dest}' already exists "
            "(pass -Force to overwrite)."
        )
        return

    if manifest_entry is not None:
        candidate = verify_artifact(bundle_dir, manifest_entry)
        print(f"[RESTORE] SHA-256 OK: {candidate.name} -> '{dest}'")
    else:
        print(f"[RESTORE] Extracting {candidate.name} -> '{dest}'")

    extracted = safe_extract_tar(candidate, dest)
    print(f"          Extracted {extracted} files.")


def cmd_unpack(args: argparse.Namespace) -> int:
    bundle_dir = Path(args.bundle_dir).resolve()
    manifest_path = bundle_dir / "dev-kit-manifest.json"
    client_dest = Path(args.client_seed_dir).resolve()

    if manifest_path.is_file():
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        artifacts = manifest.get("artifacts", {})
        if "clientSeed" not in artifacts:
            raise ValueError("Manifest is missing required 'clientSeed' artifact.")

        client_archive = verify_artifact(bundle_dir, artifacts["clientSeed"])
        print(f"[RESTORE] SHA-256 OK: {client_archive.name} -> '{client_dest}'")
        extracted_client = safe_extract_tar(client_archive, client_dest)
        print(f"          Extracted {extracted_client} files.")

        if not args.client_only:
            optional_targets = (
                ("linuxBin", args.linux_bin_dir, "jx-linux-bin"),
                ("ghidraProject", args.ghidra_project_dir, "Ghidra project"),
                ("ghidraLinuxProject", args.ghidra_linux_project_dir, "Ghidra Linux project"),
            )
            for key, dest_raw, label in optional_targets:
                if key in artifacts:
                    entry = artifacts[key]
                    _extract_optional_archive(
                        bundle_dir, entry["file"], dest_raw, label, args.force, manifest_entry=entry
                    )
        return 0

    client_archive = bundle_dir / "jx-headless-client-seed.tar.gz"
    if not client_archive.is_file():
        raise FileNotFoundError(
            f"Missing 'jx-headless-client-seed.tar.gz' in '{bundle_dir}'. "
            "Place it in artifacts\\dev-kit or pass -DownloadRelease dev-kit-v1."
        )

    print(
        "[RESTORE] Notice: dev-kit-manifest.json not found; extracting present .tar.gz "
        "archives without SHA-256 manifest check."
    )
    print(f"[RESTORE] Extracting {client_archive.name} -> '{client_dest}'")
    extracted_client = safe_extract_tar(client_archive, client_dest)
    print(f"          Extracted {extracted_client} files.")

    if not args.client_only:
        optional_files = (
            ("jx-linux-bin-clean.tar.gz", args.linux_bin_dir, "jx-linux-bin"),
            ("ghidra-jxclient-project.tar.gz", args.ghidra_project_dir, "Ghidra project"),
            ("ghidra-jxlinux-project.tar.gz", args.ghidra_linux_project_dir, "Ghidra Linux project"),
        )
        for filename, dest_raw, label in optional_files:
            _extract_optional_archive(
                bundle_dir, filename, dest_raw, label, args.force, manifest_entry=None
            )

    return 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="JXReborn Portable Dev-Kit Packager & Restorer")
    sub = parser.add_subparsers(dest="command", required=True)

    p_pack = sub.add_parser("pack", help="Build portable dev-kit archives and SHA-256 manifest")
    p_pack.add_argument("--output-dir", required=True)
    p_pack.add_argument("--source-client-dir", required=True)
    p_pack.add_argument("--source-linux-dir", required=False, default="")
    p_pack.add_argument("--prebuilt-headless-dir", required=False, default="")
    p_pack.add_argument("--ghidra-project-dir", required=False, default="")
    p_pack.add_argument("--ghidra-linux-project-dir", required=False, default="")
    p_pack.add_argument("--client-only", action="store_true")
    p_pack.add_argument("--include-ghidra-project", action="store_true")

    p_unpack = sub.add_parser("unpack", help="Verify SHA-256 digests and extract dev-kit archives")
    p_unpack.add_argument("--bundle-dir", required=True)
    p_unpack.add_argument("--client-seed-dir", required=True)
    p_unpack.add_argument("--linux-bin-dir", required=False, default="")
    p_unpack.add_argument("--ghidra-project-dir", required=False, default="")
    p_unpack.add_argument("--ghidra-linux-project-dir", required=False, default="")
    p_unpack.add_argument("--client-only", action="store_true")
    p_unpack.add_argument("--force", action="store_true")

    parsed = parser.parse_args(argv)
    if parsed.command == "pack":
        return cmd_pack(parsed)
    return cmd_unpack(parsed)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
