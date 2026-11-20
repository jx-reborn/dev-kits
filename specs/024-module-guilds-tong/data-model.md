# Data Model: Guilds/Tong System (Spec 024)

## Persistent Database Entities

### Guild (`Guilds` table)
- **GuildId**: Primary key (`int`, identity).
- **Name**: Unique guild name (`string`, max 32 chars).
- **MasterCharacterId**: Character ID of the Guild Master (`int`).
- **Camp**: Alignment camp (`CampKind`: 1=Justice, 2=Evil, 3=Balance).
- **Level**: Guild level (`int`, defaults to 1).
- **Experience**: Accumulated guild experience points (`long`).
- **TreasuryMoney**: Guild treasury currency in lượng bạc (`long`).
- **CreatedAtUtc**: Creation timestamp (`DateTime`).
- **Members**: One-to-many relationship with `GuildMember`.

### GuildMember (`GuildMembers` table)
- **GuildMemberId**: Primary key (`int`, identity).
- **GuildId**: Foreign key to `Guild` (`int`).
- **CharacterId**: Unique foreign key to `Character` (`int`).
- **Rank**: `TongRankKind` (0=Member, 1=Manager, 2=Director, 3=Master).
- **PositionSlot**: Numerical slot for rank (0..6 for Director, 0..55 for Manager).
- **JoinedAtUtc**: Timestamp when joined (`DateTime`).

## In-Memory Zone Entities

### GuildModel
- Active zone representation with cached roster, treasury, and upkeep frame counter.

### PlayerGuildSessionSlot
- Player's active guild membership representation and permission check interface.

## Invariants & Formulas
1. **Upkeep Formula (every 750 seconds)**:
   - If $\text{TreasuryMoney} \ge 1,000,000$:
     - $\text{Fee} = 5000 + \max(0, \text{MemberCount} - 100) \times 50$
     - $\Delta\text{Exp} = +120$
   - If $\text{TreasuryMoney} < 1,000,000$:
     - $\text{Fee} = 0$, $\Delta\text{Exp} = 0$.
2. **Rank Caps**:
   - Master: 1
   - Director: $\le 7$
   - Manager: $\le 56$
