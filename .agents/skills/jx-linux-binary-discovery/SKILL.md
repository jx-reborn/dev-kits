---
name: jx-linux-binary-discovery
description: >-
  Autonomously decompile and audit jx-linux binaries in a continuous loop to
  extract MECE, atomic game mechanics, exact formulas, struct offsets, and
  binary evidence into C# pure static functions, implementing strictly what is
  verified from binaries and discovering unexplored related domains until the
  frontier is exhausted.
---

# JX-Linux Binary Atomic Discovery Skill (C# Pure Static Output)

Systematically reverse-engineer `jx-linux` ELF binaries (`server1/jx_linux_y`, `server1/*.so`, `gateway/s3relay/s3relay_y`, `bishop_y`, `goddess_y`) into **MECE, atomic C# pure static functions** using an **autonomous continuous discovery loop**.

---

## 0. Hard Constraints (Violation = Invalid Output)

1. **ZERO PRIOR MEMORY**:
   - Do **NOT** use any pre-existing knowledge of Võ Lâm Truyền Kỳ, JX1, or JX-Linux.
   - Treat every mechanic, formula, constant, enum, and state variable as completely unknown until observed in decompiled output.
2. **ZERO GUESSING & STRICT PARTIALITY ("Đọc hiểu đến đâu, làm đến đó")**:
   - Never infer or extrapolate variable meanings, constants, clamp bounds, rounding rules, or behavior of un-decompiled functions.
   - Only write C# code for logic, branches, and formulas **directly observed** in decompiled pseudo-C / assembly.
   - Copy integer-division truncation, bitshifts, signed/unsigned casts, overflow guards, and `if/else/switch` conditions **verbatim**.
   - For any un-decompiled `callee`, vtable slot, or untraced offset: **do NOT stub with guessed values**. Pass it as an explicit delegate/input parameter or mark `// UNRESOLVED_BINARY_REF: <symbol> @ <0xAddr>` (throwing `NotImplementedException` only if that exact unverified branch is reached, or isolating it as an explicit dependency) and push it to `pending`.
3. **BINARY-ONLY GROUND TRUTH**:
   - All facts must come directly from `jx-linux` binaries queried via the existing `legacy-explore` Ghidra script:
     ```powershell
     python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary <bin> search-funcs "<regex>"
     python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary <bin> search-strings "<substring>"
     python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary <bin> decompile <name_or_0xAddr>
     python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary <bin> xrefs <name_or_0xAddr>
     ```
   - Every C# method and formula MUST carry XML doc-comments / inline comments citing `binary`, `function`, and `0xAddress`.

---

## 1. Workspace & State Files

Store discovery state and generated C# files under a dedicated output directory (default: `artifacts/binary-discovery/`):

- **`discovery_registry.json`**: The single source of truth for MECE deduplication and loop control (`pending`, `in_progress`, `done`, `split`).
- **`<Domain>/<AtomicClass>.cs`**: One C# file per atomic unit (or cohesive pure-static class), conforming to [csharp-atomic-template.md](./references/csharp-atomic-template.md).

---

## 2. MECE, Atomic & Pure Static C# Standards

- **Pure Static Functions**:
  - Use `public static class` and `public static` methods with **no hidden mutable static/global state**.
  - Model inputs/outputs as explicit `readonly record struct` / `struct` / `ref` parameters matching the exact binary fields/offsets observed.
  - External callees not yet decompiled (or already extracted as separate atomic functions) must be either called directly (if already `done`) or injected via `Func<...>` / explicit parameter—never faked inline.
- **Atomic**: Each C# file/function covers **exactly one** independent mechanism, formula, packet handler, or state transition.
  - If a decompiled function implements multiple independent mechanisms, **split** them into separate atomic items in `pending`, mark the coarse item `split`, and process one atomic item per iteration.
- **MECE**:
  - **Mutually Exclusive**: Before starting or adding any item to `pending`, check `done`, `split`, and `pending` in `discovery_registry.json` (by both `atomic_id` and primary binary address `0x...`). Never duplicate an already-covered mechanism; call/reference the existing `done` static function instead.
  - **Collectively Exhaustive**: Every branch (`if`, `else`, `switch`, early `return`), edge case, clamp, and side effect observed in the decompiled code must be represented in the C# function (or explicitly marked `UNRESOLVED_BINARY_REF` if a callee is not yet decompiled).

---

## 3. Autonomous Continuous Loop (Run Until `pending` Is Empty)

Execute the following loop **continuously without stopping for user confirmation** until `pending` in `discovery_registry.json` is empty (or blocked by a missing binary / Ghidra error):

### Step 1: Pick Next Target
- Read `discovery_registry.json` (if missing, initialize it with the user's seed domain/function/binary in `pending`).
- Pop the next item from `pending` and set it as `in_progress`.

### Step 2: Decompile & Extract
- Use `ghidra_elf_query.py` (`search-funcs`, `search-strings`, `decompile`, `xrefs`) on the target binary.
- Decompile the target function and any small helper callees required for this atomic unit.
- **Atomic Gate**: If the target spans multiple distinct responsibilities, split it into atomic child nodes, add unseen children to `pending`, move the coarse node to `split`, and immediately pick the first atomic child.

### Step 3: Self-Audit (MECE, Atomic, Zero-Guessing, Pure Static)
Verify before writing the `.cs` file:
- [ ] **Zero Memory & Zero Guessing**: Every constant, formula, cast, clamp, and branch comes directly from `ghidra_elf_query.py` output.
- [ ] **Strict Partiality**: Any un-decompiled callee or untraced offset is explicitly marked `UNRESOLVED_BINARY_REF` (not guessed).
- [ ] **Pure Static C#**: No hidden global state; explicit inputs/outputs; `binary` + `0xAddress` cited in XML doc-comments.
- [ ] **MECE & Atomic**: No overlap with `done` items; 100% of observed branches covered.

### Step 4: Write C# Pure Static File & Update Registry
- Write `<output_dir>/<Domain>/<AtomicName>.cs` following [csharp-atomic-template.md](./references/csharp-atomic-template.md).
- Move the item from `in_progress` to `done` in `discovery_registry.json` (recording its `cs_path` and covered `addresses`).

### Step 5: Discover Unexplored Related Domains & Loop
- Inspect all `UNRESOLVED_BINARY_REF` callees, `xrefs` (callers), struct offsets read/written elsewhere, and packet/message hooks encountered in Step 2.
- Filter out any function address or `atomic_id` already present in `done`, `split`, or `pending` of `discovery_registry.json`.
- Append all genuinely new unexplored domains/functions to `pending` (recording `discovered_from` and `entry_hint` `0x...`).
- **Return immediately to Step 1** and continue until `pending` is empty.

---

## 4. Final Report (Only After Loop Completes or Blocks)

When `pending` is empty (or if blocked by an environment error):
1. Summarize total `.cs` files generated, total functions/addresses covered, and any `UNRESOLVED_BINARY_REF` items remaining.
2. Link to `discovery_registry.json` and the generated `.cs` files.
