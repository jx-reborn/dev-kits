# JX Reborn Workbench

Private reverse-engineering reference, legacy asset extraction, and differential parity testing laboratory for JX Reborn.

> **Repository Role**: This repository pairs with public engine repo [`server`](../server) and Content Pack repo [`pack-tieudaotu`](../pack-tieudaotu).

## Layout

- `src/JXReborn.Workbench.Scripts/` — QA & parity script layer (`TestGmTokenItemScript`, automatically copied to `scripts/`).
- `tools/JXReborn.LegacyImporter/` — Legacy server/archive extractor writing canonical UTF-8 JSON catalogs and validating/compiling via `JXReborn.PackCompiler`.
- `qa/` — `JXReborn.ClientDriver` headless Win32 driver, PowerShell parity/dev-kit orchestrators, and live Oracle tests.
- `tests/JXReborn.Workbench.ParityTests/` — Offline golden wire replay (`W01..W12.capture.json`), GM Token atomicity tests, and `LegacyData` parity verification suites.
- `docs/` & `.agents/skills/` — Differential parity standards, Lua migration plan, and extraction playbooks.
- `specs/` — Archived legacy baseline and feature specifications (`001`–`028`) retained for historical reference.
- `bookmarks/` — Non-distributable symbol cross-references and RE bookmarks.
