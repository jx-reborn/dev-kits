# Research: Purple Equipment Crafting & Enchasing (Spec 023)

## Compatibility Rules & Wire Contracts

1. **Binary Wire Framing (`0x7F`, `0x92`, `0xFB`, `0xA0`)**:
   - `0x92` (`c2s_foundryrequest`, 204 bytes): Evaluates `EQUIP_UPGRADE_TYPE` `0..6` foundry upgrade requests.
   - `0x7F` (`c2s_enchaseritem`, 50 bytes): Dispatches sequential compound/enchasing operations.
   - `0xFB` (`s2c_extend`, subtype `0x0F`): Opens the client blacksmith foundry interface.
   - `0xA0` (`s2c_foundry_result`, 7 bytes): Delivers success/failure result code to the client.

2. **Server Scripts (`$LEGACY_SERVER_DIR\server1\script\item\compound\`)**:
   - `xuanjing_compound.lua`: Jewelry into XuanJing stone conversion. Fee: 1,000 silver.
   - `ore_upgrade.lua`: Compounding 3 stones of level $L$ into 1 stone of level $L+1$.
     Formula: $P_{\text{success}} = \frac{V_{\text{src}} / 3}{V_{\text{des}} - (2/3) V_{\text{src}}}$.
     On failure: removes `arynNecessaryItemIdx[1]` and alternative items; stones 2 and 3 remain in inventory.
   - `magic_distill.lua`: Extracts magic attribute line $k = \text{Particular} - 148$ into Magic Ore $199 + k$. Series lock enforced for even lines: `nEquipSeries == g_nOreSeries`.
   - `equip_compound.lua`: Forges normal/blue gear into purple base with unassigned sockets (`arynMagLvl = -1`).
   - `equip_enchase.lua`: 6-socket sequential enchasing. Next available socket must match ore particular $199 + j$. Even sockets require matching equipment series. Duplicate affixes rejected with `RESULT_MAGIC_ERROR`. Equipment is explicitly excluded from removal on failure:
     ```lua
     for i = 1, getn(arynNecessaryItemIdx) do
         if (arynNecessaryItemIdx[i] == g_nEquipIdx) then
             arynNecessaryItemIdx[i] = nil;
             break;
         end
     end
     ```
     Therefore, the purple base equipment is never destroyed on failure.

3. **Value Tables (`settings/item/004/itemvalue/ore.txt`)**:
   - Level 1: 10,000
   - Level 2: 50,000
   - Level 3: 225,000
   - Level 4: 900,000
   - Level 5: 3,300,000
   - Level 6: 11,314,000
   - Level 7: 36,770,000
   - Level 8: 126,070,000
   - Level 9: 462,250,000
   - Level 10: 1,849,000,000
