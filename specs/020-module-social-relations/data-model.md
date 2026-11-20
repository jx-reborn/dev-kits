# Data Model: Social Relations (Spec 020)

## Persistent Entities

### CharacterSocialRelation
- **CharacterId**: Foreign key to owning Character (`int`).
- **TargetCharacterId**: Foreign key to related Character (`int`).
- **TargetCharacterName**: Cached name of target character (`string`, max 32 chars).
- **RelationKind**: `SocialRelationKind` enum (`byte`):
  - `0`: None
  - `1`: Friend (Hảo Hữu)
  - `2`: Enemy (Cừu Nhân)
  - `3`: Blacklist (Hắc Danh Sách)
- **CreatedAtUtc**: Creation timestamp (`DateTime`).

## In-Memory Session Entities

### PlayerSocialSessionSlot
- **Friends**: Dictionary of friend character IDs to names and online status.
- **Enemies**: List of recorded killer character IDs.
- **Blacklist**: Set of blocked character IDs.
- **PendingRequests**: In-flight friend handshake requests.

## Invariants
1. **Capacity Constraints**:
   - `MaxFriends = 100`
   - `MaxEnemies = 50`
   - `MaxBlacklist = 50`
2. **Bilateral Symmetry**:
   - When Player A and Player B become friends, two records exist: $(A, B, \text{Friend})$ and $(B, A, \text{Friend})$.
3. **Blacklist Behavior**:
   - When Player B is in Player A's blacklist, any whisper from B to A on channel 3 or direct invite is rejected.
