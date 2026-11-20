# Specification: Party Management (Spec 019 / Parity Journey J11)

## Summary
Implements classic party formation, management, roster synchronization, AOI-based monster experience sharing, and captain elemental resistance auras for JXReborn.

## Core Rules & Formulas
1. **Party Roster**:
   - Maximum capacity: 8 players (Captain + 7 members).
   - Leadership level gates maximum party size:
     - Level 1-5: Cap = 4
     - Level 6-10: Cap = 5
     - Level 11-18: Cap = 6
     - Level 19-26: Cap = 7
     - Level 27+: Cap = 8
2. **EXP Sharing**:
   - Monster kill EXP is distributed among party members within `PLAYER_SHARE_EXP_DISTANCE = 768` Mps (distance squared <= 589,824).
   - Bonus multiplier $k = 100 + 15 \times N$ where $N$ is the number of eligible in-range party members.
   - Proportional share per member: $\text{ratio} = \max(5, k \times \text{MemberLevel} / \text{TotalLevel})$.
   - Member EXP gained: $\text{MonsterExp} \times \text{ratio} / 100$.
   - Captain Leadership EXP: $\text{MonsterExp} \times k \times 25 \times (\text{TotalLevel} - \text{KillerLevel}) / (\text{TotalLevel} \times 10,000)$.
3. **Captain Elemental Aura**:
   - Radiates passive elemental resistance buff to in-range members based on captain's series:
     - Metal (Kim): Physical Resist (+3% base, +10% allied Water)
     - Wood (Mộc): Poison Resist (+3% base, +10% allied Fire)
     - Water (Thủy): Cold Resist (+3% base, +10% allied Wood)
     - Fire (Hỏa): Fire Resist (+3% base, +10% allied Earth)
     - Earth (Thổ): Lightning Resist (+3% base, +10% allied Metal)
4. **Wire Protocol**:
   - Multiplexed C2S Opcode `0x53` (`c2s_teamprotocol`) with SubOpcodes 0x01..0x0B.
   - Multiplexed S2C Opcode `0x69` (`s2c_teamprotocol`) with SubOpcodes 0x01..0x0A, 0x0C using length-minus-one framing.
