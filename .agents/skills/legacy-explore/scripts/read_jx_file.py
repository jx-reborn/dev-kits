#!/usr/bin/env python3
"""Safe reader, function indexer, and multi-encoding searcher for legacy files.

Handles mixed encodings common in legacy-server (.lua, .ini, .cfg, .txt):
- UTF-8
- TCVN3 (ABC / TCVN 5712:1993 Vietnamese single-byte encoding)
- GB18030 / GBK (Simplified Chinese comments & legacy strings)
- CP1252 / Latin-1 fallback
"""

from __future__ import annotations

import argparse
import fnmatch
import os
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

TCVN3_PAIRS: list[tuple[int, str]] = [
    (0xA1, "Ă"), (0xA2, "Â"), (0xA3, "Ê"), (0xA4, "Ô"), (0xA5, "Ơ"), (0xA6, "Ư"), (0xA7, "Đ"),
    (0xA8, "ă"), (0xA9, "â"), (0xAA, "ê"), (0xAB, "ô"), (0xAC, "ơ"), (0xAD, "ư"), (0xAE, "đ"),
    (0xB5, "à"), (0xB6, "ả"), (0xB7, "ã"), (0xB8, "á"), (0xB9, "ạ"),
    (0xBB, "ằ"), (0xBC, "ẳ"), (0xBD, "ẵ"), (0xBE, "ắ"), (0xC6, "ặ"),
    (0xC7, "ầ"), (0xC8, "ẩ"), (0xC9, "ẫ"), (0xCA, "ấ"), (0xCB, "ậ"),
    (0xCC, "è"), (0xCE, "ẻ"), (0xCF, "ẽ"), (0xD0, "é"), (0xD1, "ẹ"),
    (0xD2, "ề"), (0xD3, "ể"), (0xD4, "ễ"), (0xD5, "ế"), (0xD6, "ệ"),
    (0xD7, "ì"), (0xD8, "ỉ"), (0xDC, "ĩ"), (0xDD, "í"), (0xDE, "ị"),
    (0xDF, "ò"), (0xE1, "ỏ"), (0xE2, "õ"), (0xE3, "ó"), (0xE4, "ọ"),
    (0xE5, "ồ"), (0xE6, "ổ"), (0xE7, "ỗ"), (0xE8, "ố"), (0xE9, "ộ"),
    (0xEA, "ờ"), (0xEB, "ở"), (0xEC, "ỡ"), (0xED, "ớ"), (0xEE, "ợ"),
    (0xEF, "ù"), (0xF1, "ủ"), (0xF2, "ũ"), (0xF3, "ú"), (0xF4, "ụ"),
    (0xF5, "ừ"), (0xF6, "ử"), (0xF7, "ữ"), (0xF8, "ứ"), (0xF9, "ự"),
    (0xFA, "ỳ"), (0xFB, "ỷ"), (0xFC, "ỹ"), (0xFD, "ý"), (0xFE, "ỵ"),
]
TCVN3_MAP: dict[int, str] = dict(TCVN3_PAIRS)
TCVN3_STR_TRANS: dict[int, str] = dict(TCVN3_PAIRS)
NON_TCVN3_HIGH_BYTES: bytes = bytes(b for b in range(0x80, 0x100) if b not in TCVN3_MAP)
_FOUR_HIGH_RE = re.compile(rb"[\x80-\xff]{4}")

BINARY_EXTS = {
    ".so", ".pak", ".spr", ".dat", ".db", ".db-wal", ".db-shm",
    ".obj", ".o", ".exe", ".dll", ".png", ".jpg", ".bmp", ".xls",
}


def find_jx_linux_root() -> Path | None:
    """Resolve legacy-server root via LEGACY_SERVER_DIR or sibling directory walk."""
    env_dir = os.environ.get("LEGACY_SERVER_DIR", "").strip()
    if env_dir:
        candidate = Path(env_dir).expanduser()
        if candidate.exists():
            return candidate.resolve()

    seen: set[Path] = set()
    for anchor in (Path.cwd().resolve(), Path(__file__).resolve().parent):
        for parent in (anchor, *anchor.parents):
            candidate = parent / "legacy-server"
            if candidate in seen:
                continue
            seen.add(candidate)
            if (candidate / "server1").is_dir():
                return candidate.resolve()
    return None


def resolve_jx_path(raw_path: str) -> Path:
    """Resolve an absolute, CWD-relative, or legacy-server-relative path."""
    p = Path(raw_path).expanduser()
    if p.is_absolute() or p.exists():
        return p
    jx_root = find_jx_linux_root()
    if jx_root is not None:
        candidate = jx_root / p
        if candidate.exists():
            return candidate
    return p


def decode_tcvn3(raw: bytes) -> str:
    if raw.isascii():
        return raw.decode("ascii")
    return raw.decode("latin1").translate(TCVN3_STR_TRANS)


def _decode_segment_auto(raw: bytes) -> str:
    if not raw:
        return ""
    if raw.isascii():
        return raw.decode("ascii")
    try:
        return raw.decode("utf-8")
    except UnicodeDecodeError:
        pass

    # Chinese GBK/GB18030 uses byte pairs (4+ consecutive high bytes or bytes outside TCVN3 vowel slots).
    has_non_tcvn = any(b not in TCVN3_MAP for b in raw if b >= 0x80)
    if has_non_tcvn or _FOUR_HIGH_RE.search(raw) is not None:
        try:
            return raw.decode("gb18030")
        except UnicodeDecodeError:
            pass

    return raw.decode("latin1").translate(TCVN3_STR_TRANS)


def _split_lua_comment_bytes(line: bytes) -> tuple[bytes, bytes]:
    """Split a line into (code_part, comment_part) at '--' outside string quotes."""
    if b"--" not in line or line.isascii():
        return line, b""
    in_single = False
    in_double = False
    escaped = False
    i = 0
    n = len(line)
    while i < n:
        b = line[i]
        if escaped:
            escaped = False
            i += 1
            continue
        if b == 0x5C:  # backslash
            escaped = True
            i += 1
            continue
        if b == 0x27 and not in_double:  # '
            in_single = not in_single
        elif b == 0x22 and not in_single:  # "
            in_double = not in_double
        elif b == 0x2D and not in_single and not in_double and i + 1 < n and line[i + 1] == 0x2D:
            return line[:i], line[i:]
        i += 1
    return line, b""


def decode_line(raw_line: bytes, encoding: str = "auto") -> str:
    raw_line = raw_line.rstrip(b"\r\n")
    if encoding == "auto":
        code_part, comment_part = _split_lua_comment_bytes(raw_line)
        return _decode_segment_auto(code_part) + _decode_segment_auto(comment_part)
    if encoding == "tcvn3":
        return decode_tcvn3(raw_line)
    return raw_line.decode(encoding, errors="replace")


def is_probably_binary(path: Path) -> bool:
    if path.suffix.lower() in BINARY_EXTS:
        return True
    try:
        with path.open("rb") as f:
            sample = f.read(2048)
        return b"\x00" in sample
    except OSError:
        return True


def cmd_read(args: argparse.Namespace) -> int:
    path = resolve_jx_path(args.path)
    if not path.is_file():
        print(f"ERROR: File not found: {path}", file=sys.stderr)
        return 1

    raw_lines = path.read_bytes().splitlines()
    total = len(raw_lines)
    start = max(1, args.start)
    end = min(total, args.end if args.end is not None else start + 249)

    print(f"# File: {path.resolve()} (showing lines {start}..{end} of {total}, encoding={args.encoding})")
    for idx in range(start, end + 1):
        text = decode_line(raw_lines[idx - 1], args.encoding)
        print(f"L{idx}: {text}")
    return 0


def cmd_functions(args: argparse.Namespace) -> int:
    path = resolve_jx_path(args.path)
    if not path.is_file():
        print(f"ERROR: File not found: {path}", file=sys.stderr)
        return 1

    raw_lines = path.read_bytes().splitlines()
    func_re = re.compile(r"^\s*(?:local\s+)?function\s+([A-Za-z0-9_.:]+)\s*\(([^)]*)\)")
    assign_re = re.compile(r"^\s*([A-Za-z0-9_.:]+)\s*=\s*function\s*\(([^)]*)\)")

    print(f"# Functions in {path.resolve()} ({len(raw_lines)} lines)")
    count = 0
    for idx, raw in enumerate(raw_lines, 1):
        line = decode_line(raw, args.encoding)
        m = func_re.match(line) or assign_re.match(line)
        if m:
            count += 1
            print(f"L{idx}: function {m.group(1)}({m.group(2).strip()})")
    print(f"# Total functions found: {count}")
    return 0


def iter_candidate_files(target: Path, glob_pat: str | None):
    if target.is_file():
        yield target
        return
    for root, _, files in os.walk(target):
        for name in files:
            if glob_pat and not fnmatch.fnmatch(name.lower(), glob_pat.lower()):
                continue
            full = Path(root) / name
            if not is_probably_binary(full):
                yield full


def cmd_search(args: argparse.Namespace) -> int:
    target = resolve_jx_path(args.path)
    if not target.exists():
        print(f"ERROR: Path not found: {target}", file=sys.stderr)
        return 1

    flags = re.IGNORECASE if args.ignore_case else 0
    pattern = re.compile(args.query if args.regex else re.escape(args.query), flags)

    matches = 0
    for file_path in iter_candidate_files(target, args.glob):
        try:
            raw_lines = file_path.read_bytes().splitlines()
        except OSError:
            continue
        for idx, raw in enumerate(raw_lines, 1):
            decoded = decode_line(raw, args.encoding)
            if pattern.search(decoded):
                preview = decoded if len(decoded) <= 220 else decoded[:220] + "..."
                print(f"{file_path.resolve()}:{idx}: {preview}")
                matches += 1
                if matches >= args.max:
                    print(f"# Reached max results ({args.max}). Narrow --path or --glob for more.")
                    return 0
    print(f"# Total matches: {matches}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Read and search legacy files safely across UTF-8/TCVN3/GB18030.")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_read = sub.add_parser("read", help="Read line range with automatic TCVN3/GB18030/UTF-8 decoding.")
    p_read.add_argument("path", help="Path to file")
    p_read.add_argument("--start", type=int, default=1, help="1-based start line (default: 1)")
    p_read.add_argument("--end", type=int, default=None, help="1-based end line (default: start + 249)")
    p_read.add_argument("--encoding", default="auto", help="auto | utf-8 | tcvn3 | gb18030 | cp1252")
    p_read.set_defaults(func=cmd_read)

    p_fn = sub.add_parser("functions", help="List all Lua function definitions with line numbers.")
    p_fn.add_argument("path", help="Path to .lua file")
    p_fn.add_argument("--encoding", default="auto", help="auto | utf-8 | tcvn3 | gb18030 | cp1252")
    p_fn.set_defaults(func=cmd_functions)

    p_search = sub.add_parser("search", help="Search Unicode/Vietnamese/Chinese text across legacy files.")
    p_search.add_argument("query", help="Substring or regex pattern (Unicode Vietnamese/Chinese supported)")
    p_search.add_argument("--path", required=True, help="Directory or file to search")
    p_search.add_argument("--glob", default=None, help="Filename filter, e.g. '*.lua' or '*.txt'")
    p_search.add_argument("--regex", action="store_true", help="Treat query as regular expression")
    p_search.add_argument("--ignore-case", "-i", action="store_true", help="Case-insensitive match")
    p_search.add_argument("--max", type=int, default=50, help="Maximum matches to print (default: 50)")
    p_search.add_argument("--encoding", default="auto", help="auto | utf-8 | tcvn3 | gb18030 | cp1252")
    p_search.set_defaults(func=cmd_search)

    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
