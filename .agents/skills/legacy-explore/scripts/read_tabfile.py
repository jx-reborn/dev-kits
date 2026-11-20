#!/usr/bin/env python3
"""Deterministic parser for legacy tab-delimited (.txt) settings tables.

Prevents column-misalignment errors on wide tables (e.g. npcs.txt, skills.txt,
magicscript.txt, tasklink_*.txt) and decodes UTF-8 / TCVN3 / GB18030 cells cleanly.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

# Import shared encoding & path-resolution helpers from sibling read_jx_file.py
sys.path.insert(0, str(Path(__file__).resolve().parent))
from read_jx_file import _decode_segment_auto, decode_tcvn3, resolve_jx_path  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")


def decode_cell(raw: bytes, encoding: str = "auto") -> str:
    if encoding == "auto":
        return _decode_segment_auto(raw)
    if encoding == "tcvn3":
        return decode_tcvn3(raw)
    return raw.decode(encoding, errors="replace")


def read_table(path: Path, encoding: str = "auto") -> tuple[list[str], list[tuple[int, list[str]]]]:
    raw_lines = path.read_bytes().splitlines()
    if not raw_lines:
        return [], []
    headers_raw = raw_lines[0].rstrip(b"\r\n").split(b"\t")
    headers = [
        decode_cell(cell, encoding).strip() or f"_col_{idx + 1}"
        for idx, cell in enumerate(headers_raw)
    ]
    rows: list[tuple[int, list[str]]] = []
    for line_no, raw in enumerate(raw_lines[1:], start=2):
        cells = [decode_cell(c, encoding) for c in raw.rstrip(b"\r\n").split(b"\t")]
        rows.append((line_no, cells))
    return headers, rows


def cmd_schema(args: argparse.Namespace) -> int:
    path = resolve_jx_path(args.path)
    if not path.is_file():
        print(f"ERROR: File not found: {path}", file=sys.stderr)
        return 1

    headers, rows = read_table(path, args.encoding)
    print(f"# Table: {path.resolve()}")
    print(f"# Total lines (TabFile rows): {len(rows) + 1} (header=row 1, data rows={len(rows)})")
    print(f"# Columns ({len(headers)}):")

    first_row = rows[0][1] if rows else []
    for idx, col in enumerate(headers, start=1):
        sample = first_row[idx - 1] if idx - 1 < len(first_row) else ""
        sample_str = f"  (row 2 sample: {sample!r})" if sample != "" else ""
        print(f"  [{idx}] {col}{sample_str}")
    return 0


def cmd_rows(args: argparse.Namespace) -> int:
    path = resolve_jx_path(args.path)
    if not path.is_file():
        print(f"ERROR: File not found: {path}", file=sys.stderr)
        return 1

    headers, rows = read_table(path, args.encoding)
    wanted_cols: list[str] | None = None
    if args.cols:
        wanted_cols = [c.strip() for c in args.cols.split(",") if c.strip()]

    where_col: str | None = None
    where_pat: re.Pattern[str] | None = None
    if args.where:
        if "=" not in args.where:
            print("ERROR: --where must be in format Column=Pattern (use *=Pattern to search any column)", file=sys.stderr)
            return 1
        where_col, expr = args.where.split("=", 1)
        where_col = where_col.strip()
        where_pat = re.compile(expr, re.IGNORECASE)

    print(f"# Table: {path.resolve()} (total lines={len(rows) + 1})")
    printed = 0
    for line_no, cells in rows:
        if line_no < args.start:
            continue
        if args.end is not None and line_no > args.end:
            break

        row_map: dict[str, str] = {}
        for idx, val in enumerate(cells):
            col_name = headers[idx] if idx < len(headers) else f"_extra_col_{idx + 1}"
            row_map[col_name] = val

        if where_pat is not None:
            if where_col == "*":
                if not any(where_pat.search(v) for v in row_map.values()):
                    continue
            else:
                target_val = row_map.get(where_col, "")
                if not where_pat.search(target_val):
                    continue

        filtered: dict[str, str] = {}
        for k, v in row_map.items():
            if wanted_cols is not None and k not in wanted_cols:
                continue
            if not args.include_empty and v == "":
                continue
            filtered[k] = v

        print(f"TabFileRow {line_no}: {json.dumps(filtered, ensure_ascii=False)}")
        printed += 1
        if printed >= args.max:
            print(f"# Reached max rows ({args.max}). Adjust --start/--end or --max.")
            break

    print(f"# Rows printed: {printed}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Inspect legacy tab-delimited (.txt) settings files accurately.")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_schema = sub.add_parser("schema", help="Show column names (1-indexed) and row 2 preview.")
    p_schema.add_argument("path", help="Path to .txt TabFile")
    p_schema.add_argument("--encoding", default="auto", help="auto | utf-8 | tcvn3 | gb18030 | cp1252")
    p_schema.set_defaults(func=cmd_schema)

    p_rows = sub.add_parser("rows", help="Query and map rows to column headers without tab-counting errors.")
    p_rows.add_argument("path", help="Path to .txt TabFile")
    p_rows.add_argument("--start", type=int, default=2, help="1-based start TabFile row / file line (default: 2)")
    p_rows.add_argument("--end", type=int, default=None, help="1-based end TabFile row / file line")
    p_rows.add_argument("--cols", default=None, help="Comma-separated column names to include")
    p_rows.add_argument("--where", default=None, help="Filter as ColName=RegexOrSubstr or *=RegexOrSubstr")
    p_rows.add_argument("--include-empty", action="store_true", help="Include empty-string columns in output")
    p_rows.add_argument("--max", type=int, default=25, help="Max rows to print (default: 25)")
    p_rows.add_argument("--encoding", default="auto", help="auto | utf-8 | tcvn3 | gb18030 | cp1252")
    p_rows.set_defaults(func=cmd_rows)

    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
