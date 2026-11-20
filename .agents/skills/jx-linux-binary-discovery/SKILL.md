---
name: jx-linux-binary-discovery
description: >-
  Autonomously decompile and audit jx-linux binaries in a continuous loop to
  extract MECE, atomic game mechanics, exact formulas, struct offsets, and
  binary evidence into faithful C++ reconstruction files, implementing strictly
  what is read from decompilation without guessing or standalone build pressure,
  and tracking all progress in a single persistent registry file.
---

# JX-Linux Binary Atomic Discovery Skill (Faithful C++ Output)

Systematically reverse-engineer `jx-linux` ELF binaries (`server1/jx_linux_y`, `server1/*.so`, `gateway/s3relay/s3relay_y`, `bishop_y`, `goddess_y`) into **MECE, atomic C++ code** using an **autonomous continuous discovery loop**.

---

## 0. Hard Constraints (Violation = Invalid Output)

1. **ZERO PRIOR MEMORY**:
   - Do **NOT** use any pre-existing knowledge of Võ Lâm Truyền Kỳ, JX1, or JX-Linux.
   - Treat every mechanic, formula, constant, enum, and state variable as completely unknown until observed in decompiled output.
2. **ZERO GUESSING & STRICT PARTIALITY ("Đọc đến đâu, ghi đến đó")**:
   - Never infer or extrapolate variable meanings, constants, clamp bounds, rounding rules, or behavior of un-decompiled functions.
   - Only write C++ code for logic, branches, and formulas **directly observed** in decompiled pseudo-C / assembly.
   - Preserve the **original C++ language semantics** (original class/method names, `this->m_...` member names, integer-division truncation, bitshifts, signed/unsigned casts, overflow guards, and `if/else/switch` conditions verbatim).
   - **Code does NOT need to compile or run standalone**: Do **NOT** invent fake headers, dummy structs, or guessed stub bodies just to satisfy a compiler. For any un-decompiled `callee`, vtable slot, or untraced offset, keep the exact call/expression as observed and mark `// UNRESOLVED_BINARY_REF: <symbol> @ <0xAddr>`, then push it to `pending`.
3. **BINARY-ONLY GROUND TRUTH**:
   - All facts must come directly from `jx-linux` binaries queried via the existing `legacy-explore` Ghidra script:
     ```powershell
     python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary <bin> search-funcs "<regex>"
     python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary <bin> search-strings "<substring>"
     python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary <bin> decompile <name_or_0xAddr>
     python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary <bin> xrefs <name_or_0xAddr>
     ```
   - Every C++ file and function MUST carry comments citing `binary`, `function`, and `0xAddress`.

---

## 1. Single Persistent Registry & Output Layout

All invocations of this skill MUST share **one single registry file** so progress persists across sessions:

- **`artifacts/binary-discovery/discovery_registry.json`**: The **only** registry file used for MECE deduplication and cross-session loop tracking (`pending`, `in_progress`, `done`, `split`). Never create separate per-domain registry files.
- **`artifacts/binary-discovery/<domain>/<AtomicName>.cpp`** (and `.hpp` only when structs/enums are directly observed): Faithful C++ reconstruction files conforming to [cpp-atomic-template.md](./references/cpp-atomic-template.md).

---

## 2. MECE & Atomic Standards

- **Faithful C++**:
  - Keep original C++ class/method names (e.g., `KNpc::DoStand`, `KNpc::CalcColdAttribDamage`) and original member/global names (`this->m_CurrentLife`, `NpcSet.m_nPKDamageRate`) as seen in Ghidra DWARF/decompile output.
  - Do **NOT** refactor into C# or artificial pure-static wrappers that obscure the original binary structure.
- **Atomic**: Each `.cpp` file (or method block) covers **exactly one** independent mechanism, formula, packet handler, or state transition.
  - If a decompiled function implements multiple independent mechanisms, **split** them into separate atomic items in `pending`, mark the coarse item `split` in `discovery_registry.json`, and process one atomic item per iteration.
- **MECE**:
  - **Mutually Exclusive**: Before starting or adding any item to `pending`, check `done`, `split`, and `pending` in `artifacts/binary-discovery/discovery_registry.json` (by both `atomic_id` and primary binary address `0x...`). Never duplicate an already-covered mechanism.
  - **Collectively Exhaustive**: Every branch (`if`, `else`, `switch`, early `return`), edge case, clamp, and side effect observed in the decompiled code must be captured (or explicitly marked `UNRESOLVED_BINARY_REF` if a callee is not yet decompiled).

---

## 3. Autonomous Continuous Loop (Run Until `pending` Is Empty)

Execute the following loop **continuously without stopping for user confirmation** until `pending` in `artifacts/binary-discovery/discovery_registry.json` is empty (or blocked by a missing binary / Ghidra error):

### Step 1: Pick Next Target
- Read `artifacts/binary-discovery/discovery_registry.json` (if missing, initialize it; if it already exists, **append** any new seed targets not already in `done`/`split`/`pending` without overwriting existing history).
- Pop the next item from `pending` and set it as `in_progress`.

### Step 2: Decompile & Extract
- Use `ghidra_elf_query.py` (`search-funcs`, `search-strings`, `decompile`, `xrefs`) on the target binary.
- Decompile the target function and any small helper callees required for this atomic unit.
- **Atomic Gate**: If the target spans multiple distinct responsibilities, split it into atomic child nodes, add unseen children to `pending`, move the coarse node to `split`, and immediately pick the first atomic child.

### Step 3: Self-Audit (MECE, Atomic, Zero-Guessing, Faithful C++)
Verify before writing the `.cpp` file:
- [ ] **Zero Memory & Zero Guessing**: Every constant, formula, cast, clamp, and branch comes directly from `ghidra_elf_query.py` output.
- [ ] **Strict Partiality**: Any un-decompiled callee or untraced offset is explicitly marked `// UNRESOLVED_BINARY_REF: ...` (not guessed, not fabricated for compilation).
- [ ] **Faithful C++**: Original class/method/field names and `binary` + `0xAddress` cited in doc-comments.
- [ ] **MECE & Atomic**: No overlap with `done` items in `discovery_registry.json`; 100% of observed branches covered.

### Step 4: Write C++ File & Update Single Registry
- Write `artifacts/binary-discovery/<domain>/<AtomicName>.cpp` following [cpp-atomic-template.md](./references/cpp-atomic-template.md).
- Move the item from `in_progress` to `done` in `artifacts/binary-discovery/discovery_registry.json` (recording its `cpp_path` and covered `addresses`).

### Step 5: Discover Unexplored Related Domains & Loop
- Inspect all `UNRESOLVED_BINARY_REF` callees, `xrefs` (callers), struct offsets read/written elsewhere, and packet/message hooks encountered in Step 2.
- Filter out any function address or `atomic_id` already present in `done`, `split`, or `pending` of `discovery_registry.json`.
- Append all genuinely new unexplored domains/functions to `pending` (recording `discovered_from` and `entry_hint` `0x...`).
- **Return immediately to Step 1** and continue until `pending` is empty.

---

## 4. Final Report (Only After Loop Completes or Blocks)

When `pending` is empty (or if blocked by an environment error):
1. Summarize total `.cpp` files generated, total functions/addresses covered, and any `UNRESOLVED_BINARY_REF` items remaining.
2. Link to `artifacts/binary-discovery/discovery_registry.json` and the generated `.cpp` files.
