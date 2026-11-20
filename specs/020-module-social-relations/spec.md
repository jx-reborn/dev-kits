# Specification: Social Relations (Spec 020 / Parity Journey J12)

## Summary
Implements bilateral player friendship handshakes, friend list synchronization, enemy auto-recording upon player PK slaughter, and blacklist interaction/whisper suppression for JXReborn.

## Core Rules & Behaviors
1. **Friend Handshake**:
   - Player A requests friend addition with optional greeting via `c2s_chatapplyaddfriend` (`0x61`).
   - Server validates Player B is online and has not blacklisted Player A.
   - Server delivers `s2c_chatapplyaddfriend` (`0x6A`) to Player B.
   - Player B accepts via `c2s_chataddfriend` (`0x62`) or refuses via `c2s_chatrefusefriend` (`0x63`).
   - On acceptance, mutual friendship is saved to `CharacterSocialRelations` database table and `s2c_chataddfriend` (`0x6B`) is dispatched to both participants.
2. **Enemy Auto-Recording**:
   - When a player is killed in PK/Slaughter mode, the killer's ID is automatically registered in the victim's social relation graph under relation kind `Enemy` (`2`).
   - Safe zone, duel, arena, and NPC deaths do not trigger enemy recording.
3. **Blacklist Suppression**:
   - When Player A is blacklisted by Player B:
     - Whispers on channel 3 from Player A to Player B are silently dropped or blocked.
     - Party invitations, trade invitations, and friend requests from Player A to Player B are suppressed.
4. **Presence Synchronization**:
   - Login / logout events dispatch `s2c_chatfriendonline` (`0x71`) and `s2c_chatfriendoffline` (`0x72`) to all connected friends.
