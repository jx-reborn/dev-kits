# Test Mode & GM Test Token Handbook (`(6, 1, 4258)`)

## 1. Secure Defaults
In both `JXReborn.ZoneEngine` and `src/JXReborn.Host/appsettings.Development.json`, Test Mode defaults to **`false`**:
- `ZoneEngine:EnableTestGmToken = false`
- `ZoneEngine:AutoGrantGmTokenOnLogin = false`

When `EnableTestGmToken = false`, item `(6, 1, 4258)` (`cam-nang-tan-thu`) has no active effect and will not open the GM menu.

---

## 2. How to Enable Test Mode for Local QA

### Option A — Environment Variables When Running `JXReborn.Host` (Recommended, Keeps Git Clean)
```powershell
$env:ZoneEngine__EnableTestGmToken="true"
$env:ZoneEngine__AutoGrantGmTokenOnLogin="true"
dotnet run --project src\JXReborn.Host
```

### Option B — Temporarily Enable in `src/JXReborn.Host/appsettings.Development.json`
```json
{
  "ZoneEngine": {
    "EnableTestGmToken": true,
    "AutoGrantGmTokenOnLogin": true
  }
}
```
*(Remember to revert both flags back to `false` before committing or deploying to production.)*

---

## 3. Obtaining & Using the Test Handbook
- When both `AutoGrantGmTokenOnLogin = true` and `EnableTestGmToken = true` are set, characters automatically receive the **Test Handbook** (`(6, 1, 4258)`) in their bag (`EquipRoom`) on world entry.
- In game, open the inventory (`F4`) and **right-click** the Test Handbook to open the interactive QA menu.

---

## 4. Test Handbook Menu Categories

| Option | Category | Primary Capabilities |
| :--- | :--- | :--- |
| `0` | **Character & Leveling** | Level up quickly to Lv10 / Lv50 / Lv80, grant unallocated attribute/skill points, and fast-join a sect. |
| `1` | **Silver / Currency** | Grant inventory silver for NPC shop, personal stall, or P2P trade testing. |
| `2` | **Preset Kits (Journey Fixtures)** | Load deterministic item/level/point bundles tailored for QA journeys (`J09`, `J13`, `J16`, etc.). |
| `3` | **Quick Teleport** | Teleport directly to *Ba Lăng Huyện* (53), *Tương Dương* (78), *Dương Châu* (80), *Đạo Hương Thôn* (101), *Võ Đang* (81), or *Vũ Lăng Động* (199). |
| `4` | **Combat / PK / Rank Testing** | Restore full HP/MP/Stamina; set `PkValue = 10` or `0` (verify evil ladder rank & PK name color); toggle `FightMode = 1/0` on demand. |
| `5` | **Trade & Equipment Test Gear** | Grant 5 unbound HP potions + 5 unbound MP potions, or unbound weapon + armor for instant two-client P2P trade and stall buyout tests. |
