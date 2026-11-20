# Quickstart: Party Management (Spec 019)

## Creating a Party
1. Player sends `c2s_teamapplycreate` (`[0x53, 0x07, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00]`).
2. Server validates player is not currently in a party.
3. Server initializes `PartyModel` with player as captain (CaptainPlayerId = CharacterId).
4. Server replies with `s2c_teamcreatesuccess` (`0x69`, SubOpcode `0x04`).

## Inviting Members
1. Captain sends `c2s_teaminviteadd` (`[0x53, 0x07, 0x00, 0x0A, dwTargetNpcId]`).
2. Server checks captaincy, leadership capacity limit, and blacklist status.
3. Server routes `s2c_teaminviteadd` (`0x69`, SubOpcode `0x0C`) to invitee.
4. Invitee replies with `c2s_teamreplyinvite` (`0x53`, SubOpcode `0x0B`).
5. On accept, `s2c_teamaddmember` (`0x08`) broadcasts to existing party and `s2c_teamselfinfo` (`0x02`) syncs to applicant.

## Monster EXP Sharing
- Triggered automatically in `GroundObjectTickSystem` when a monster dies:
  ```csharp
  partyManager.DistributeMonsterExp(zone, monsterPos, killerCharacterId, monsterExp);
  ```
- Members within 768 Mps receive proportional EXP: $\text{ratio} = \max(5, (100 + 15N) \times \text{Level} / \text{TotalLevel})$.

## Live Differential Parity Verification (Journey `J11`)
```powershell
.\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParityPartyTests"
```
