#!/usr/bin/env python3
"""Direct Ghidra analyzeHeadless wrapper for Tier 3 ELF analysis (jx_linux_y, *.so, s3relay_y).

Bypasses Windows REA P0 PE-only restriction by invoking Ghidra's native
analyzeHeadless.bat directly and caching the analyzed .gpr project under
artifacts/ghidra-legacy-server/ (gitignored).
"""

from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from read_jx_file import resolve_jx_path  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

TAG = "[JX-GHIDRA] "


def find_ghidra_headless() -> Path:
    candidates: list[Path] = []
    env_dir = os.environ.get("GHIDRA_INSTALL_DIR", "").strip()
    if env_dir:
        candidates.append(Path(env_dir).expanduser())

    for root in (Path("C:/Workspace"), Path("C:/Tools"), Path.cwd().parent):
        if root.is_dir():
            for child in sorted(root.glob("ghidra_*"), reverse=True):
                candidates.append(child)

    script_name = "analyzeHeadless.bat" if os.name == "nt" else "analyzeHeadless"
    for base in candidates:
        headless = base / "support" / script_name
        if headless.is_file():
            return headless.resolve()

    raise FileNotFoundError(
        "Could not locate Ghidra analyzeHeadless. Set GHIDRA_INSTALL_DIR to your Ghidra 12.2_PUBLIC folder."
    )


def find_repo_root() -> Path:
    here = Path(__file__).resolve()
    for parent in here.parents:
        if (parent / ".git").exists():
            return parent
    return Path.cwd().resolve()


def is_binary_imported(project_dir: Path, project_name: str, binary_name: str) -> bool:
    rep_dir = project_dir / f"{project_name}.rep"
    if not rep_dir.is_dir():
        return False
    # Ghidra stores program metadata under project.rep/idata/xx/*.prp containing the imported name.
    idata = rep_dir / "idata"
    if not idata.is_dir():
        return False
    for prp in idata.rglob("*.prp"):
        try:
            text = prp.read_text(encoding="utf-8", errors="ignore")
            if f'VALUE="{binary_name}"' in text:
                return True
        except OSError:
            continue
    return False


def run_ghidra(binary_path: Path, project_dir: Path, project_name: str, script_args: list[str]) -> int:
    headless = find_ghidra_headless()
    scripts_dir = Path(__file__).resolve().parent
    project_dir.mkdir(parents=True, exist_ok=True)

    already_imported = is_binary_imported(project_dir, project_name, binary_path.name)

    cmd = [
        str(headless),
        str(project_dir),
        project_name,
    ]
    if already_imported:
        cmd.extend(["-process", binary_path.name, "-noanalysis", "-readOnly"])
    else:
        print(f"# First-time import & auto-analysis of {binary_path.name} into {project_dir / (project_name + '.gpr')}...", flush=True)
        cmd.extend(["-import", str(binary_path)])

    cmd.extend([
        "-scriptPath", str(scripts_dir),
        "-postScript", "JxExploreGhidraQuery.java",
    ])

    env = os.environ.copy()
    if len(script_args) >= 1:
        env["JX_GHIDRA_CMD"] = script_args[0]
    if len(script_args) >= 2:
        env["JX_GHIDRA_ARG1"] = script_args[1]
    if len(script_args) >= 3:
        env["JX_GHIDRA_ARG2"] = script_args[2]
    if "JAVA_HOME" not in env or not env["JAVA_HOME"]:
        for jdk in sorted(Path("C:/Program Files/Eclipse Adoptium").glob("jdk-25*"), reverse=True):
            env["JAVA_HOME"] = str(jdk)
            break

    proc = subprocess.run(cmd, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")

    tagged_lines: list[str] = []
    for line in proc.stdout.splitlines():
        if TAG in line:
            idx = line.index(TAG)
            cleaned = line[idx + len(TAG):].rstrip()
            if cleaned.endswith("(GhidraScript)"):
                cleaned = cleaned[: -len("(GhidraScript)")].rstrip()
            tagged_lines.append(cleaned)

    if tagged_lines:
        for l in tagged_lines:
            print(l)
    else:
        print(proc.stdout)
        if proc.stderr.strip():
            print(proc.stderr, file=sys.stderr)

    return proc.returncode


def main() -> int:
    parser = argparse.ArgumentParser(description="Query/decompile legacy ELF binaries via Ghidra analyzeHeadless.")
    parser.add_argument(
        "--binary",
        default="server1/jx_linux_y",
        help="ELF binary path (absolute or relative to legacy-server, default: server1/jx_linux_y)",
    )
    parser.add_argument(
        "--project-dir",
        default=None,
        help="Ghidra project directory (default: <repo>/artifacts/ghidra-jxlinux)",
    )
    parser.add_argument("--project-name", default="JxLinuxProj", help="Ghidra project name (default: JxLinuxProj)")

    sub = parser.add_subparsers(dest="cmd", required=True)

    p_sf = sub.add_parser("search-funcs", help="Search function names by regex.")
    p_sf.add_argument("pattern", help="Java regex pattern")
    p_sf.add_argument("--max", type=int, default=50, help="Max results (default: 50)")

    p_dec = sub.add_parser("decompile", help="Decompile one function by name or 0xAddress.")
    p_dec.add_argument("target", help="Exact/partial function name or 0xHexAddress")

    p_ss = sub.add_parser("search-strings", help="Search defined strings.")
    p_ss.add_argument("query", help="Case-insensitive substring")
    p_ss.add_argument("--max", type=int, default=50, help="Max results (default: 50)")

    p_xr = sub.add_parser("xrefs", help="List incoming cross-references to function or 0xAddress.")
    p_xr.add_argument("target", help="Function name or 0xHexAddress")
    p_xr.add_argument("--max", type=int, default=50, help="Max results (default: 50)")

    args = parser.parse_args()
    binary_path = resolve_jx_path(args.binary)
    if not binary_path.is_file():
        print(f"ERROR: Binary not found: {binary_path}", file=sys.stderr)
        return 1

    project_dir = Path(args.project_dir) if args.project_dir else (find_repo_root() / "artifacts" / "ghidra-jxlinux")

    if args.cmd == "search-funcs":
        s_args = ["search-funcs", args.pattern, str(args.max)]
    elif args.cmd == "decompile":
        s_args = ["decompile", args.target]
    elif args.cmd == "search-strings":
        s_args = ["search-strings", args.query, str(args.max)]
    else:
        s_args = ["xrefs", args.target, str(args.max)]

    return run_ghidra(binary_path.resolve(), project_dir.resolve(), args.project_name, s_args)


if __name__ == "__main__":
    raise SystemExit(main())
