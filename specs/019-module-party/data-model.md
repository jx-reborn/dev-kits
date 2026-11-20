# Data Model: Party Management (Spec 019)

## Conceptual Entities

### Party
- **PartyId**: Unique integer identifier (`int`) identifying the active party session in the zone.
- **CaptainPlayerId**: Character ID of the party captain (`int`).
- **IsOpen**: Boolean flag indicating whether the party is open for applications (`bool`).
- **Members**: Ordered list of active party members (`List<int>`), maximum capacity 8.
- **AuraSkillId**: Current active captain aura skill ID (`int`).

### PartyMember
- **CharacterId**: Unique character identifier (`int`).
- **Name**: Character display name (`string`, max 32 chars).
- **Level**: Character level (`byte`).
- **Faction**: Sect / Faction ID (`byte`).
- **Series**: Elemental series (0=Metal, 1=Wood, 2=Water, 3=Fire, 4=Earth).
- **CurrentHp / MaxHp**: Hit points for UI health bar synchronization.
- **CurrentMp / MaxMp**: Mana points for UI mana bar synchronization.
- **JoinedAtUtc**: Timestamp when the player joined the party.

### Formulas & Invariants
1. **Capacity Limit**:
   - $\text{MaxMembers} \le 8$
   - Leadership level gates:
     - Level 1–5: 4 members
     - Level 6–10: 5 members
     - Level 11–18: 6 members
     - Level 19–26: 7 members
     - Level 27+: 8 members
2. **EXP Sharing Math**:
   - Radius: $\le 768$ Mps ($\text{dist}^2 \le 589,824$).
   - Multiplier: $k = 100 + 15 \times N$ ($N =$ number of in-range members).
   - Member Share Ratio: $\text{ratio} = \max(5, k \times \text{MemberLevel} / \text{TotalLevel})$.
   - EXP awarded: $\text{MonsterExp} \times \text{ratio} / 100$.
   - Captain Leadership EXP: $\text{MonsterExp} \times k \times 25 \times (\text{TotalLevel} - \text{KillerLevel}) / (\text{TotalLevel} \times 10,000)$.
3. **Elemental Aura (Kim/Mộc/Thủy/Hỏa/Thổ)**:
   - Base elemental resist: $+3\%$.
   - Allied element resist: $+10\%$.
