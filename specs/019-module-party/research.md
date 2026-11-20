# Research & Ground Truth: Party Management (Spec 019)

## Authoritative References
- **Compatibility Baseline**: Legacy 6.0 team management protocol (`0x53` C2S / `0x69` S2C), AOI EXP sharing, and leadership capacity rules.
- **Configuration**: `team_resist_effect.ini`, `level_lead_exp.txt`.

## Packet Structures
- **C2S Multiplexed Opcode `0x53`**:
  - `wLength` at byte 1 (word)
  - `btSubOpcode` at byte 3 (byte)
- **S2C Multiplexed Opcode `0x69`**:
  - Length-minus-one framing (`wLength = totalLength - 1`)
  - Sub-opcodes `0x01` (`teaminfo`), `0x02` (`teamselfinfo`), `0x04` (`createsuccess`), `0x08` (`addmember`), `0x09` (`leave`), `0x0A` (`changecaptain`), `0x0C` (`teaminviteadd`).

## EXP & Aura Invariants
- Distance cutoff: 768 Mps ($768^2 = 589,824$).
- Leadership progression table from `level_lead_exp.txt`:
  - 1..5: 4 members
  - 6..10: 5 members
  - 11..18: 6 members
  - 19..26: 7 members
  - 27+: 8 members
