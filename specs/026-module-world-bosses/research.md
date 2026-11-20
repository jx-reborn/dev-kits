# Research: Great Golden World Bosses (Spec 026)

## Canonical Architecture Analysis

1. **Scheduling**:
   - Great Golden Bosses spawn at fixed daily intervals (typically 12:30 and 19:30).
   - Boss entities feature elevated base stats, unique auras (Kim/Mộc/Thủy/Hỏa/Thổ), and elemental resistance passives.

2. **Damage Attribution & Drop Rights**:
   - The original core logic evaluates damage attribution upon NPC death:
     - All attacking characters within the sharing radius (768 Mps / 3x3 AOI) have their damage accumulated.
     - Active party members aggregate their individual contributions into a party total.
     - If `PartyTotalDamage > HighestIndividualDamage`, the party captain is awarded the kill and drop rights.
     - Otherwise, the highest individual damager receives the drop rights.

3. **Loot Drops & Protection**:
   - Dropped items and money are spawned in Archimedean spiral coordinates around the boss's death coordinate.
   - 30-second ownership protection prevents unauthorized pickup.
