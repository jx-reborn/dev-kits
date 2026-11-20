# Legacy Binary Symbol Bookmarks (Local Only — Gitignored)

Local Ghidra virtual-address bookmarks archived from clean-room specification & documentation cleanup so compatibility verification (`/legacy-explore` and parity debugging against `ghidra_12.2_PUBLIC`) retains fast jump targets without tracking disassembly metadata in Git.

## 1. Protocol Size Tables (`002`)

| Subsystem / Spec | Binary | Address | Symbol / Purpose |
| :--- | :--- | :--- | :--- |
| Protocol Size Table (`002`) | `jx_linux_y` | `0x080ef560` | `gInitServerProtocolSize` (`S2C` fixed/variable packet size table initializer) |
| Protocol Size Table (`002`) | `jx_linux_y` | `0x080ef960` | `gInitClientProtocolSize` (`C2S` fixed/variable packet size table initializer) |

## 2. Movement & AOI (`006`)

| Subsystem / Spec | Binary | Address | Symbol / Purpose |
| :--- | :--- | :--- | :--- |
| Movement (`006`) | `jx_linux_y` | `0x08098a30` | `KNpc::SyncNpcMinPlayer` (`s2c_syncnpcminplayer` `0x4E` / `KNpc::ServeMove` movement sync cadence) |

## 3. Inventory, Currency & Items (`008`)

| Subsystem / Spec | Binary | Address | Symbol / Purpose |
| :--- | :--- | :--- | :--- |
| Inventory (`008`) | `jx_linux_y` | `0x0807b410` | `KItemList::SetItemCount` (stackable quantity sync rather than full `0x5F`) |
| Inventory (`008`) | `jx_linux_y` | `0x08075dd0` | `KItemList::SendMoneySync` (`S2C` player currency synchronization) |
| Inventory (`008`) | `jx_linux_y` | `0x0807e180` | `KItemList::ExchangeItemHand` (`s2c_playermoveitem` `0x62`) |

## 4. Dialogs & Scripting Host (`012`)

| Subsystem / Spec | Binary | Address | Symbol / Purpose |
| :--- | :--- | :--- | :--- |
| Dialogs / Scripts (`012`) | `jx_linux_y` | `0x080f2180` | `KProtocolProcess::DialogNpc` (`c2s_dialognpc` `0x6E`, validates anti-cheat key `0x506C`) |
| Dialogs / Scripts (`012`) | `jx_linux_y` | `0x080ba630` | `KPlayer::DialogNpc` (invokes target NPC dialog script) |
| Dialogs / Scripts (`012`) | `jx_linux_y` | `0x080bf0a0` | `KPlayer::IsCheckValueValid` (validates packet anti-cheat token) |
| Dialogs / Scripts (`012`) | `jx_linux_y` | `0x080bfbb0` | `KPlayer::DoScriptAction` (`s2c_scriptaction` `0x63`, `m_wProtocolLong = 15 + m_nBufferLen`) |
| Dialogs / Scripts (`012`) | `jx_linux_y` | `0x08130c60` | `LuaSelectUI` (`s2c_scriptaction` `0x63`, 900-byte TCVN3 NUL-separated options) |
| Dialogs / Scripts (`012`) | `jx_linux_y` | `0x08131130` | `ScriptDescribe` (`s2c_scriptaction` `0x63`, narrative/describe payload) |
| Dialogs / Scripts (`012`) | `jx_linux_y` | `0x080f2460` | `KProtocolProcess::PlayerSelUI` (`c2s_playerselui` `0x5F`, 17-byte v6.0 packet) |
| Dialogs / Scripts (`012`) | `jx_linux_y` | `0x080c6330` | `KPlayer::OnSelectFromUI` (dispatches selected dialog callback) |
| Dialogs / Scripts (`012`) | `jx_linux_y` | `0x081fc920` | `KBuySell::OpenSale` (`s2c_opensalebox` `0x84`) |

## 5. Durability & Golden Items (`013`)

| Subsystem / Spec | Binary | Address | Symbol / Purpose |
| :--- | :--- | :--- | :--- |
| Durability / Golden Items (`013`) | `jx_linux_y` | `0x08075240` | `gMakeSyncItem_Info` (`0x5F` `s2c_syncitem`: `Quality=1` packs `GoldRowId - 1` into `DetailType` & `-1` durability) |

## 6. PK, Camp Relations & Punishment (`014`)

| Subsystem / Spec | Binary | Address | Symbol / Purpose |
| :--- | :--- | :--- | :--- |
| PK / Combat (`014`) | `jx_linux_y` | `0x080da540` | `KPlayerPK::Init(int)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x080da5e0` | `KPlayerPK::ExercisePKClose()` (sparring end cleanup) |
| PK / Combat (`014`) | `jx_linux_y` | `0x080da6b0` | `KPlayerPK::EnmityPKClose()` (enmity end cleanup) |
| PK / Combat (`014`) | `jx_linux_y` | `0x080da7a0` | `KPlayerPK::CloseAll()` |
| PK / Combat (`014`) | `jx_linux_y` | `0x080da7c0` | `KPlayerPK::EnmityPKCountDown()` (200-frame warmup & 10-min expiry) |
| PK / Combat (`014`) | `jx_linux_y` | `0x080da8d0` | `KPlayerPK::ExercisePKOpen(int)` (start 1v1 sparring) |
| PK / Combat (`014`) | `jx_linux_y` | `0x080daac0` | `KPlayerPK::Active()` (18-Hz PK tick update) |
| PK / Combat (`014`) | `jx_linux_y` | `0x080dabb0` | `KPlayerPK::CheckLockPK(int)` (lock/cooldown check) |
| PK / Combat (`014`) | `jx_linux_y` | `0x080dac80` | `KPlayerPK::SetPKValue(int)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x080dacf0` | `KPlayerPK::AddPKValue(int)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x080dad60` | `KPlayerPK::SetNormalPKState(KE_NORMALPKSTATUS, int)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x080daee0` | `KPlayerPK::EnmityPKOpen(int)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x080b63d0` | `KNpcSet::GetRelation(KPlayer*, KPlayer*)` (PvP relation precedence) |
| PK / Combat (`014`) | `jx_linux_y` | `0x080b667e` | `KNpcSet::GetRelation` same-camp (`Camp=1` Justice) immunity branch |
| PK / Combat (`014`) | `jx_linux_y` | `0x080b67f0` | `KNpcSet::GetRelation(int, int)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x08091a90` | `KNpc::SetCurrentCamp(int)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x08091b20` | `KNpc::RestoreCurrentCamp()` |
| PK / Combat (`014`) | `jx_linux_y` | `0x080948c0` | `KNpc::DeathCalcPKValue(int, int, int&)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x08098140` | `KNpc::DeathPunish(int, int, int)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x080cd340` | `KPlayer::PkPunish(KPlayer*)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x082506c0` | `TABLE_PUNISHEXP` (Level >= 130 high-level max XP loss table) |
| PK / Combat (`014`) | `jx_linux_y` | `0x0807dc30` | `KItemList::AbradePercentInAllEquip(int)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x0806e910` | `KItem::AbradePercent(int)` |
| PK / Combat (`014`) | `jx_linux_y` | `0x080f1d20` | `KProtocolProcess::c2sPKApplyChangeNormalFlag` (`0x76`; lacks server `Camp=Begin` check) |
| PK / Combat (`014`) | `jx_linux_y` | `0x080f27d0` | `KProtocolProcess::PlayerApplySetPK` (`c2s_playersetpk`) |
| PK / Combat (`014`) | `jx_linux_y` | `0x080f36c0` | `KProtocolProcess::c2sPKApplyEnmity` (`0x77` `c2s_pkapplyenmity`) |
| PK / Combat (`014`) | `jx_linux_y` | `0x08122aa0` | `LuaSetPKFlag` (script binding invoking `ExercisePKOpen`) |

## 7. Factions & Skills (`015`)

| Subsystem / Spec | Binary | Address | Symbol / Purpose |
| :--- | :--- | :--- | :--- |
| Factions / Skills (`015`) | `jx_linux_y` | `0x080f27a0` | `KProtocolProcess::PlayerApplyFactionData` (`c2s_playerapplyfactiondata` `0x14`, 1B) |
| Factions / Skills (`015`) | `jx_linux_y` | `0x080be510` | `KPlayer::SendFactionData` (`PLAYER_FACTION_DATA` sync) |
| Factions / Skills (`015`) | `jx_linux_y` | `0x080f3650` | `KProtocolProcess::PlayerAddBaseSkillPoint` (`0x58` skill point allocation) |
| Factions / Skills (`015`) | `jx_linux_y` | `0x080c0eb0` | `KSkillList::Add` (preserves `SkillLevel = 0` skills in UI tree) |

## 8. Social, Chat, Shops, Repository, Party, Guilds & Quests (`016`–`025`)

| Subsystem / Spec | Binary | Address | Symbol / Purpose |
| :--- | :--- | :--- | :--- |
| Social / Chat (`016`) | `jx_linux_y` | `0x081df070` | `KPlayerChat::SendSystemInfo` (`0xFB` sub-opcode `0x21` system announcement) |
| Shops (`017`) | `jx_linux_y` | `0x080c6aa0` | `KPlayer::BuyItem` (shop buy flow) |
| Shops (`017`) | `jx_linux_y` | `0x081fe950` | `KBuySell::Buy` (backend purchase handler) |
| Shops (`017`) | `jx_linux_y` | `0x080c5e70` | `KPlayer::SellItem` (shop sell-back flow) |
| Shops (`017`) | `jx_linux_y` | `0x080f18b0` | `KProtocolProcess::ItemRepair` (`c2s_itemrepair`) |
| Shops (`017`) | `jx_linux_y` | `0x081df120` | `KPlayerChat::SendOpenSaleBox` (`s2c_opensalebox` `0x84`) |
| Repository Box (`018`) | `jx_linux_y` | `0x080f1ee0` | `KProtocolProcess::StoreMoneyCommand` (silver deposit / withdraw) |
| Repository Box (`018`) | `jx_linux_y` | `0x080f1580` | `KProtocolProcess::c2sBoxOperate` (storage box PIN operations) |
| Repository Box (`018`) | `jx_linux_y` | `0x081df200` | `KPlayerChat::SendOpenStoreBox` (`s2c_openstorebox` `0x88`) |
| Party (`019`) | `jx_linux_y` | `0x080f2f50` | Party protocol handler / team command entrypoint |
| Social Relations (`020`) | `jx_linux_y` | `0x080f0890` | Social / friend / enemy chat command handler |
| Purple Crafting (`023`) | `jx_linux_y` | `0x080f5b30` | `_ZN16KProtocolProcess17c2sFoundryRequestEiPh` (`0x92` 204B, `EQUIP_UPGRADE_TYPE` 0..6) |
| Purple Crafting (`023`) | `jx_linux_y` | `0x080f34c0` | `_ZN16KProtocolProcess12EnchaserItemEiPh` (`0x7F` 50B, `KItemEnchaser::Compound`) |
| Purple Crafting (`023`) | `jx_linux_y` | `0x08128450` | `_Z14LuaFoundryItemP9lua_State` (`0xFB` subtype `0x0F` open blacksmith foundry UI) |
| Purple Crafting (`023`) | `jx_linux_y` | `0x080f5ca5` | `KPlayer::SendData` (`s2c_foundry_result` `0xA0`, 7B) |
| Guilds (`024`) | `jx_linux_y` | `0x080e7e50` | Guild (`KTong`) protocol relay/command handler |
| Guilds (`024`) | `jx_linux_y` | `0x080f11e0` | Guild (`KTong`) client packet dispatcher |
| Quests / Da Tau (`025`) | `jx_linux_y` | `0x08130740` | `Prise` Lua C API (`s2c_scriptaction` `0x63` sub-type `0x0D`) |
| Quests / Da Tau (`025`) | `jx_linux_y` | `0x081286d0` | `SendTaskOrder` (`0xB6` 65B task order notification) |

## 9. Client PE Hooks (`game.exe`, `015`–`016`)

| Subsystem / Spec | Binary | Address | Symbol / Purpose |
| :--- | :--- | :--- | :--- |
| Factions / Skills (`015`) | `game.exe` | `0x00567ad0` | `s2c_npcchangecurcamp` (`0x58` updates `Npc[idx].m_CurrentCamp`) |
| Factions / Skills (`015`) | `game.exe` | `0x00567b00` | `s2c_npcchgcamp` (`0x59` updates `Npc[idx].m_Camp`) |
| Social / Chat (`016`) | `game.exe` | `0x00557EF4` | System announcement `0xFB` (`0x21`) handler (`"Công bố "` prefix) |
