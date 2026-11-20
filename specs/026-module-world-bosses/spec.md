# Specification: Great Golden World Bosses (Spec 026)

**Feature Code**: `026-module-world-bosses`  
**Status**: Implemented & Verified  
**Golden Reference**: Spec 016 (`IZoneModule`, `IMissionController`)  

---

## 1. Executive Summary

This feature provides authoritative, scheduled spawns, combat engagement, real-time damage attribution, party vs. individual drop ownership evaluation, 30-second drop protection, and Archimedean spiral loot distribution for the 11 Great Golden World Bosses in JXReborn Phase 2.

The implementation functions strictly as an isolated plug-in module (`WorldBossZoneModule : IZoneModule`) registering tick systems and command routes without altering `MapZoneWorker.cs`.

---

## 2. Functional Requirements

### FR-001: Authoritative Spawn Scheduling
- Bosses spawn authoritative entities at canonical server times: 12:30 and 19:30 UTC+7.
- Default maps and coordinates match legacy 6.0 world boss data (Cổ Bách, Huyền Giác, Đường Phi Yến, Lam Y Y, Hà Linh Phiêu, Yên Hiểu Trái, Mạnh Thương Lương, Gia Luật Tị Ly, Đạo Thanh Chân Nhân, Tuyền Cơ Tử, Từ Đại Nhạc).
- Server announces boss appearance across all active shards.

### FR-002: Combat Damage Attribution & Drop Rights
- Tracks real-time cumulative damage dealt by each player and active party members.
- Aggregates damage for party members present in the boss's 3x3 AOI (radius 768 Mps).
- Compares highest party total damage against highest individual player damage.
- Winning entity receives exclusive drop ownership: if party wins, party captain receives drop ownership; if individual wins, that player receives ownership.

### FR-003: 30-Second Drop Ownership Protection
- Loot items and money drops are protected for 30 seconds (`DropOwnershipProtectSeconds = 30`).
- During this window, only the designated winning character (or party members) can pick up drops.
- After 30 seconds, protection lapses and drops become free-for-all.

### FR-004: Archimedean Spiral Loot Distribution
- Ground objects are arranged in an Archimedean spiral around the boss's death coordinates.
- Prevents item stacking and ensures aesthetic, canonical dispersion.

### FR-005: Server-Wide System Announcements
- Defeat announcement broadcast when boss life drops to 0, identifying the victorious player or party.

---

## 3. Success Criteria

1. **Zero Core Modifications**: Zero changes to `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs` (`git diff = 0`).
2. **Deterministic Attribution**: The entity (party vs solo) with higher verified damage in the 3x3 AOI reliably wins drop ownership.
3. **Drop Protection**: 30-second ownership window enforced on all boss drops.
4. **Architecture Compliance**: Clean Release build with `-warnaserror` (0w/0e) and 4/4 passed `SolutionArchitectureComplianceTests`.
