# JXReborn QA & Headless Client Harness (`qa/`)

Self-contained QA harness workspace separated from the core `JXReborn.slnx` server solution so client-driver tooling, parity scripts, and headless tests can be managed independently or split into a dedicated private repository.

## Contents

- `JXReborn.QaHarness.slnx`: Dedicated Visual Studio / `dotnet` solution covering `JXReborn.ClientDriver`, `JXReborn.ClientDriver.Cli`, `JXReborn.ClientDriver.Tests`, and referenced `../src/*` assemblies.
- `JXReborn.ClientDriver/`: Managed C# client driver (`JxHeadlessClient`, `JxHeadlessSandbox`, `Win32JobObject`) communicating over Named Pipe JSON-RPC.
- `JXReborn.ClientDriver.Cli/`: Interactive command-line driver for headless legacy client automation.
- `JXReborn.ClientDriver.Native/`: Native x86 proxy DLLs (`NullRepresent2`, `JxHeadlessAgent`) and provisioning script (`build-native.ps1`).
- `JXReborn.ClientDriver.Tests/`: Headless end-to-end and differential parity test suites (`[Trait("Category", "Headless")]`, `[Trait("Category", "DifferentialParity")]`).
- `scripts/`:
  - `run-parity.ps1`: Runs containerized legacy oracle vs `JXReborn` differential parity checks (`-Mode Live|Record|Replay`).
  - `pack-legacy-profiles.ps1` / `pack_legacy_profiles.py`: Builds clean Zero-Sim reference packages into `artifacts/packages/`.
  - `pack-dev-kit.ps1` / `restore-dev-kit.ps1` / `dev_kit_packager.py`: Packs and restores local non-Git QA prerequisites.
- `docs/headless-client-re-notes.md`: Headless client hook and memory layout notes.

## Environment Variables (`LEGACY_*`)

| Variable | Purpose |
| :--- | :--- |
| `LEGACY_CLIENT_DIR` / `LEGACY_CLIENT_SEED_DIR` | Read-only legacy client source directory for `build-native.ps1` and dev-kit scripts |
| `LEGACY_SERVER_DIR` / `LEGACY_SERVER_ARCHIVE` | Local legacy server directory or `.tar.gz` archive |
| `LEGACY_HEADLESS_TEMPLATE_DIR` / `LEGACY_HEADLESS_SANDBOX_ROOT` | Override `artifacts/headless-client` template or sandbox folder |
| `LEGACY_HEADLESS`, `LEGACY_PIPE_NAME`, `LEGACY_CRASH_DUMP_PATH`, `LEGACY_INTERACTIVE_WINDOW` | Runtime variables passed to `JxHeadlessAgent` (`winmm.dll`) |
| `LEGACY_ORACLE_HOST`, `LEGACY_ORACLE_PORT`, `LEGACY_ORACLE_PROFILE`, `LEGACY_ORACLE_CONTAINER`, `LEGACY_ORACLE_PACKAGE_SHA256`, `LEGACY_ORACLE_IMAGE`, `LEGACY_ORACLE_NEWBIE_BOOST` | Differential parity oracle settings |
| `LEGACY_PARITY_MODE`, `LEGACY_CONTAINER_CLI` | Parity execution mode (`Live`/`Record`/`Replay`) and container CLI (`wslc`/`docker`) |

## Quick Commands

```powershell
# Build QA harness solution
dotnet build qa/JXReborn.QaHarness.slnx -c Release

# Build native DLLs and provision artifacts/headless-client
powershell -NoProfile -ExecutionPolicy Bypass -File .\qa\JXReborn.ClientDriver.Native\build-native.ps1 -SourceClientDir <path-to-client>

# Run headless E2E tests (requires local artifacts/headless-client and artifacts/keys/legacy-public-keys.bin)
dotnet test qa/JXReborn.ClientDriver.Tests/JXReborn.ClientDriver.Tests.csproj --filter "Category=Headless"
```

## Extracting `qa/` Into a Separate Private Repository

When ready to split `qa/` into its own repository, run:

```powershell
git subtree split --prefix=qa -b qa-harness-export
```

Note that `qa/JXReborn.ClientDriver*.csproj` currently use relative `ProjectReference` links to `../../src/JXReborn.*`; if moved outside this monorepo checkout, adjust those references to your sibling checkout or NuGet feeds.
