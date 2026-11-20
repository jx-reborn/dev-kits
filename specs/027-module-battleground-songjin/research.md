# Research: Song-Jin Battleground (Spec 027)

## Canonical Analysis

1. **Battleground Lifecycle**:
   - Canonical timeline: 10 minutes registration followed by 60 minutes of battle, with a 30s settlement and cooldown.
   - Camp balancing dynamically checks member difference: `|Song - Jin| <= 5`.

2. **Scoring Formulas**:
   - Base kill: 50 points.
   - 5x5 Rank Bonus Matrix scales base points based on attacker vs victim military rank (1 to 5).
   - Kill streaks: Consecutive kills without dying grant escalating bonus points.
   - Shutdown bonuses reward ending an opponent's kill streak.

3. **Settlement**:
   - Final victory: 1200 points to winners, 600 points to losers, 900 points on a draw.
   - Points are banked into Task 747 for Quartermaster shop exchange.
