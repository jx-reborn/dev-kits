# Quickstart: Guilds/Tong System (Spec 024)

## Creating a Guild
1. Candidate Master satisfies requirements:
   - Level $\ge 50$
   - Leadership level $\ge 20$
   - Item *Nhạc Vương Kiếm* (`Genre=4`, `DetailType=195`) in inventory
   - 1,000,000 cash in inventory
   - Free camp (`CampKind.Free = 4`)
   - Not in party, not in guild
2. Client sends creation command (e.g. `c2s_tongextend` opcode `0x9A` or lua NPC trigger).
3. Server executes atomic creation:
   - Deducts 1M cash and consumes Nhạc Vương Kiếm item.
   - Sets character camp to chosen guild camp.
   - Inserts `Guild` and `GuildMember` with Master rank.
   - Sends `s2c_tongcreate` (`0xA0`) confirmation.

## Donating to Treasury
1. Member donates cash:
   ```csharp
   guildManager.DonateTreasury(guildId, amount);
   ```
2. Inventory cash decreases, guild treasury increases.

## Automated Upkeep Tick
- Every 750 seconds, `GuildTreasuryUpkeepTickSystem` evaluates:
  - If `Treasury >= 1,000,000`, deducts $5000 + \max(0, N - 100) \times 50$ and awards $+120$ EXP.

## Live Differential Parity Verification (Journey `J16`)
```powershell
.\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParityEndgameMissionTests"
```
