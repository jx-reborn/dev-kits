# Quickstart: Social Relations (Spec 020)

## Sending a Friend Request
1. Player A sends `c2s_chatapplyaddfriend` (`0x61`) with target NPC ID and introductory message.
2. Server validates target is online and hasn't blacklisted Player A.
3. Target receives `s2c_chatapplyaddfriend` (`0x6A`) popup dialog.

## Accepting a Friend Request
1. Target sends `c2s_chataddfriend` (`0x62`).
2. Server saves bilateral `CharacterSocialRelation` records in DB.
3. Both clients receive `s2c_chataddfriend` (`0x6B`) updating their friend panels.

## Slaughter PK Enemy Recording
- When Player A slaughters Player B in PK mode:
  ```csharp
  victimSlot.RecordSlaughterEnemy(killerCharacterId);
  ```
- Killer is appended to victim's enemy list and saved.

## Live Differential Parity Verification (Journey `J12`)
```powershell
.\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParitySocialGraphTests"
```
