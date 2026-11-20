---
name: legacy-capture-test
description: >-
  Capture real gameplay behaviors, bug reports, wire packets, or game.exe user
  actions and turn them into high-ROI, anti-tautology automated tests at the
  optimal tier: L1 Golden Wire byte-for-byte & boundary tests, L2 deterministic
  18-Hz ZoneSimulationHarness & Control-Plane DB tests, L3A TCP compliance tests,
  L3A+ Offline Wire Capture-Replay (*.capture.json via multi-actor VirtualJxClient,
  0 Docker & 0 game.exe on CI), and L3B single-run real game.exe Headless smoke
  tests. Use this skill when the user asks to capture, record, replay, or generate
  tests for a gameplay feature, bug report, packet, or legacy game.exe experience.
---

# `legacy-capture-test` — Multi-Tier Capture, Replay & Test Generation Skill

Select the **cheapest, fastest test tier that reliably catches the target defect**, enforce `JXReborn`'s anti-tautology and zero-sleep hygiene rules, and prefer **Offline Wire Capture-Replay (`L3A+`)** whenever locking real `game.exe` + `legacy` wire/state behavior so regressions run in ~1s on standard GitHub Actions (`ubuntu-latest`) without Docker or `game.exe`.

Always resolve workspace, asset, baseline, and `legacy-server` paths dynamically (`TestWorkspacePaths`, `WireCaptureStore`, `$env:LEGACY_SERVER_DIR`)—never hard-code machine-specific drive paths.

---

## 1. Step 1 — Select the Optimal Test Tier (Cost vs. Signal Matrix)

Classify the scenario into **one primary tier** (plus at most one fast `L1`/`L2` unit test when isolating a low-level formula or byte offset bug):

| Scenario / Bug Class | Target Tier & Project | Speed & Runtime Cost | Required Trait |
| :--- | :--- | :--- | :--- |
| Packet byte layout, opcode size, bit-packing, `TCVN3` encoding, malformed/truncated frames | **L1 — Golden Wire & Boundary**<br>`JXReborn.Protocol.Tests` | ~5 ms; literal `byte[]` vectors catch 1-byte drift that crashes `game.exe`. | *(none — runs in CI)* |
| Static `.pak`, JSON Schema, `jxreborn-data.pack`, catalog tables (`potion.txt`, `level_add.txt`, `npcs.txt`) | **L1 — Asset / Catalog**<br>`JXReborn.GameAssets.Tests` | ~20 ms; asserts exact literal values from `server1/settings/`. | *(none — runs in CI)* |
| Pure 18-Hz server mechanics (combat formulas, buffs/auras, NPC Lua->C# dialogs, AI, seeded RNG) | **L2 — 18-Hz Simulation**<br>`JXReborn.ZoneEngine.Tests` | ~10–50 ms via `ZoneSimulationHarness.StepFramesAsync`; 0 DB, 0 TCP. | *(none — runs in CI)* |
| Atomic persistence (`Trade`, `Guild`, `Social`), `SessionLease` fencing, EF Core queries | **L2 — Control-Plane DB**<br>`JXReborn.Application.Tests` / `Infrastructure.Tests` | ~50 ms; SQLite `:memory:` + `SqlCommandCaptureInterceptor`. | *(none — runs in CI)* |
| Handcrafted socket lifecycle, login handoff, disconnect/eviction fault isolation | **L3A — Synthetic TCP**<br>`JXReborn.Integration.Tests` | ~200 ms; `SimulatedJxClientConnection` + `GameExeProtocolVerifier`. | *(none — runs in CI)* |
| **Legacy `game.exe` player experience vs `legacy` (`C2S` action -> `S2C` timeline + 18-Hz ticks + converged state across 1 or 2 actors: equip, potions, skills, shops, storage, PK, chat, party, trade, friends)** | **L3A+ — Offline Wire Capture-Replay (DEFAULT for gameplay parity)**<br>`JXReborn.Integration.Tests/WireCaptures` | **~1 s; real encrypted TCP + Steppable 18-Hz clock (multi-actor `VirtualJxClient`), 0 Docker & 0 `game.exe` at replay!** | *(none — runs in CI `ubuntu-latest`)* |
| Inspecting `game.exe`'s own C++ client RAM, UI window state, relog DB persistence, or client sprite animation frames | **L3B — Real `game.exe` Headless Smoke**<br>`qa/JXReborn.ClientDriver.Tests/WorldJourneys` | ~10 s on Windows; spawns `game.exe` against `JXReborn` (0 Docker). | `[Trait("Category", "Headless")]` |
| High-density load, `<55.5ms` tick budget, ALC script unload | **Non-Functional**<br>`*StressTests.cs` | Excluded from routine PR runs. | `[Trait("Category", "Stress")]` |

> **Rule of Thumb (`L3A+` vs `L3B`):** If the goal is testing **`JXReborn`** to prove a real `game.exe` client receives the exact same wire packets, tick cadence, and converged player/inventory/NPC/peer state as `legacy`, **always choose `L3A+` (`WireCaptures`)** so CI executes it on every push. Use `L3B` only as a single-run `game.exe` smoke guard for client crash safety, UI rendering, or SQLite relog persistence.

---

## 2. Step 2 — Non-Negotiable Ground-Truth & Hygiene Guardrails

1. **Anti-Tautology:** Assert literal oracle values from `legacy` captures, `server1/settings/`, or `jx_linux_y`/`game.exe` disassembly (use `legacy-explore` when C++ offsets/formulas are unknown). Never assert `sut.X == ProductionConsts.X` or rely solely on `WritePayload -> TryParse` self-roundtrips.
2. **Zero Sleep & Zero Disjunctive Asserts:** Never use `Task.Delay`, `Thread.Sleep`, or `Assert.True(A || B)`. Use `StepWorldTicksAsync`, `ZoneSimulationHarness.StepFramesAsync`, or `JxHeadlessClient.WaitUntilAsync`.
3. **Architecture Compliance (`SolutionArchitectureComplianceTests`):**
   - **One Type Per File** matching the exact type name inside pluralized feature folders (never project root; never `Spec0xx*` / `Phase*` names).
   - Mark concrete test/helper classes `sealed`.
   - Integration test classes in `JXReborn.Integration.Tests` MUST end with `*IntegrationTests`, and every `[Fact]`/`[Theory]` method name MUST contain `_Should`.
4. **Bug-Report TDD (`Red -> Green`):** Record from `legacy` (or write the unit/simulation test) **first**, confirm it **FAILS** against unpatched `JXReborn`, fix `JXReborn` (never weaken captured oracle expectations to hide a server bug), and verify **GREEN**.
5. **Cryptographic Baseline Integrity:** Never hand-edit `*.capture.json` (`WireCaptureStore`); it enforces SHA-256 content hashes on load.

---

## 3. Step 3 — Authoring Workflow for `L3A+` Offline Wire Capture-Replay

### A. Separate Setup from Action via Symmetric MECE Atomic GM Token (`Ba Lăng Huyện` Map 53)
1. Keep recorded `steps[]` strictly focused on the target user action (`1–3` steps). Perform all character preparation **before** `BeginWireCaptureAsync()` by composing **MECE Atomic** paths on GM Token `(6, 1, 4258)` (no Preset Kits):
   - Teleport to **`Ba Lăng Huyện` (`Map 53`)** (`[5, 0]`) for consistent deterministic coordinates.
   - Reuse or add atomic actions symmetrically in **both**:
     - `JXReborn`: `src/JXReborn.Scripts/Items/TestGmTokenItemScript.cs`
     - `legacy`: `qa/scripts/pack_legacy_profiles.py` (`build_camnang_gm_lua`) + `qa/JXReborn.ClientDriver.Tests/DifferentialParity/TestGmTokenDriver.cs` (`TestGmTokenDriver.Path*`)
2. **Eliminate RNG & Pitfalls in Actors:**
   - Use white normal equipment (`quality = 0`, `seed = 0`, zero random magic affixes) and stationary `noRevive` training dummies (`templateId = 413` `Cọc gỗ`, `RespawnFrames = int.MaxValue`).
   - Use `accountName = "gm..."` (auto-grants GM Token when `AutoGrantGmTokenOnLogin = true`), **never** include `"GM"` inside `RoleName` (blocked by `goddess_rolename.flt` on `JXReborn`; use names like `HeroEquipW01`).
   - Remember `EquipRoom` is **`6x6`** (`0..5` × `0..5`, unlike `6x10` `RepositoryRoom`)—scan `preStep.Items` dynamically for free bag cells.

### B. Record Ground-Truth `*.capture.json` from `legacy` + `game.exe`
Choose either recording path:
- **Path 1 — Automated Recorder Test (Preferred & Repeatable):**
  1. Add `Record_Wxx_<Scenario>_ShouldPersistSignedCaptureFromLiveOracle` in `qa/JXReborn.ClientDriver.Tests/WireCaptures/OracleWireCaptureRecorderTests.cs`.
  2. Bracket only the action under test with `await client.BeginWireCaptureAsync()` and `await client.EndWireCaptureAsync()` (for 2-actor scenarios like chat/party/trade/friends, bracket both `clientA` and `clientB` and pass `observerActorId: "P2"` to `BuildExpectedS2cContract`).
  3. Symbolize dynamic IDs (`$actor:P1`, `$actor:P2`, `$item:...`, `$npc:...`) via `WireCaptureAutoSymbolizer.BuildSymbolicC2sFrames(...)` and `WireCaptureAutoSymbolizer.BuildExpectedS2cContract(..., initialMana: preStep.Player.Mana)` (passing `initialMana` filters out pre-action idle `0x46` heartbeats on `legacy`; note `0x59` medicines patch `(Place, GridX, GridY)` because `game.exe` puts `Item[]` array index at `+0x02`).
  4. Persist via `WireCaptureStore.CreateSignedDocument(...)` + `WireCaptureStore.Save(doc)` into `tests/JXReborn.Integration.Tests/WireCaptures/Baselines/<CaptureId>.capture.json`.
- **Path 2 — Interactive Manual Exploration (`game.exe` visible window):**
  ```powershell
  dotnet run --project qa/JXReborn.ClientDriver.Cli -- launch --ip 127.0.0.1 --port 15622 --interactive
  dotnet run --project qa/JXReborn.ClientDriver.Cli -- begin-capture
  # Perform the exact mouse/keyboard action inside game.exe, then dump untruncated C2S/S2C hex frames:
  dotnet run --project qa/JXReborn.ClientDriver.Cli -- end-capture --out artifacts/raw-capture.json
  ```
  Inspect the exact `C2S`/`S2C` opcodes and tick offsets in `raw-capture.json`, then codify the reproducible `Record_Wxx_*` test in `OracleWireCaptureRecorderTests.cs`.

### C. Add the Fast Offline Replay Test (`JXReborn.Integration.Tests`)
Add a `[Fact]` method in `tests/JXReborn.Integration.Tests/WireCaptures/LegacyGameExeExperienceReplayIntegrationTests.cs` calling:
```csharp
await OfflineWireCaptureReplayer.ReplayAndAssertAsync("<CaptureId>", _output);
```
`OfflineWireCaptureReplayer` boots `IntegrationTestServerHarness` with `manualZoneTicks: true`, logs in all declared `VirtualJxClient` actors over real `LegacyStreamCipher` TCP + `GameExeProtocolVerifier`, runs the identical atomic `GmMenuSelections`, patches symbolic `dwID`/grid coordinates + anti-cheat check values, steps exact 18-Hz ticks via `StepWorldTicksAsync` while draining S2C across all actors, and verifies every semantic `S2C` contract and converged actor state.

---

## 4. Step 4 — Authoring Workflow for Other Tiers (`L1` / `L2` / `L3B`)

- **L1 / L2 / L3A:** Place single-type `sealed` test classes inside existing pluralized feature folders; reuse `ZoneSimulationHarness`, `PlayerOutboundEventRecorder`, `SharedGameAssetsFixture`, and `GameExeProtocolVerifier`.
- **L3B Single-Run `game.exe` Smoke (`qa/JXReborn.ClientDriver.Tests/WorldJourneys`):** Tag with `[Trait("Category", "Headless")]`, boot `HeadlessTestServerHarness` (0 Docker), and verify real `game.exe` crash safety, UI rendering, or SQLite relog persistence.

---

## 5. Step 5 — Execute & Verify Commands

1. **Record `L3A+` Wire Capture from Live `legacy` Oracle** *(add `-Repack` only if `qa/scripts/pack_legacy_profiles.py` was modified)*:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\qa\scripts\run-parity.ps1 -Profile v1 -Mode Record -Filter "Record_Wxx" -StopAfter
   ```
2. **Run Offline `L3A+` Wire Capture Replay + SHA-256 Integrity Gate (0 Docker, 0 `game.exe`):**
   ```powershell
   dotnet test tests/JXReborn.Integration.Tests/JXReborn.Integration.Tests.csproj --filter "FullyQualifiedName~WireCaptures"
   ```
3. **Run Scoped `L1`–`L2` Regression & Architecture Compliance Gate:**
   ```powershell
   dotnet test <TargetTestProjectCsproj> --filter "FullyQualifiedName~<TestClassName>"
   dotnet test tests/JXReborn.Domain.Tests/JXReborn.Domain.Tests.csproj --filter "FullyQualifiedName~SolutionArchitectureComplianceTests"
   ```
4. **Run `L3B` Single-Run `game.exe` Smoke (0 Docker):**
   ```powershell
   dotnet test qa/JXReborn.ClientDriver.Tests/qa/JXReborn.ClientDriver.Tests.csproj --filter "Category=Headless"
   ```
