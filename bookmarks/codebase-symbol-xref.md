# Codebase Legacy Symbol & Address Cross-Reference (`codebase-symbol-xref.md`)

> **Strictly Private Reference (`jx-reborn/workbench`)**: Archived automatically from C# source/tool/test comments prior to Clean-Room scrubbing `jx-reborn/server`.
> **Total archived lines**: 534 across 167 C# files.

## `src/` (239 lines in 106 files)

### `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.ChatSink.cs`
- **L27**: `// In game.exe KPlayerChat::ReceiveChat (0x005b57d0):`
- **L28**: `// - Channels 1 (World), 3 (PrivateWhisper), and 6 (Tong) identify the sender via KPlayer::m_dwID`
- **L29**: `//   (g_HashString2Id over the TCVN3 role name, resolved via KNpcSet::SearchNameID).`
- **L30**: `// - Channels 2 (Nearby), 4 (Team), and 5 (Faction) identify the sender via KNpc::m_dwID (RuntimeNpcId).`

### `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.DialogSink.cs`
- **L35**: `// Domain ShopTemplate.Id is 1-based (matching Lua Sale(nId)); KBuySell::OpenSale (0x081fc920 / LuaSale 0x0812fa6b)`

### `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.WorldSink.cs`
- **L96**: `// 10b. 0xC9: SyncMemFile notification so client KBuySell::OnFileChanged loads buysell.txt/goods.txt`
- **L322**: `// Emit 0x4E (s2c_syncnpcminplayer) BEFORE 0xC5 (s2c_syncpos): in game.exe, 0x4E loads cross-region 3x3`

### `src/JXReborn.Adapter.Legacy/GameServer/ObjMouseClickPacketHandler.cs`
- **L11**: `/// with <c>IsMouseClick = true</c> (<c>KProtocolProcess::ObjMouseClick</c> @ <c>0x080f4500</c>).`

### `src/JXReborn.Adapter.Legacy/GameServer/PlayerCommunityExtendPacketHandler.cs`
- **L10**: `/// <c>0x21</c> channel chat, <c>0x2D</c> group chat) emitted by the <c>game.exe</c> chat bar.`

### `src/JXReborn.Adapter.Legacy/GameServer/RelayExtendPacketHandler.cs`
- **L9**: `/// and tong (<c>\O</c>) channels in <c>game.exe</c> UI (<c>0x0048f510</c> / <c>0x0048e030</c>).`

### `src/JXReborn.Adapter.Legacy/GameServer/RepairEquipmentItemPacketHandler.cs`
- **L9**: `/// In <c>jx_linux_y</c> <c>KProtocolProcess::ItemRepair</c> (<c>0x080f18b0</c>), <c>RepairType == 0</c> repairs durability;`
- **L10**: `/// <c>2</c> and <c>3</c> are <c>ApplyBindOperate</c> (<c>0x080f1936</c>).`

### `src/JXReborn.Adapter.Legacy/GameServer/ViewEquipPacketHandler.cs`
- **L12**: `/// (<c>KProtocolProcess::c2sViewEquip</c> @ <c>0x080f31d0</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/AskClientForNumberS2CPacket.cs`
- **L9**: `/// emitted by <c>LuaAskClientForNumber</c> (<c>0x08138c50</c>) in <c>jx_linux_y</c>.`

### `src/JXReborn.Adapter.Legacy/Packets/CastSkillDirectlyS2CPacket.cs`
- **L11**: `/// (triggers <c>KNpc::SetNextSkillCastTime</c> in <c>game.exe @ 0x005558c5 -&gt; 0x00579930</c>); <c>0</c> otherwise (e.g. <c>CastAura</c> / direct script cast).`

### `src/JXReborn.Adapter.Legacy/Packets/CommonInputNumberC2SPacket.cs`
- **L8**: `/// handled by <c>KCityWarProcess::c2sCityWarCommonInputNumber</c> (<c>0x080f1680</c>) in <c>jx_linux_y</c>.`

### `src/JXReborn.Adapter.Legacy/Packets/EnchaserItemC2SPacket.cs`
- **L10**: `/// <c>c2s_enchaseritem</c> (<c>0x7F</c>, 50 bytes): client enchase item request (<c>KProtocolProcess::EnchaserItem</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/FoundryResultS2CPacket.cs`
- **L8**: `/// <c>s2c_foundry_result</c> (<c>0xA0</c>, 7 bytes): server response for foundry and compound operations (<c>KPlayer::SendFoundryResult</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/ItemAutoMoveSyncS2CPacket.cs`
- **L9**: `/// Verified against <c>jx_linux_y</c> DWARF <c>tagITEM_AUTO_MOVE_SYNC</c>:`

### `src/JXReborn.Adapter.Legacy/Packets/ItemCountSyncS2CPacket.cs`
- **L8**: `/// a stackable item's remaining quantity in-place (<c>KItemList::SetItemCount</c> @ <c>0x0807b410</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/LadderQueryC2SPacket.cs`
- **L7**: `/// Verified against <c>jx_linux_y</c> <c>KProtocolProcess::LadderQuery</c> (<c>0x080f1990</c>)`
- **L8**: `/// and <c>game.exe</c> (<c>0x0058d370</c>):`

### `src/JXReborn.Adapter.Legacy/Packets/LadderResultS2CPacket.cs`
- **L9**: `/// Verified against <c>jx_linux_y</c> <c>KProtocolProcess::LadderQuery</c> (<c>0x080f19de</c>)`
- **L10**: `/// and <c>game.exe</c> (<c>0x005571d0..0x0055723c</c> / <c>0x004bc746</c>):`

### `src/JXReborn.Adapter.Legacy/Packets/MsgShowS2CPacket.cs`
- **L7**: `/// (<c>msgId=0x000A</c> silver lost on death with <c>int32</c> param @ <c>game.exe 0x00555cd1..0x0055648a</c>,`
- **L8**: `/// <c>0x0023</c> PK cooldown @ <c>0x00556229</c>, <c>0x002B</c> cannot PK @ <c>0x00556305</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/NpcRemoveS2CPacket.cs`
- **L8**: `/// from the client's local <c>KNpcSet</c> when it leaves the <c>3x3</c> AOI or disconnects.`

### `src/JXReborn.Adapter.Legacy/Packets/NpcSitS2CPacket.cs`
- **L6**: `/// <c>s2c_npcsit</c> (<c>0x83</c>, <c>5</c> bytes, <c>&lt;BI</c>): notifies <c>3x3</c> AOI observers that <c>NpcId</c> sat down (<c>KNpc::DoSit</c> @ <c>0x08096cc0</c>, 'FR-007').`

### `src/JXReborn.Adapter.Legacy/Packets/NpcSyncStateInfoS2CPacket.cs`
- **L9**: `/// (<c>jx_linux_y</c> <c>KNpc::BroadCastState</c> @ <c>0x08091150</c>, <c>game.exe</c> <c>0x00555770</c> -&gt; <c>0x00578b10</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/ObjAddS2CPacket.cs`
- **L50**: `// KObjSet::Add (0x080bd9a0): Object[idx].m_szName is char[32]; names >= 32 bytes truncate to 28 bytes + "..." + '\0'`

### `src/JXReborn.Adapter.Legacy/Packets/OpenDiceGameS2CPacket.cs`
- **L8**: `/// emitted by <c>LuaOpenDiceGame</c> (<c>0x08126a50</c>) and <c>KDiceGame::ProcessGame</c> (<c>0x082019f0</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/OpenStoreBoxS2CPacket.cs`
- **L5**: `/// Canonical wire layout (<c>jx_linux_y</c> <c>LuaOpenBox</c> @ <c>0x0813ef7d</c> &amp; <c>LuaOpenStoreBox</c> @ <c>0x081398cb</c>):`

### `src/JXReborn.Adapter.Legacy/Packets/PlayerBuyItemC2SPacket.cs`
- **L7**: `/// Canonical wire layout (<c>game.exe</c> <c>0x0058d620</c> &amp; <c>jx_linux_y</c> <c>KPlayer::BuyItem</c> @ <c>0x080c6b85</c>):`

### `src/JXReborn.Adapter.Legacy/Packets/PlayerCommunityChannelChatS2CPacket.cs`
- **L9**: `/// delivers Relay channel chat matching <c>game.exe</c> <c>0x00557ef4</c>.`

### `src/JXReborn.Adapter.Legacy/Packets/PlayerCommunityChatC2SPacket.cs`
- **L8**: `/// <c>c2s_playercommunityextend</c> (<c>0xFB</c>): parses client Relay chat commands emitted by <c>game.exe</c> UI`
- **L9**: `/// (<c>sub=0x20</c> whisper @ <c>0x0044d6bc</c>, <c>sub=0x21</c> channel chat @ <c>0x0044d4f9</c>,`
- **L10**: `/// <c>sub=0x2D</c> group chat @ <c>0x0044d437</c>).`
- **L151**: `// 0x0044d59e prepends [0x1B, colorByte] when color filtering is enabled`

### `src/JXReborn.Adapter.Legacy/Packets/PlayerCommunityWhisperS2CPacket.cs`
- **L9**: `/// delivers Relay private whispers matching <c>game.exe</c> <c>0x00557eae</c>.`

### `src/JXReborn.Adapter.Legacy/Packets/PlayerLevelUpS2CPacket.cs`
- **L11**: `/// unallocated attribute/skill points, split 64-bit experience, and base vital maximums (<c>KPlayer::SyncLevelUp</c> @ <c>0x080bfd50</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/PlayerPortraitSyncS2CPacket.cs`
- **L7**: `/// Verified against <c>jx_linux_y</c> <c>KPlayer::SetPortrait</c> (<c>0x080c31e3</c>):`

### `src/JXReborn.Adapter.Legacy/Packets/ProtocolOpcodes.cs`
- **L71**: `public const byte c2s_ladderquery = 0x79;                    // 121 (8B — KProtocolProcess::LadderQuery @ 0x080f1990)`
- **L78**: `public const byte c2s_setportrait = 0x7E;                    // 126 (9B — KProtocolProcess::SetPortrait @ 0x080f1b70)`
- **L80**: `public const byte c2s_enchaseritem = 0x7F;                   // 127 (50B — KProtocolProcess::EnchaserItem)`
- **L84**: `public const byte c2s_citywarextend = 0x82;                  // 130 (Var — KProtocolProcess::c2sCityWarExtend @ 0x080f1680)`
- **L94**: `public const byte c2s_foundry_request = 0x92;                // 146 (204B — KProtocolProcess::c2sFoundryRequest)`
- **L96**: `public const byte c2s_TongExtend = 0xFD;                     // 253 (Var — KClientProcess::ProcessPlayerTongMsg @ 0x081f6020)`
- **L158**: `public const byte s2c_chatdeletefriend = 0x72;               // 114 (5B — KPlayerChat::DeleteFriendData @ 0x081df2a0)`
- **L159**: `public const byte s2c_chatfriendoffline = 0x73;              // 115 (5B — KPlayerChat::GetMsgOffLine @ 0x081dee80)`
- **L160**: `public const byte s2c_tradechangestate = 0x75;               // 117 (6B — KPlayerMenuState::SetState @ 0x080d9d60)`
- **L161**: `public const byte s2c_syncmenustate = 0x76;                  // 118 (Var — KPlayerMenuState::SetState @ 0x080d9d60)`
- **L162**: `public const byte s2c_trademoneysync = 0x77;                 // 119 (5B — KPlayer::SyncTradeState @ 0x080bfc50)`
- **L163**: `public const byte s2c_tradedecision = 0x78;                  // 120 (2B — KPlayer::CancelTrade @ 0x080c2740 / TradeDecision @ 0x080ce328)`
- **L164**: `public const byte s2c_npcsyncstateinfo = 0x7A;               // 122 (11B — KNpc::BroadCastState @ 0x08091150)`
- **L167**: `public const byte s2c_playerlevelup = 0x80;                  // 128 (130B — KPlayer::SyncLevelUp @ 0x080bfd50)`
- **L168**: `public const byte s2c_tradepressoksync = 0x81;               // 129 (5B — KPlayer::SyncTradeState @ 0x080bfc50)`
- **L170**: `public const byte s2c_npcsit = 0x83;                         // 131 (5B — KNpc::DoSit @ 0x08096cc0)`
- **L178**: `public const byte s2c_tradeapplystart = 0x8B;                // 139 (9B — KPlayer::TradeApplyStart @ 0x080c4330)`
- **L191**: `public const byte s2c_ladderresult = 0x96;                   // 150 (466B — LADDER_DATA top 10 @ 0x080f19de)`
- **L192**: `public const byte s2c_ladderqueryresult = 0x96;              // 150 (466B — LADDER_DATA top 10 @ 0x080f19de)`
- **L193**: `public const byte s2c_showladder = 0x97;                     // 151 (Var 7 + 4*N — LuaShowLadder @ 0x081236e0)`
- **L194**: `public const byte s2c_setportrait = 0x9F;                    // 159 (9B — KPlayer::SetPortrait @ 0x080c31e3)`
- **L195**: `public const byte s2c_battlefieldstatus = 0xB0;              // 176 (Var — KBattle::BroadCastGameData @ 0x0815dfc0)`
- **L197**: `public const byte s2c_citywarextend = 0xA3;                  // 163 (77B — LuaAskClientForNumber @ 0x08138c50)`
- **L199**: `public const byte s2c_tongcreate = 0x98;                     // 152 (51B — KPlayerTong::Create @ 0x080e9174)`
- **L200**: `public const byte s2c_foundry_result = 0xA0;                 // 160 (7B — KPlayer::SendFoundryResult)`
- **L202**: `public const byte s2c_syncboxparam = 0xA6;                   // 166 (5B — KItemList::SyncBoxParam @ 0x08075d40)`
- **L204**: `public const byte s2c_syncnpcfeature = 0xAD;                 // 173 (22B — KNpc::SyncFeatureData @ 0x080916a0)`
- **L207**: `public const byte s2c_syncpos = 0xC5;                        // 197 (17B — KNpc::SyncPos @ 0x0809b020)`
- **L208**: `public const byte s2c_syncmemfile = 0xC9;                    // 201 (Var — KFileSyncToolGS::SyncFilesToClient @ 0x08196790 -> KBuySell::OnFileChanged @ 0x005b8030)`
- **L209**: `public const byte s2c_itembindsync = 0xCA;                   // 202 (9B — KItemList::SyncItemBindState @ 0x08076390)`
- **L210**: `public const byte s2c_syncresist = 0xCC;                     // 204 (102B — KPlayer::SyncResist @ 0x080c0100)`

### `src/JXReborn.Adapter.Legacy/Packets/RelayChannelIdResultS2CPacket.cs`
- **L9**: `/// (<c>sub=0x22</c> @ <c>s3relay_y 0x08052bdb</c> / <c>game.exe 0x005566a0</c>).`
- **L168**: `/// <c>1..7</c> only when a Tong Master holds a city per <c>s3relay_y CChannelMgr::MarkViceroy</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/RelayChannelQueryC2SPacket.cs`
- **L7**: `/// <c>c2s_extend</c> (<c>0xFA</c>): Relay channel-ID query (<c>sub=0x21</c>, <c>37</c>B @ <c>game.exe 0x00490ec0</c>)`
- **L8**: `/// and channel subscribe/unsubscribe (<c>sub=0x24</c>, <c>13</c>B @ <c>game.exe 0x00490ff0</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/RequestPlayerC2SPacket.cs`
- **L7**: `/// <c>c2s_requestplayer</c> (<c>0x48</c>, 5-byte legacy or variable-length <c>gInitServerProtocolSize[0x48] == -1</c>):`

### `src/JXReborn.Adapter.Legacy/Packets/RoleListResultS2CPacket.cs`
- **L25**: `// Use 2-byte inner header (RoleCount, RefuseCount = 0) + 35B * RoleCount so both game.exe and JX-Swarm parse identically.`

### `src/JXReborn.Adapter.Legacy/Packets/SetPortraitC2SPacket.cs`
- **L7**: `/// Verified against <c>jx_linux_y</c> <c>KProtocolProcess::c2sNotify</c> (<c>0x080f1b70</c>):`

### `src/JXReborn.Adapter.Legacy/Packets/ShowLadderS2CPacket.cs`
- **L8**: `/// emitted by <c>LuaShowLadder</c> (<c>0x081236e0</c>) in <c>jx_linux_y</c>.`

### `src/JXReborn.Adapter.Legacy/Packets/StallViewItemSerializer.cs`
- **L12**: `/// matching <c>KPlayerStall::GetStallInfo</c> (<c>0x080e1ae0</c>) and <c>gMakeSyncItem_Info</c> (<c>0x08075240</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/SyncBoxParamS2CPacket.cs`
- **L5**: `/// Canonical wire layout (<c>jx_linux_y</c> <c>KItemList::SyncBoxParam</c> @ <c>0x08075d40</c>):`

### `src/JXReborn.Adapter.Legacy/Packets/SyncItemBindStateS2CPacket.cs`
- **L9**: `/// synchronizes an item's soulbind / lock state to <c>game.exe</c> (<c>KItemList::SyncItemBindState</c> @ <c>0x08076390</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/SyncItemS2CPacket.cs`
- **L11**: `/// synchronizes a single item instance in the player's inventory (<c>KItemList::SyncItem</c> @ <c>0x080786c0</c>,`
- **L12**: `/// <c>gMakeSyncItem_Info</c> @ <c>0x08075240</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/SyncMemFileS2CPacket.cs`
- **L7**: `/// <c>KFileSyncCenterClient::OnSyncFiles</c> (<c>0x005f01c0</c>) and`
- **L8**: `/// <c>gOnUpdateFinishForMemFileManager</c> (<c>KBuySell::OnFileChanged</c> at <c>0x005b8030</c>)`

### `src/JXReborn.Adapter.Legacy/Packets/SyncNpcFeatureS2CPacket.cs`
- **L10**: `/// (<c>KNpc::SyncFeatureData</c> @ <c>0x080916a0</c>, <c>KNpc::Init</c> @ <c>0x08093b6e</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/SyncNpcMinS2CPacket.cs`
- **L8**: `/// compact neighbor position/posture/vital sync (<c>KNpc::NormalSync</c> @ <c>0x08098af0</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/SyncNpcPosS2CPacket.cs`
- **L7**: `/// 17-byte <c>s2c_syncpos</c> (<c>0xC5</c>, <c>NPC_POS_SYNC</c> / <c>KNpc::SyncPos</c> @ <c>0x0809b020</c>) packet`
- **L8**: `/// that unconditionally snaps an entity's <c>(MapMpsY, MapMpsX)</c> coordinates on <c>game.exe</c> (<c>0x0055D970</c>)`

### `src/JXReborn.Adapter.Legacy/Packets/SyncPlayerMinS2CPacket.cs`
- **L9**: `/// real-time player equipment appearance and speed updates (<c>KNpc::NormalSync</c> @ <c>0x080982a0</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/SyncPlayerS2CPacket.cs`
- **L11**: `/// <c>WeaponResType</c>, <c>HorseResType</c>) from <c>KNpc::SendSyncData</c> (<c>0x080994b0</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/SyncResistS2CPacket.cs`
- **L9**: `/// matching <c>KPlayer::SyncResist</c> (<c>jx_linux_y 0x080c0100</c>) and <c>game.exe 0x0056b580</c>.`

### `src/JXReborn.Adapter.Legacy/Packets/SyncStateEffectS2CPacket.cs`
- **L10**: `/// (<c>jx_linux_y</c> <c>KNpc::SetStateSkillEffect</c> @ <c>0x080a11b0</c>, <c>game.exe</c> <c>0x00556590</c> -&gt; <c>0x0057b080</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/SystemInfoExtendS2CPacket.cs`
- **L9**: `/// server system announcement matching <c>KPlayerChat::SendSystemInfo</c> (<c>jx_linux_y</c> @ <c>0x081df070</c>)`
- **L10**: `/// and <c>game.exe</c> (<c>0x00557EF4</c>) with <c>K_MESSAGE_SYSTEM_ANNOUCE_HEAD</c> (<c>"Công bố "</c>) sender ('FR-011').`

### `src/JXReborn.Adapter.Legacy/Packets/TaskOrderS2CPacket.cs`
- **L7**: `/// <c>s2c_taskorder</c> (<c>0xB6</c>, 65 bytes): synchronizes quest description / task order to client (<c>KPlayer::SendTaskOrder</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/TeamProtocolS2CPacket.cs`
- **L92**: `// game.exe 0x00567fa0 reads +4 as NEW captain NpcId and +8 as OLD captain NpcId`

### `src/JXReborn.Adapter.Legacy/Packets/TradeApplyStateCloseC2SPacket.cs`
- **L7**: `/// (<c>KPlayer::TradeApplyClose</c> @ <c>0x080c26e0</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/TradeApplyStateOpenC2SPacket.cs`
- **L9**: `/// (<c>KPlayer::TradeApplyOpen</c> @ <c>0x080c2950</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/ViewEquipC2SPacket.cs`
- **L8**: `/// (<c>KProtocolProcess::c2sViewEquip</c> @ <c>0x080f31d0</c>).`

### `src/JXReborn.Adapter.Legacy/Packets/ViewEquipS2CPacket.cs`
- **L13**: `/// (<c>KPlayer::SendEquipItemInfo</c> @ <c>0x080c2560</c>, <c>KPlayer::_FillViewItemDetailInfo</c> @ <c>0x080c0a10</c>).`

### `src/JXReborn.GameAssets/Chats/ChatCostCatalog.cs`
- **L10**: `/// <c>KServerCore::PayForSpeech</c> (<c>0x0804e920</c>), with canonical defaults if the file is absent.`

### `src/JXReborn.GameAssets/Items/ItemGenerator.cs`
- **L9**: `/// Deterministic C# port of <c>KItemGenerator::Gen_Equipment</c>, <c>Gen_Medicine</c>, <c>Gen_TownPortal</c>,`
- **L324**: `/// Rolls up to 6 magic affix slots (<c>0..5</c>) matching <c>KItemGenerator::Gen_MagicAttrib</c>:`

### `src/JXReborn.GameAssets/PlayerTemplates/PkRulesTemplate.cs`
- **L23**: `/// Canonical high-level experience loss ceiling for Level 130+ indexed by PK value 0..10 (<c>TABLE_PUNISHEXP</c> @ <c>0x082506c0</c>).`

### `src/JXReborn.Primitives/Characters/PlayerTaskConsts.cs`
- **L29**: `// --- Travel Bookmarks (201..219, KPlayer::SavePlayerTaskList @ 0x080d57e0) ---`

### `src/JXReborn.Primitives/Chats/ChatConsts.cs`
- **L21**: `public const ushort DisabledChatTaskId = PlayerTaskConsts.DisabledChatFlags; // 0x87 (KPlayer::GetDisabledFuncFlag @ 0x080c05e0)`
- **L38**: `/// Maps <see cref="ChatChannelKind"/> (<c>1..8</c>) to the exact <c>jx_linux_y</c> <c>g_dwChannelOr</c> (<c>0x082b6ac0</c>) bitmask.`

### `src/JXReborn.Primitives/Combat/DamageChannelKind.cs`
- **L4**: `/// Six combat damage channels (<c>DAMAGE_TYPE</c> in <c>KNpc.cpp</c>).`

### `src/JXReborn.Primitives/Combat/EntityRelationKind.cs`
- **L6**: `/// Entity relationship flags (<c>NPC_RELATION</c> in <c>KNpcSet.h</c>).`

### `src/JXReborn.Primitives/Combat/FiveElementsSeriesMatrix.cs`
- **L7**: `/// and conquest (<c>Tương Khắc</c> / <c>g_nConquerSeries</c>) rules from <c>KMath.cpp</c>.`

### `src/JXReborn.Primitives/Combat/MissileMoveKind.cs`
- **L4**: `/// Projectile movement archetype (<c>MISSLE_MOVEKIND</c> in <c>KMissle.cpp</c> / <c>skill-catalog.schema.json</c>).`

### `src/JXReborn.Primitives/Combat/SixtyFourDirectionTrigTable.cs`
- **L89**: `/// matching <c>g_GetDirIndex</c> in <c>KMath.h</c>. Returns <c>-1</c> if the coordinates are identical.`

### `src/JXReborn.Primitives/Combat/SkillStyleKind.cs`
- **L4**: `/// Martial arts skill execution style (<c>SKILL_STYLE</c> in <c>SkillDef.h</c> / <c>skill-catalog.schema.json</c>).`

### `src/JXReborn.Primitives/Crafting/EquipUpgradeType.cs`
- **L4**: `/// Authoritative foundry / crafting operation types matching the canonical <c>EQUIP_UPGRADE_TYPE</c> enum in <c>jx_linux_y</c>.`

### `src/JXReborn.Primitives/Crafting/FoundryResultType.cs`
- **L4**: `/// Authoritative result codes returned by the foundry engine matching canonical <c>FOUNDRY_RESULT_TYPE</c> in <c>jx_linux_y</c>.`

### `src/JXReborn.Primitives/Encodings/LegacyPasswordHasher.cs`
- **L9**: `/// matching <c>KSG_Password</c> (<c>char szPassword[64]</c>).`

### `src/JXReborn.Primitives/Encodings/Tcvn3Encoding.cs`
- **L141**: `/// over a null-terminated TCVN3 byte sequence without case folding (used for <c>KPlayer::m_dwID</c> and chat source IDs).`

### `src/JXReborn.Primitives/Guilds/GuildConsts.cs`
- **L5**: `/// Extracted from tongset.ini, tong_setting.ini, and GameDataDef.h.`

### `src/JXReborn.Primitives/Items/EquipmentPartKind.cs`
- **L4**: `/// Character equipment slot parts (<c>itempart_*</c> in <c>KItemList.h</c>).`

### `src/JXReborn.Primitives/Items/GroundObjectKind.cs`
- **L4**: `/// Ground object classification (<c>Obj_Kind_*</c> in <c>KObj.h</c> / <c>item-catalog.schema.json</c>).`

### `src/JXReborn.Primitives/Items/ItemContainerPlace.cs`
- **L4**: `/// Inventory container placement (<c>ITEM_POSITION</c> / <c>pos_*</c> in <c>KItemList.h</c>).`

### `src/JXReborn.Primitives/Items/ItemGenreKind.cs`
- **L4**: `/// Item genre classification (<c>item_genre</c> in <c>KItem.h</c> / <c>item-catalog.schema.json</c>).`

### `src/JXReborn.Primitives/Shops/ShopConsts.cs`
- **L4**: `/// NPC shop constants verified from <c>jx_linux_y</c> and <c>KBuySell.cpp</c>.`

### `src/JXReborn.Primitives/Stalls/PlayerMenuStateKind.cs`
- **L4**: `/// Legacy player menu states (<c>PLAYER_MENU_STATE</c> in <c>KPlayerMenuState::SetState</c> @ <c>0x080d9d60</c>`
- **L5**: `/// and <c>game.exe</c> <c>s2c_syncmenustate</c> @ <c>0x0055b440</c>).`

### `src/JXReborn.Primitives/Storage/StorageBoxOperationKind.cs`
- **L5**: `/// Canonical jump table (<c>game.exe</c> <c>0x0054ad20..0x0054ae20</c> &amp; <c>jx_linux_y</c> <c>c2sBoxOperate</c> @ <c>0x080f1580</c> / <c>0x0825183c</c>):`

### `src/JXReborn.Primitives/WorldZones/MovementAntiCheatConsts.cs`
- **L4**: `/// Implements the exact <c>KPlayer::IsCheckValueValid</c> (<c>0x080bf0a0</c>) anti-cheat checksum verification`
- **L50**: `/// Clamps negative walk/run target coordinates to zero per <c>jx_linux_y</c>`
- **L51**: `/// <c>KProtocolProcess::NpcWalkCommand</c> (<c>0x080f2910</c>) and <c>NpcRunCommand</c> (<c>0x080f2850</c>).`

### `src/JXReborn.Scripts/Items/TestGmTokenItemScript.cs`
- **L20**: `/// symmetrical 1-to-1 with <c>\script\global\nobitaxd\item\camnang_gm.lua</c> generated by`
- **L26**: `public const string GmTokenScriptPath = @"\script\global\nobitaxd\item\camnang_gm.lua";`

### `src/JXReborn.ZoneEngine/Bosses/WorldBossAttributionSession.cs`
- **L14**: `/// Evaluates top damager (party vs individual) upon boss death according to KNpcDeathCalcExp rules.`

### `src/JXReborn.ZoneEngine/Combat/ActiveMissileSlot.cs`
- **L9**: `/// Pre-allocated pooled projectile instance stepped by <c>MissileSimulationTickSystem</c> at 18 Hz (<c>KMissle</c>).`

### `src/JXReborn.ZoneEngine/Combat/AutoCastTriggerState.cs`
- **L7**: `/// Tracks registered <see cref="AutoCastSkillEntry"/> procs and per-target cooldown gates (<c>KSkillAutoCast</c>).`

### `src/JXReborn.ZoneEngine/Combat/CombatDamageCalculator.cs`
- **L13**: `/// Implements canonical combat relation checks (<c>KNpcSet::GetRelation</c>), skill damage payload assembly`
- **L14**: `/// (<c>KNpc::AppendSkillEffect</c> / <c>KSkill::CreateMissleMagicAttribsLink</c>), hit rating resolution`
- **L15**: `/// (<c>KNpc::CheckHitTarget</c>), resistance calculation (<c>KNpc::CalcCurRes</c> / <c>KNpc::ModifyAllMaxRes</c>),`
- **L16**: `/// 6-channel damage mitigation &amp; return (<c>KNpc::ReceiveDamage</c> / <c>KNpc::CalcDamage</c>),`
- **L17**: `/// and hurt recoil (<c>KNpc::DoHurt</c>) matching <c>jx_linux_y</c>.`
- **L207**: `/// Weighted poison damage blending (<c>KNpcAttribModify::MixPoisonDamage</c> @ <c>0x080b07f0</c>).`
- **L236**: `/// Computes <c>addskilldamage1..6</c> + <c>skill_enhance</c> percentage bonus (<c>KSkill::CreateMissleMagicAttribsLink</c> @ <c>0x081034d0</c>).`
- **L295**: `/// (<c>KNpc::AppendSkillEffect</c> @ <c>0x08092d30</c>, <c>CalcPhysicsAttribDamage</c> @ <c>0x080925f0</c>,`
- **L296**: `/// <c>CalcCold/Fire/Light/PoisonAttribDamage</c> @ <c>0x08092810..0x08092c10</c>).`
- **L312**: `// KPlayer::SetNpcPhysicsDamage (0x080c38a0) & SetFirstDamage (0x080bf4e0)`
- **L506**: `// 1. CalcPhysicsAttribDamage (0x080925f0)`
- **L536**: `// 2. CalcColdAttribDamage (0x08092810)`
- **L568**: `// 3. CalcFireAttribDamage (0x08092c10)`
- **L593**: `// 4. CalcLightAttribDamage (0x08092960)`
- **L620**: `// 5. CalcPoisonAttribDamage (0x08092a90) & MixPoisonDamage (0x080b07f0)`
- **L697**: `// 6. magicdamage_v (0x08092d30:72-81)`
- **L701**: `// 7. AttackRating (0x08092d30:27)`
- **L709**: `// 8. IsPhysical gating (0x08092d30:82-135)`
- **L980**: `/// (<c>KNpc::CalcCurRes</c> @ <c>0x08090320</c>). Negative resists are returned unmodified for damage amplification.`
- **L994**: `/// Evaluates <c>KNpc::DoHurt</c> (<c>0x08099270</c>) recoil probability and frame duration.`
- **L1034**: `/// Resolves full <c>KNpc::ReceiveDamage</c> (<c>0x080a1dd0</c>) + <c>KNpc::CalcDamage</c> (<c>0x0809e790</c>) against <paramref name="defender"/>.`
- **L1532**: `/// Resolves a single periodic Poison DoT tick through <c>KNpc::CalcDamage(..., damage_poison, bStateHurt: true)</c>.`
- **L1884**: `/// Evaluates <c>seriesdamage_p</c> + <c>CalcCurRes</c> semantics matching <c>jx_linux_y</c>.`

### `src/JXReborn.ZoneEngine/Combat/CombatHitResolutionResult.cs`
- **L4**: `/// Full outcome of a single <c>KNpc::ReceiveDamage</c> / <c>KNpc::CalcDamage</c> hit resolution.`

### `src/JXReborn.ZoneEngine/Combat/EntityCombatModifiers.cs`
- **L544**: `// "earthenhance_p", "candetonate1..3", "skill_append1..2" handled elsewhere / no-op in jx_linux_y`

### `src/JXReborn.ZoneEngine/Combat/EntityStatusEffectState.cs`
- **L65**: `/// Blends incoming Poison DoT with any active Poison DoT using the canonical formula from <c>KNpc::AddPoisonState</c> / <c>KNpc::ReceiveDamage</c> (<c>jx_linux_y @ 0x08091c10</c>):`

### `src/JXReborn.ZoneEngine/Combat/MissileSimulationTickSystem.cs`
- **L15**: `/// Deterministic 18-Hz projectile pool and trajectory simulation tick system (<c>KMissle</c> &amp; <c>KSkill</c>).`
- **L293**: `// CastWall (0x08106850): HIWORD(Param2) != 0 centers on caster; LOWORD(Param2) != 0 fires perpendicular (forward toward target)`
- **L402**: `// CastZone (0x08107340): NxN cell grid with optional circle clip when IsBaseSkill && Param1 == 1`
- **L607**: `// OnStartEvent (0x08107780)`
- **L1074**: `// KMissle::Activate (0x0808de30): camp/PK change triggers DoVanish(0) without OnVanishSkillId.`
- **L1140**: `// KMissle::CreateMissle -> DoWait -> next-tick KMissle::Activate -> DoFly (0x0808de30) +`
- **L1141**: `// KMissle::OnFly (0x0808d8e0) CheckCollision BEFORE StepFly:`
- **L1142**: `// aligns server damage & NpcLifeSync with game.exe's local AnimFile4 explosion frame.`
- **L1279**: `// Height gate (KMissle::CheckCollision 0x0808ca60): requires 1 <= CurrentZ <= 20`

### `src/JXReborn.ZoneEngine/Combat/SkillStateEffectApplier.cs`
- **L11**: `/// Centralizes <c>KNpc::SetStateSkillEffect</c> (<c>0x080a11b0</c>), <c>RemoveStateSkillEffect</c> (<c>0x08093140</c>),`
- **L12**: `/// <c>ClearHideState</c> (<c>0x080947d0</c>), and <c>Style=3</c> passive aggregation into <c>SkillCombatModifiers</c>.`

### `src/JXReborn.ZoneEngine/Configuration/ZoneEngineOptions.cs`
- **L45**: `/// (<c>AttackFrame=18</c>), used by <c>KNpc::DoOrdinSkill</c> + <c>KNpc::OnSkill</c> (<c>IsReachFrame(60)</c>).`
- **L51**: `/// (<c>CastFrame=18</c>), used by <c>KNpc::DoOrdinSkill</c> + <c>KNpc::OnSkill</c> (<c>IsReachFrame(60)</c>).`

### `src/JXReborn.ZoneEngine/Items/GoldenPlatinaEquipResCatalog.cs`
- **L12**: `/// (<c>KItemChangeRes::LoadEquipRes</c> @ <c>0x080710b0</c>, <c>GetEquipRes</c> @ <c>0x08070cf0</c>,`
- **L13**: `/// <c>GetPlatinaEquipRes</c> @ <c>0x08070d80</c>, <c>KItemList::GetFeatureType</c> @ <c>0x08077410</c>).`

### `src/JXReborn.ZoneEngine/Items/GroundObjectTickSystem.cs`
- **L194**: `/// (<c>267..271</c>, <c>Kind = Money</c>). <c>DataID 1..8</c> are <c>Prop</c>/<c>Box</c> entries in <c>game.exe</c>.`
- **L255**: `// 1. Evaluate money drop per canonical KNpc::LoseMoney formula: (MoneyScale * m_nMoneyNum) / 100`
- **L492**: `// game.exe KObjSet::FindID (0x005ad820) reserves IDs 1..999 for client-local map props and rejects < 1000.`

### `src/JXReborn.ZoneEngine/Items/ItemForbidRuleEntry.cs`
- **L4**: `/// Single rule entry from <c>server1/settings/forbititem.ini</c> (<c>KForbitItemBase::TItemInfo</c> @ <c>0x0806d0e0</c>).`

### `src/JXReborn.ZoneEngine/Items/ItemForbidRulesCatalog.cs`
- **L7**: `/// (<c>KForbitItemBase::LoadFromFile</c> @ <c>0x0806d0e0</c>, <c>KItem::ModifyBrokenFlag</c> @ <c>0x0806f5b0</c>,`
- **L8**: `/// <c>KPlayer::ServerThrowAwayItem</c> @ <c>0x080ccce0</c>).`
- **L276**: `// Map C# ItemQualityKind.Platina (3) -> jx_linux_y equip_platina (4)`

### `src/JXReborn.ZoneEngine/Items/LegacyEquipmentResResolver.cs`
- **L9**: `/// matching <c>KItemChangeRes::GetArmorRes</c>, <c>GetHelmRes</c>, <c>GetWeaponRes</c>, and <c>GetHorseRes</c> (<c>0x08070df0..0x080710aa</c>)`

### `src/JXReborn.ZoneEngine/Items/MagicAttribLevelIndexCatalog.cs`
- **L12**: `/// <c>settings/item/004/magicattriblevel_index.txt</c> and <c>KItemGenerator::Gen_EnchasableEquipment</c> (<c>0x08073120</c>).`

### `src/JXReborn.ZoneEngine/Items/PlayerInventoryGrid.cs`
- **L21**: `/// equipment requirement validation, and <c>KItemList::ms_ActivedEquip</c> Five-Elements hidden affix activation ('FR-012').`
- **L28**: `/// Ground-truth <c>KItemList::ms_ActivedEquip[part][0..1]</c> table from <c>KItemList.cpp</c> mapping each equipment part (<c>0..9</c>)`
- **L661**: `/// Validates <c>KItemList::ExchangeItemHand</c> (<c>0x0807e180</c>) container-level access rules:`
- **L1059**: `/// Evaluates <c>KItemList::ms_ActivedEquip</c> Five-Elements (<c>Tương Sinh</c>) hidden affix activation across all equipped items`

### `src/JXReborn.ZoneEngine/Items/PlayerItemSlot.cs`
- **L112**: `/// Ground label color index matching <c>KObjSet::MakeObjItemInfo</c> (<c>0x080be550</c>):`
- **L123**: `/// Effective wire/forbid genre matching <c>KItem::ModifyBrokenFlag</c> (<c>0x0806f5b0</c>):`
- **L132**: `/// Effective cell width matching <c>KItem::Trans2BrokenEquip</c> (<c>0x0806eac0</c>): <c>1x1</c> when broken.`
- **L140**: `/// Effective cell height matching <c>KItem::Trans2BrokenEquip</c> (<c>0x0806eac0</c>): <c>1x1</c> when broken.`

### `src/JXReborn.ZoneEngine/PlayerSessions/PlayerEquipmentInspectSnapshot.cs`
- **L10**: `/// (<c>KPlayer::SendEquipItemInfo</c> @ <c>0x080c2560</c>, <c>KPlayer::_FillViewItemDetailInfo</c> @ <c>0x080c0a10</c>).`

### `src/JXReborn.ZoneEngine/PlayerSessions/PlayerZoneState.cs`
- **L250**: `// Equipment sprite appearance indices (KItemChangeRes::GetDefaultRes)`

### `src/JXReborn.ZoneEngine/PlayerSessions/ViewPlayerEquipmentZoneCommand.cs`
- **L8**: `/// (<c>KProtocolProcess::c2sViewEquip</c> @ <c>0x080f31d0</c>).`

### `src/JXReborn.ZoneEngine/Storage/RepositoryBoxCommandHandler.cs`
- **L144**: `// KItemList::PlayerSetPassword (0x08078970): requires ExBox1 != 0 and HasPwd == 0`
- **L159**: `// KItemList::PlayerCanclePassword (0x0807a900): requires HasPwd != 0`
- **L183**: `// KItemList::PlayerChangePassword (0x0807a820): requires ExBox1 != 0 and HasPwd != 0`
- **L206**: `// KItemList::PlayerLock (0x08078a30): requires ExBox1 != 0 and HasPwd != 0`
- **L219**: `// KItemList::PlayerUnlock (0x0807a780): requires HasPwd != 0`

### `src/JXReborn.ZoneEngine/Trades/TradeCommandHandler.cs`
- **L115**: `// jx_linux_y KPlayer::TradeApplyStart (0x080c4330): invitee (playerB) must have TradeOpen (MenuState == 2);`
- **L191**: `// jx_linux_y KPlayer::c2sTradeReplyStart (0x080cbbb0) + KPlayerMenuState::SetState (0x080d9d60 case 3):`
- **L431**: `// jx_linux_y KPlayer::TradeDecision (0x080ce328) + game.exe s2c_tradedecision (0x0056b3b0):`

### `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`
- **L1000**: `// Per jx_linux_y KNpc::DoRun (0x08097060): unmounted players with stamina below PK-mode threshold fall back to DoWalk.`
- **L1200**: `// 0x46 (s2c_synccurplayernormal) drives game.exe's m_nServerLoop++ clock in KProtocolProcess::ProcessMsg;`
- **L1266**: `// Periodic KRegion::Activate NormalSync (0x4D) every 90 ticks (~5s) to refresh game.exe m_SyncSignal (< 1000 frames)`
- **L1687**: `// Matches server1/settings/npc/player/stamina.ini & KNpc::ProcessState (0x0809fef4):`
- **L2233**: `// Compute effective cast cooldown & global KNpc::DoOrdinSkill (0x0809c160) + KNpc::OnSkill (0x0809bdb0)`
- **L2252**: `// Enforce full KNpc::DoOrdinSkill animation lock (nTotalFrame) in addition to WaitFrames / MinFramesPerCast`
- **L2281**: `// KNpc::DoSkill (0x0809c43c): reveal stealth before broadcasting 0x5A`
- **L3845**: `/// matching <c>KItemList::Abrade</c> (<c>0x0807d830</c>) and <c>KItem::ModifyBrokenFlag</c> (<c>0x0806f5b0</c>).`
- **L4005**: `// KProtocolProcess::ObjMouseClick (0x080f4500..0x080f46dd):`
- **L4192**: `// KPlayer::ServerMoveItem (jx_linux_y @ 0x080cc0f3):`
- **L4193**: `// KItemList::ExchangeItemHand on both server and game.exe requires DownPos.Place == UpPos.Place`
- **L4528**: `// KPlayer::ThrowAwayMedicineFromEquipmentRoom (0x080c1b30): only throw unbound, non-forbidden medicine in EquipRoom (never Repository/ExpandBox!)`
- **L4569**: `// KPlayer::ServerThrowAwayItem (0x080ccce0): reject bound items and forbititem.ini ForbitThrowAway=1 items with up to 10 tips`
- **L4585**: `// ServerThrowAwayItem (0x080ccce0 lines 228-235): SetEntireBelong when ForbitTrade != 0 or QuestKey ParticularType <= 0`
- **L4620**: `// KPlayer::ServerDivideItem (0x080c2cb0): disallow while stalling (m_nStallState >= 1)`
- **L4635**: `// KItemList::DivideItem (0x0807d370): check PIN lock and repository access for Place 2, 4, 9, 10, 11`
- **L4738**: `// KItem::CanBeRepaired (0x0806e9e0): broken equipment (Genre = 7) cannot be repaired via normal blacksmith hammer`
- **L4789**: `// KItemList::EatMecidine (0x0807c830): game.exe sends client Item[] index (not m_dwID) at +0x02 for genre==1 medicines,`
- **L4813**: `// jx_linux_y potion.txt cadence: 10 frames per tick (e.g. Cuu Chuyen duration=100, min=500 -> 5000 HP; Ngung Than trung duration=200, min=10 -> t+10/t+20/t+30).`
- **L5116**: `// KObjSet::Add (0x080bd9a0): newly spawned/dropped items & silver set SoundFlag=1 | MovieFlag=2 -> 0x03`
- **L5154**: `// Legacy KNpc::DoDeath / KPlayer::Revive (0x080c7890): normal death waits for c2s_npcrevive (0x74)`
- **L5347**: `// Canonical PvE KNpc::DeathPunish: 2% (<=Lv10) or 3% (>Lv10) EXP loss capped at 130,000; 50% carried silver lost (25% dropped on ground) + 0x86 msgId=10`
- **L5574**: `// Legacy KPlayer::Revive (0x080c7890): if client sends c2s_npcrevive (0x74) while already alive,`
- **L5575**: `// emit a stand/position recovery sync instead of silently dropping so game.exe never stays stuck in do_death.`
- **L5628**: `// Legacy KPlayer::Revive (0x080c7890): ALWAYS send 0x89 (s2c_playerrevive) + restored vitals`
- **L5629**: `// BEFORE KNpc::ChangeWorld (0x49 s2c_syncworld) so game.exe exits do_death and closes the`
- **L5677**: `// game.exe's 0x5A handler runs KSkillList::CanCast(skillId, time, npc.Level) and aborts if ReqLevel > npc.Level`
- **L6121**: `// If target had a prior Enmity with someone else, KPlayerPK.cpp:180 closes it before binding the new one`
- **L6652**: `// OnStartEvent runs after ReceiveDamage + SetStateSkillEffect matching KSkill::CastInitiativeSkill (0x08107a10)`
- **L7581**: `ScriptCode: @"\script\global\nobitaxd\item\camnang_gm.lua",`
- **L7695**: `// KProtocolProcess::c2sViewEquip (0x080f31d0): 0x1D (29) frame cooldown`
- **L7712**: `// KPlayer::SendEquipItemInfo (0x080c2560): PK < 5 -> 0, else clamp [5..10]`

### `src/JXReborn.ZoneEngine/WorldZones/PlayerTravelBookmarkHelper.cs`
- **L10**: `/// from <c>jx_linux_y</c> <c>KPlayer::SavePlayerTaskList</c> (<c>0x080d57e0</c>):`

## `tools/` (3 lines in 3 files)

### `tools/JXReborn.DataExporter/Legacy/HybridLegacyTextDecoder.cs`
- **L537**: `(-15701, "lu"), (-15681, "lv"), (-15667, "luan"), (-15661, "lue"), (-15659, "lun"),`

### `tools/JXReborn.DataExporter/Legacy/LegacyReplaceNameTable.cs`
- **L10**: `/// Loads legacy <c>lang/vn/replacename_npc.txt</c> (<c>KSubWorldSet::SubExecute_LoadReplaceName</c> in <c>jx_linux_y</c>)`

### `tools/JXReborn.DataExporter/Legacy/MagicAttributeRegistry.cs`
- **L11**: `/// Authoritative <c>MAGIC_ATTRIB</c> registry seeded from <c>jx_linux_y</c> <c>KMagicDesc::KMagicDesc</c> (<c>0x08088900</c>)`

## `tests/` (292 lines in 58 files)

### `tests/JXReborn.Domain.Tests/Architecture/SolutionArchitectureComplianceTests.cs`
- **L260**: `if (csprojText.Contains("g_uPublicKeys.bin", StringComparison.OrdinalIgnoreCase))`
- **L263**: `violations.Add($"[EmbeddedLegacyKeyTable] {rel}: must not embed g_uPublicKeys.bin in runtime src/.");`
- **L378**: `violations.Add($"[MissingHeadlessTrait] {rel}: real game.exe E2E/WorldJourney suites must declare [Trait(\"Category\", \"Headless\")].");`

### `tests/JXReborn.Domain.Tests/Encodings/Tcvn3EncodingTests.cs`
- **L44**: `// Every non-zero byte 0x01..0xFF received from C++ game.exe MUST survive DecodeNullTerminated -> WriteFixedString / EncodeBytes`

### `tests/JXReborn.GameAssets.Tests/Catalogs/SkillAndMissileCatalogParityTests.cs`
- **L132**: `// MoveKind=2 -> Random (jx_linux_y KMissle::StepFly 0x0808cbd0)`

### `tests/JXReborn.GameAssets.Tests/Sources/LegacyServerSourceTests.cs`
- **L35**: `WriteUstarEntry(gz, "legacy/server1/jx_linux_y", ignoredBinary);`
- **L53**: `Assert.False(source.TryReadServerFile("jx_linux_y", out _));`

### `tests/JXReborn.Integration.Tests/AuthAndCharacters/GameExeWorldEntryComplianceIntegrationTests.cs`
- **L14**: `/// End-to-end wire-compliance regression tests reproducing the exact C++ <c>game.exe</c> client flow:`
- **L68**: `// Reply with 0x43 c2s_syncend, then emit real game.exe post-sync C2S packets:`

### `tests/JXReborn.Integration.Tests/ItemsAndEconomy/NpcShopAndEconomyIntegrationTests.cs`
- **L21**: `/// (<c>server1/settings/buysell.txt</c> + <c>goods.txt</c>) and <c>KBuySell::Sell</c> (<c>0x081fbd50</c>: <c>price / 4</c>).`
- **L68**: `// - jx_linux_y KBuySell::Sell @ 0x081fbe75 -> sellCredit = 2700 / 4 = 675`
- **L118**: `// - jx_linux_y KBuySell::Sell @ 0x081fbd50 / KPlayer::SellItem @ 0x080c5f33 passes Player.m_BuyInfo.m_nBuyIdx`

### `tests/JXReborn.Integration.Tests/ItemsAndEconomy/PlayerStallMarketIntegrationTests.cs`
- **L134**: `// 3. Verify balances per jx_linux_y KPlayerStall::CalcTaxFee (0x080e2940..0x080e29c0 + 0x080e2490):`

### `tests/JXReborn.Integration.Tests/WireCaptures/LegacyGameExeExperienceReplayIntegrationTests.cs`
- **L6**: `/// Offline <c>game.exe</c> experience parity tests replaying real <c>legacy server</c> + <c>game.exe</c> wire captures`
- **L8**: `/// without requiring <c>game.exe</c> or Docker.`

### `tests/JXReborn.Integration.Tests/WireCaptures/OfflineWireCaptureReplayer.cs`
- **L12**: `/// (<c>Bishop</c> + <c>GameServer</c>) driven by a deterministic 18-Hz steppable clock (0 <c>game.exe</c>, 0 Docker, 0 <c>Task.Delay</c>).`

### `tests/JXReborn.Protocol.Tests/Bishop/BishopGoldenVectorPacketTests.cs`
- **L13**: `/// C++ <c>KProtocol.h</c> and <c>gateway/bishop_y</c> binary disassembly (Principle 1: No Echo-Chamber Self-Roundtrip).`
- **L22**: `// +1:  Size = 0x007E (2B, sizeof(KLoginAccountInfo) - 1 = 126? Note KProtocolDef: 0x007E = 126)`
- **L25**: `// +39: Password[64] (KSG_PASSWORD)`
- **L72**: `// Ground-truth C++ KProtocol.h:818-823 & bishop_y CGamePlayer::_DeleteRole_ToDBRole (0x0804cd68):`
- **L77**: `//     KSG_PASSWORD Password;    // +33  (64B)`

### `tests/JXReborn.Protocol.Tests/GameServer/ChatPacketTests.cs`
- **L89**: `// game.exe KNpc::SetChatInfo prepends [0x02, 0xFF, 0xFF, 0x00] + "<Sender>:\x03" where byte[3] is 0x00 (Blue=0).`

### `tests/JXReborn.Protocol.Tests/GameServer/EntityAndInventoryGoldenVectorPacketTests.cs`
- **L11**: `/// all expected wire byte arrays are independent literals derived from <c>jx_linux_y</c> DWARF/assembly,`
- **L86**: `// tagNPC_NORMAL_SYNC (18B fixed @ KNpc::NormalSync 0x08098af0):`
- **L93**: `// +15..+16: LifePer=128 -> scaled11=0 (full HP 2048 & 0x7FF == 0 per jx_linux_y), Doing=1 (Stand) -> +15 = 0x00, +16 = 0x01`
- **L279**: `// Corrupted Series = 0xFF (bug that caused game.exe KItemGenerator::Gen_Equipment to drop Toàn Đầu Chùy)`
- **L293**: `// 1. 0x83 s2c_npcsit (5B — KNpc::DoSit @ 0x08096cc0; MUST NOT use 0x53 which is -1 variable-length!)`
- **L317**: `// 3. 0xAD s2c_syncnpcfeature (22B — KNpc::SyncFeatureData @ 0x080916a0, KNpc::Init @ 0x08093b6e):`
- **L349**: `// 4. 0x80 s2c_playerlevelup (130B — KPlayer::SyncLevelUp @ 0x080bfd50; MUST NOT use 0x5B s2c_playertalk!):`
- **L380**: `// 5. 0x48 c2s_requestplayer: both 5B legacy and variable-length (gInitServerProtocolSize[0x48] == -1) must parse!`

### `tests/JXReborn.Protocol.Tests/GameServer/FoundryWirePacketTests.cs`
- **L17**: `/// - <c>game.exe</c> <c>GOI 61</c> (<c>0x3D</c> @ <c>0x0053cf7d..0x0053d056</c>):`
- **L20**: `///   <c>[49]=uint8(slotOneBased - 1)</c> (<c>sub al, 1; mov [ebp+0xb35], al</c> @ <c>0x0053d027</c>).`
- **L21**: `/// - <c>jx_linux_y</c> <c>KProtocolProcess::EnchaserItem</c> (<c>0x080f34c0..0x080f35ff</c>):`
- **L22**: `///   iterates 4x12B <c>ItemPos</c> slots (<c>pos_equiproom = 3</c> check @ <c>0x080f35d0</c>), reads <c>[edx+0x31]</c> (byte 49) as`
- **L23**: `///   0-based socket index whenever slot 3 (<c>[37..48]</c> Ore) is present (<c>0x080f355c..0x080f356a</c>), emits 7B <c>0xA0</c> (<c>action=8</c>).`
- **L24**: `/// - <c>jx_linux_y</c> <c>KProtocolProcess::c2sFoundryRequest</c> (<c>0x080f5b30..0x080f5d68</c>) &amp; <c>game.exe</c> <c>0x00538ae0..0x00538be4</c>:`
- **L34**: `// game.exe GOI 61 (0x0053cf7d..0x0053d056) & jx_linux_y EnchaserItem (0x080f34c0..0x080f356a):`
- **L78**: `// jx_linux_y c2sFoundryRequest (0x080f5b30):`
- **L113**: `[InlineData(9, 0, 0)]  // MainCount = 9 > 8 (jx_linux_y 0x080f5b4d: cmp dword [eax+2], 8; jg reject)`
- **L115**: `[InlineData(1, 9, 0)]  // AltCount = 9 > 8 (jx_linux_y 0x080f5b57: cmp byte [eax+0x66], 8; ja reject)`
- **L116**: `[InlineData(1, 0, 7)]  // UpgradeType = 7 > 6 (jx_linux_y 0x080f5b61: cmp byte [eax+1], 6; ja reject)`
- **L144**: `// Foundry 0x92 must be exact 204B and reject 0x82 (c2sCityWarExtend @ jx_linux_y 0x080f1680)`
- **L158**: `public void FoundryResultS2CPacket_WithLiteral7ByteGoldenVector_ShouldMatchGameExe0x00557990Layout()`
- **L160**: `// jx_linux_y 0x080f3587 / 0x080f5c7f & game.exe 0x00557990:`

### `tests/JXReborn.Protocol.Tests/GameServer/GameServerGoldenVectorAndSizeTableTests.cs`
- **L13**: `/// Golden Wire Vector &amp; binary size-table parity tests against <c>server1/jx_linux_y</c>`
- **L14**: `/// (<c>gInitServerProtocolSize</c> @ <c>0x080ef560</c>, <c>gInitClientProtocolSize</c> @ <c>0x080ef960</c>,`
- **L15**: `/// <c>KNpc::SendSyncData</c> @ <c>0x080994b0</c>).`
- **L33**: `// Ground-truth C++ KNpc::SendSyncData (0x080994b0) & LoadPlayerBaseInfo (0x080d8a80):`
- **L77**: `// In game.exe, immediately after SyncEnd (0x43), client transmits 0x8A (c2sWelcome2Server, 9B),`
- **L99**: `$"ProtocolSizeTable rejected {rejected.Count} valid jx_linux_y C2S opcodes: {string.Join(", ", rejected)}");`

### `tests/JXReborn.Protocol.Tests/GameServer/GroundObjectPacketTests.cs`
- **L42**: `// Verified against KObj::SyncAdd (0x080bca30) & KObjSet::Add (0x080bd9a0):`

### `tests/JXReborn.Protocol.Tests/GameServer/GuildWirePacketTests.cs`
- **L11**: `/// Evidence-backed literal Golden Vector and opcode fidelity tests for <c>025-Guilds</c> (<c>KPlayerTong</c>) and <c>LadderQuery</c>.`
- **L13**: `/// - <c>game.exe</c> <c>KPlayerTong::ApplyCreateTong</c> (<c>0x005b8250..0x005b83eb</c>) &amp; <c>jx_linux_y</c>`
- **L14**: `///   <c>KClientProcess::ProcessMessage</c> (<c>0x081f7774</c> -&gt; <c>0x081f6020</c>):`
- **L20**: `///   Meanwhile <c>0x9A</c> in <c>jx_linux_y</c> (<c>0x080f13f0</c>) &amp; <c>game.exe</c> (<c>0x005a6f4c</c>) is <c>c2sPartnerExtend</c> (12B), NOT Tong!`
- **L21**: `/// - <c>jx_linux_y</c> <c>KPlayerTong::Create</c> (<c>0x080e9174..0x080e91dc</c>) &amp; <c>game.exe</c> (<c>0x00556ea0</c> -&gt; <c>0x005b8620</c>):`
- **L23**: `///   NOT <c>0xA0</c> (<c>10</c>B, which collides with <c>s2c_foundry_result = 0xA0</c> 7B @ <c>0x00557990</c>)!`
- **L24**: `/// - <c>jx_linux_y</c> <c>KProtocolProcess::LadderQuery</c> (<c>0x080f1990..0x080f19de</c>):`
- **L26**: `/// - <c>game.exe</c> <c>s2c_tongexmsg = 0xC3</c> (<c>0x00556c50</c> -&gt; <c>0x005a5910</c>):`
- **L34**: `// 1. S2C 0xA0 in game.exe (0x00557990) is exclusively s2c_foundry_result (7B)`
- **L37**: `// 2. S2C s2c_tongcreate is 0x98 (51B @ jx_linux_y 0x080e9174 & game.exe 0x00556ea0), NOT 0xA0!`
- **L42**: `// 3. S2C s2c_ladderresult is 0x96 (466B @ jx_linux_y 0x080f19de), NOT 0x98!`
- **L45**: `// 4. C2S c2s_extendtong is 0xFD (game.exe 0x005b83a9 & jx_linux_y 0x081f7774), whereas 0x9A is c2sPartnerExtend!`
- **L52**: `// 1) ApplyCreateTong (14B @ game.exe 0x005b83a9): [0]=0xFD, [1..2]=13, [3]=1, [4]=1(Justice), [5..13]="HaoKiet\0\0"`
- **L61**: `// 2) ApplyAddTong (8B @ game.exe 0x005b85ce): [0]=0xFD, [1..2]=7, [3]=2, [4..7]=0x000178C5`
- **L65**: `// 3) AcceptMember (13B @ game.exe 0x005b88e7): [0]=0xFD, [1..2]=12, [3]=3, [4..7]=playerIdx(2), [8..11]=nameHash, [12]=1`
- **L77**: `// Live jx-oracle-test capture (J16) + jx_linux_y KPlayerTong::Create (0x080e9170..0x080e91dc) & game.exe 0x005b8620..0x005b86af:`
- **L118**: `// jx_linux_y KPlayerTong::CheckCreateCondition (0x080e7eb1) & KPlayerTong::Create (0x080e91af):`

### `tests/JXReborn.Protocol.Tests/GameServer/InventoryAndEquipmentPacketTests.cs`
- **L100**: `// Verified against KPlayer::ServerMoveItem (jx_linux_y @ 0x080cc000):`

### `tests/JXReborn.Protocol.Tests/GameServer/ItemDisplayProtocolParityTests.cs`
- **L18**: `/// and <c>0x95</c> (<c>s2c_viewequip</c> / <c>VIEW_EQUIP_SYNC</c>, <c>1146</c>B), verified against <c>jx_linux_y</c>`
- **L19**: `/// (<c>gMakeSyncItem_Info</c> @ <c>0x08075240</c>, <c>KItemList::SyncItem</c> @ <c>0x080786c0</c>,`
- **L20**: `/// <c>Gen_PlatinaEquipment</c> @ <c>0x080727c0</c>, <c>Gen_EnchasableEquipment</c> @ <c>0x08073120</c>,`
- **L21**: `/// <c>KPlayerStall::GetStallInfo</c> @ <c>0x080e1ae0</c>, and <c>KPlayer::SendEquipItemInfo</c> @ <c>0x080c2560</c>).`
- **L28**: `// jx_linux_y gMakeSyncItem_Info (0x08075240) + Gen_PlatinaEquipment (0x080727c0):`
- **L91**: `// jx_linux_y Gen_EnchasableEquipment (0x08073120) + magicattriblevel_index.txt:`
- **L94**: `// Unenchased sockets on Purple equipment must be -1 (0xFFFFFFFF) so game.exe renders "Chưa khảm nạm"`
- **L158**: `// KItemList::SyncItem (0x080786c0 lines 198-204):`
- **L219**: `// KPlayerStall::GetStallInfo (0x080e1ae0):`
- **L341**: `// S2C 0x95 (1146B = 0x47A) verified instruction-by-instruction from KPlayer::SendEquipItemInfo (0x080c2560)`
- **L342**: `// and KPlayer::_FillViewItemDetailInfo (0x080c0a10):`

### `tests/JXReborn.Protocol.Tests/GameServer/MovementAndPosturePacketTests.cs`
- **L36**: `// s2c_npcwalk (0x50): KNpc::DoWalk writes NpcId at +1, DestMpsY at +5, DestMpsX at +9`
- **L48**: `// s2c_npcrun (0x51): KNpc::DoRun writes DestMpsY at +1, NpcId at +5, DestMpsX at +9`

### `tests/JXReborn.Protocol.Tests/GameServer/NpcShopGoldenVectorPacketTests.cs`
- **L11**: `/// - <c>game.exe</c>:`
- **L12**: `///   - <c>SendBuyItem</c> @ <c>0x0058d620</c>: <c>[0]=0x5D, [1]=TargetY(u8), [2]=TargetX(u8), [3]=TargetPlace(u8), [4..7]=BuyIndex(i32 LE), [8..11]=Tail(u32 LE)</c> (<c>12B</c>)`
- **L13**: `///   - <c>SendSellItem</c> @ <c>0x0058d5b0</c>: <c>[0]=0x5C, [1..4]=RuntimeItemId(u32 LE), [5..8]=SubType(u32 LE), [9..12]=CheckToken(u32 LE)</c> (<c>13B</c>)`
- **L14**: `///   - <c>SendRepairItem</c> @ <c>0x0058d8c0</c>: <c>[0]=0x7A, [1..4]=RuntimeItemId(u32 LE), [5..8]=OperateMode(u32 LE)</c> (<c>9B</c>)`
- **L15**: `/// - <c>jx_linux_y</c>:`
- **L16**: `///   - <c>KProtocolProcess::PlayerBuyItem</c> @ <c>0x080f2500</c> -&gt; <c>KPlayer::BuyItem</c> @ <c>0x080c6b85</c> (<c>[edi+1]=Y, [edi+2]=X, [edi+3]=Place, [edi+4]=BuyIndex</c>)`
- **L17**: `///   - <c>KBuySell::OpenSale</c> @ <c>0x081fc920</c>: <c>0x84</c> (<c>13B</c>: <c>[0]=0x84, [1..4]=ShopId(i32 LE), [5..8]=ScalePercent(i32 LE), [9..12]=CurrencyType(i32 LE)</c>)`
- **L18**: `///   - <c>KPlayer::RepairItem</c> @ <c>0x080c7e2a</c>: <c>0x9B</c> (<c>13B</c>: <c>[0]=0x9B, [1..4]=RuntimeItemId(u32 LE), [5..8]=MaxDur(i32 LE), [9..12]=CurDur(i32 LE)</c>)`
- **L26**: `// Live wire vector captured from game.exe SendBuyItem (0x0058d620) for:`
- **L60**: `// Live wire vector captured from game.exe SendSellItem (0x0058d5b0) selling RuntimeItemId=0x66 (102):`
- **L89**: `// Live wire vector captured from game.exe SendRepairItem (0x0058d8c0) repairing RuntimeItemId=0x64 (100):`
- **L138**: `// Live wire vector captured from jx-oracle-test KPlayer::RepairItem (0x080c7e2a) restoring RuntimeItemId=0x64 to 42/42:`

### `tests/JXReborn.Protocol.Tests/GameServer/PartyWirePacketTests.cs`
- **L12**: `/// - <c>game.exe</c>: <c>KProtocolProcess</c> <c>0x69</c> jump table @ <c>0x00554a50..0x00554c28</c>,`
- **L13**: `///   <c>KPlayer</c> S2C handlers @ <c>0x00567840..0x00568450</c> &amp; <c>0x0056fc00</c>,`
- **L14**: `///   <c>KPlayerTeam</c> C2S senders @ <c>0x00566a50..0x00566c10</c>, <c>0x00568fe0</c>, <c>0x0058d910..0x0058d9a0</c>,`
- **L15**: `///   <c>KNpc::SetAuraSkill</c> @ <c>0x00577d90</c>.`
- **L16**: `/// - <c>jx_linux_y</c>: <c>c2sTeamProtocol</c> @ <c>0x080f2f50</c> &amp; <c>KPlayerTeam::InviteAdd</c> @ <c>0x080e6440</c>.`
- **L21**: `public void TeamProtocolC2SPacket_WhenInviteAddGoldenVectorFrom0x0058d960_ShouldMatchExact8ByteWireLayout()`
- **L23**: `// game.exe SendInviteAdd @ 0x0058d960: [0]=0x53, [1..2]=0x0007, [3]=0x0A, [4..7]=targetNpcDwId (0x00003039 = 12345)`
- **L37**: `public void TeamProtocolC2SPacket_WhenReplyInviteGoldenVectorFrom0x0058d9a0_ShouldMatchExact9ByteWireLayout()`
- **L39**: `// game.exe SendReplyInvite @ 0x0058d9a0: [0]=0x53, [1..2]=0x0008, [3]=0x0B, [4]=bAccept(1), [5..8]=inviterPlayerIdx(2)`
- **L78**: `// jx_linux_y c2sTeamProtocol @ 0x080f2f50 validates exact 8B (wLenMinusOne=7) for sub 1..10 and 9B (wLenMinusOne=8) for sub 0x0B`
- **L95**: `// game.exe KNpc::SetAuraSkill @ 0x00577d90: [0]=0x6F, [1..4]=int32 skillId (159 = 0x9F Thất Tinh Trận)`
- **L112**: `public void TeamProtocolS2CPacket_WhenBuildInviteAdd_ShouldFormatFixed40ByteTcvn3FrameMatching0x00554bda()`
- **L114**: `// game.exe 0x00554bda: cmp cx, 0x27; jne 0x554c28 -> MUST be 40 bytes fixed (wLenMinusOne = 39 = 0x0027)!`
- **L131**: `public void TeamProtocolS2CPacket_WhenBuildChangeCaptain_ShouldWriteNewCaptainAtOffset4AndOldCaptainAtOffset8Matching0x00567fa0()`
- **L133**: `// game.exe 0x00567fa0..0x00567fb5:`
- **L152**: `// game.exe 0x00554b66 (cmp cx, 0x28 -> 41B) & 0x00567dc0: [4]=btLevel, [5..8]=dwNpcId, [9..40]=TCVN3 char[32]`
- **L170**: `// game.exe 0x00554aab (cmp cx, 0x134 -> 309B) & 0x00567840:`

### `tests/JXReborn.Protocol.Tests/GameServer/PlayerStallGoldenVectorPacketTests.cs`
- **L13**: `/// - <c>jx_linux_y</c> <c>KPlayerStall::CheckProtocolSize</c> (<c>0x080e1410</c>) &amp; jump table (<c>0x08251308</c>):`
- **L15**: `/// - <c>game.exe</c> <c>GOI 65..72</c> (<c>0x0053d0e7..0x0053d73c</c>), <c>s2c_stallextend</c> (<c>0x00557280</c> -> <c>0x005490d0</c>),`
- **L16**: `///   and <c>s2c_syncmenustate</c> (<c>0x0055b440</c>: <c>[7]=2</c> TradeOpen @ <c>0x0055b48a</c>, <c>[7]=5</c> Stall @ <c>0x0055b4ab</c>).`
- **L23**: `// jx_linux_y KPlayerMenuState::SetState (0x080d9d60) & game.exe 0x0055b440 / 0x0053c652`
- **L34**: `// game.exe 0x0053d31d: [0]=0x7C, [1..2]=19 (0x0013), [3]=0, [4..7]=count(1), [8..11]=0, [12..15]=itemId(101), [16..19]=price(5000)`
- **L75**: `// Live jx-oracle-test capture (J14, game.exe 0x0053d58e..0x0053d607 & jx_linux_y 0x080e2590):`
- **L147**: `public void StallExtendS2CPacket_AllSubTypes0Through7_ShouldMatchGameExe0x005490d0WireLayouts()`
- **L164**: `// sub=6 SellSuccess (68B: [4..35] buyer TCVN3, [36..67] item TCVN3 @ 0x0054927c)`
- **L171**: `// sub=7 TaxRate (12B @ 0x005492cd: [4..7]=taxRate, [8..11]=taxScale)`

### `tests/JXReborn.Protocol.Tests/GameServer/PlayerTradeGoldenVectorPacketTests.cs`
- **L10**: `/// - <c>game.exe</c> S2C dispatch table (<c>0x0055ef80</c>, <c>offset = opcode * 4</c>):`
- **L11**: `///   - <c>0x75</c> (<c>+0x1D4</c> = <c>0x00555700</c> -> <c>0x0056ae60</c>): <c>s2c_tradechangestate</c> (6B)`
- **L12**: `///   - <c>0x77</c> (<c>+0x1DC</c> = <c>0x00555720</c> -> <c>0x00567260</c>): <c>s2c_trademoneysync</c> (5B)`
- **L13**: `///   - <c>0x78</c> (<c>+0x1E0</c> = <c>0x00555740</c>): <c>s2c_tradedecision</c> (2B)`
- **L14**: `///   - <c>0x81</c> (<c>+0x204</c> = <c>0x005557e0</c> -> <c>0x005b5f80</c>): <c>s2c_tradestatesync</c> / <c>TradePressOkSync</c> (5B)`
- **L15**: `///   - <c>0x8B</c> (<c>+0x22C</c> = <c>0x00556600</c> -> <c>0x00570200</c>): <c>s2c_tradeapplystart</c> (9B)`
- **L17**: `/// - <c>jx_linux_y</c> <c>KPlayerTrade</c> (<c>0x080c43fa</c> <c>0x8B</c>, <c>0x080bfc6b</c> <c>0x77</c>, <c>0x080bfca8</c> <c>0x81</c>, <c>0x080c27fd</c> <c>0x78</c>).`
- **L18**: `/// - <c>game.exe</c> C2S emitters: <c>0x005697e0</c> (<c>0x69</c>), <c>0x00566de0</c> (<c>0x6A</c>), <c>0x00566e00</c> (<c>0x6B</c> 9B),`
- **L19**: `///   <c>0x005698c0</c> (<c>0x6C</c> 5B), <c>0x00569940</c>/<c>0x00569990</c> (<c>0x6D</c> 2B), <c>0x005b60e0</c> (<c>0x75</c> 10B).`
- **L26**: `// C2S opcodes (game.exe + jx_linux_y 0x080f0760)`
- **L34**: `// S2C opcodes (game.exe 0x0055ef80 + jx_linux_y KPlayerTrade::ApplyStart 0x080c43fa / SyncTradeState 0x080bfc50)`
- **L73**: `// Live jx-oracle-test capture (J13, game.exe 0x005698c0): [0]=0x6C, [1..4]=int32 money (25000 = 0x000061A8)`
- **L86**: `[InlineData(0)] // Cancel (GOI 23 @ 0x0053c738)`
- **L87**: `[InlineData(1)] // Confirm (GOI 22 @ 0x0053c6f6)`
- **L88**: `[InlineData(2)] // Lock (GOI 21 @ 0x005699ad)`
- **L105**: `// game.exe 0x00570200 & jx_linux_y 0x080c43fa:`
- **L121**: `// game.exe 0x0056ae60: [0]=0x75, [1]=state (2=trading), [2..5]=partnerNpcDwId (0x000178C4)`
- **L136**: `// game.exe 0x00555720 -> 0x00567260 & jx_linux_y 0x080bfc6b: [0]=0x77, [1..4]=int32 money (100000 = 0x000186A0)`
- **L150**: `// game.exe 0x005557e0 -> 0x005b5f80 & jx_linux_y 0x080bfca8:`
- **L170**: `// game.exe 0x00555740 & jx_linux_y 0x080c27fd: [0]=0x78, [1]=decision`

### `tests/JXReborn.Protocol.Tests/GameServer/ProgressionAndRevivalPacketTests.cs`
- **L17**: `// Verified against KProtocolProcess::PlayerAddBaseAttribute (jx_linux_y @ 0x080f2700)`
- **L36**: `// Verified against KProtocolProcess::PlayerApplyAddSkillPoint (jx_linux_y @ 0x080f3650)`
- **L61**: `0x74,                   // +0x00 ProtocolType = c2s_npcrevive (0x74 in jx_linux_y)`
- **L130**: `// Verified against KPlayer::AddBaseVitality (jx_linux_y @ 0x080c3e90)`
- **L150**: `// Verified against KPlayer::AddSkillPoint (jx_linux_y @ 0x080ca3e0)`

### `tests/JXReborn.Protocol.Tests/GameServer/RelayChatAndMsgShowPacketTests.cs`
- **L204**: `// In s3relay_y CChannelMgr::MarkViceroy (0x0804ed46) & game.exe FUN_0048ed00 (0x0048ed00),`

### `tests/JXReborn.Protocol.Tests/GameServer/RepositoryBoxGoldenVectorPacketTests.cs`
- **L13**: `/// - <c>game.exe</c>:`
- **L14**: `///   - <c>SendStoreMoney</c> @ <c>0x0058d6e0</c>: <c>[0]=0x73, [1]=Direction(0=deposit, 1=withdraw), [2..5]=Amount(i32 LE), [6..9]=CheckToken(u32 LE), [10..13]=Tail(u32 LE)</c> (<c>14B</c>)`
- **L15**: `///   - <c>KItemList::SendSetPassword..SendUnlock</c> @ <c>0x0054ad20..0x0054ae20</c>:`
- **L17**: `/// - <c>jx_linux_y</c>:`
- **L18**: `///   - <c>StoreMoneyCommand</c> @ <c>0x080f1ee0</c> -&gt; <c>KItemList::ExchangeMoney</c> @ <c>0x08076f50</c>`
- **L19**: `///   - <c>c2sBoxOperate</c> @ <c>0x080f1580</c> (jump table @ <c>0x0825183c</c>: <c>0..4</c>) -&gt; <c>KItemList::SyncBoxParam</c> @ <c>0x08075d40</c> (<c>0xA6</c>, <c>5B</c>)`
- **L20**: `///   - <c>LuaOpenBox</c> @ <c>0x0813ef7d</c> &amp; <c>LuaOpenStoreBox</c> @ <c>0x081398cb</c>:`
- **L29**: `// Live wire vector captured from game.exe clicking bao-ruong-1 (51593, 101357):`
- **L30**: `// 72 64 00 62 00 6C 04 00 00 -> RegionId=0x00620064, ObjId=0x0000046C (1132)`
- **L37**: `Assert.Equal(0x00620064u, parsed.RegionId);`
- **L83**: `// Live wire vector captured from game.exe KItemList::SendSetPassword (0x0054ad20, pwd=123456=0x0001E240):`
- **L96**: `// Canonical sub-opcode jump table proven from game.exe (0x0054ad20..0x0054ae20) & jx_linux_y c2sBoxOperate (0x080f1580 / 0x0825183c):`
- **L97**: `// 0 = PlayerSetPassword (0x08078970), 1 = PlayerCanclePassword (0x0807a900), 2 = PlayerChangePassword (0x0807a820),`
- **L98**: `// 3 = PlayerLock (0x08078a30), 4 = PlayerUnlock (0x0807a780).`
- **L111**: `// Live wire vector captured from jx-oracle-test LuaOpenBox (0x0813ef7d) for a standard character`
- **L121**: `// Expanded & PIN-locked state (LuaOpenStoreBox @ 0x081398cb: ExBox1=1, PasswordState=3 Locked -> 88 01 03)`
- **L133**: `// Live wire vector captured from jx-oracle-test KItemList::SyncBoxParam (0x08075d40)`
- **L154**: `// jx_linux_y KItemList::SyncItemBindState (0x08076390):`
- **L180**: `// jx_linux_y KProtocolProcess::PlayerDivideItem (0x080f2420):`

### `tests/JXReborn.Protocol.Tests/GameServer/ScriptModalAndLadderPacketTests.cs`
- **L11**: `/// ladder boards, dice casino, and multi-currency shop packets verified against <c>jx_linux_y</c>.`
- **L20**: `// LuaSale(99, 1) -> KBuySell::OpenSale(playerIdx, shopIdx=98, currency=1, scale=100) [Dược điếm Tương Dương]`
- **L31**: `// LuaSale(93, 2) -> shopIdx=92 (0x5C), currency=2 (FuYuan) [Thần bí Thương Nhân Liễu Ất]`
- **L37**: `// LuaSale(141, 3) -> shopIdx=140 (0x8C), currency=3 (Coin/Tiền Đồng) [Tạp hóa Hoạt động cửa hàng]`
- **L51**: `// jx_linux_y LuaAskClientForNumber (0x08138c50): 77 bytes (0x4D), variable opcode 0xA3 (gInitClientProtocolSize[0xA3] == -1)`
- **L88**: `// jx_linux_y KProtocolProcess::c2sCityWarExtend (0x080f1680) -> SubType 3 reads *(int32*)(buf + 4)`
- **L110**: `// jx_linux_y LuaShowLadder (0x081236e0): ShowLadder(3, 1, 2, 3) emits 19 bytes:`
- **L136**: `// jx_linux_y LuaOpenDiceGame (0x08126a50) & KDiceGame::ProcessGame (0x082019f0):`

### `tests/JXReborn.Protocol.Tests/GameServer/SkillAndStateEffectGoldenVectorTests.cs`
- **L12**: `/// (<c>0x87</c>, <c>0x7A</c>, <c>0x85</c>, <c>0x5A</c>, <c>0x56</c>) verified against <c>game.exe</c> and <c>jx_linux_y</c>.`

### `tests/JXReborn.Protocol.Tests/GameServer/SocialWirePacketTests.cs`
- **L11**: `/// <c>game.exe</c> <c>KProtocolProcess</c> constructor (<c>0x0055ef80</c>) and <c>jx_linux_y</c> C2S dispatch table (<c>0x080f0680</c>):`
- **L12**: `/// - <c>game.exe</c> S2C table (<c>0x0055ef80</c>):`
- **L13**: `///   - <c>0x6F</c> -&gt; <c>0x00554000</c> (<c>s2c_chatfriendonline</c>), NOT <c>0x71</c> (<c>0x71</c> = <c>0x00553e30</c> <c>s2c_tradeapplystart</c>)!`
- **L14**: `///   - <c>0x70</c> -&gt; <c>0x00553bb0</c> (<c>s2c_chatonefrienddata</c>, 41B TCVN3).`
- **L15**: `///   - <c>0x79</c> -&gt; <c>0x005532a0</c> (<c>s2c_chatfriendoffline</c>), NOT <c>0x72</c> (<c>0x72</c> = <c>0x005555a0</c> <c>s2c_trademoneysync</c>)!`
- **L16**: `///   - <c>0x71</c> -&gt; <c>0x00553e30</c> (<c>s2c_chatfriendonline</c>).`
- **L17**: `///   - <c>0x72</c> -&gt; <c>0x005555a0</c> (<c>s2c_chatdeletefriend</c>).`
- **L18**: `///   - <c>0x73</c> -&gt; <c>0x005556a0</c> (<c>s2c_chatfriendoffline</c>).`
- **L19**: `///   - <c>0x7A</c> -&gt; <c>0x00555770</c> (<c>s2c_npcsyncstateinfo</c> <c>NPC_SYNC_STATEINFO</c>, NOT <c>s2c_chatdeletefriend</c>)!`
- **L26**: `// jx_linux_y c2sChatAddFriend @ 0x080f2390 -> 0x080c2bc0: [0]=0x62, [1..4]=inviterPlayerIdx(42)`
- **L45**: `// jx_linux_y c2sChatRefuseFriend @ 0x080f2360 -> 0x080c2b00: [0]=0x63, [1..4]=inviterPlayerIdx(99)`
- **L61**: `public void FriendStatusS2COpcodes_AgainstGameExe0x0055ef80DispatchTable_ShouldMatchExactClientHandlers()`
- **L63**: `// game.exe KProtocolProcess::KProtocolProcess (0x0055f210..0x0055f224, offset = opcode * 4):`
- **L64**: `// - 0x71 (0x1c4/4) = 0x00553e30 -> 0x00567100 (s2c_chatfriendonline: CoreDataChanged 0x1D GDCNI_CHAT_FRIEND_STATUS)`
- **L65**: `// - 0x72 (0x1c8/4) = 0x005555a0 (s2c_chatdeletefriend: erases friend from KChat::m_FriendList @ +0xeaac)`
- **L66**: `// - 0x73 (0x1cc/4) = 0x005556a0 -> 0x005b5040 (s2c_chatfriendoffline)`
- **L67**: `// - 0x8B (0x22c/4) = 0x00556600 (s2c_tradeapplystart, matching jx_linux_y 0x080c43fa: mov byte [ebp-0x15], 0x8b)`
- **L191**: `// 1) GOI_FRIEND_INVITE (0x25 @ 0x0053c82a) -> 38B: FC 25 00 07 00 FF "J12SociB\0..."`
- **L195**: `// 2) GOI_FRIEND_INVITE_RESPONSE (0x2A @ 0x0053ca23) -> 39B: FC 26 00 07 01 FF "J12SociA\0..." 01`
- **L199**: `// 3) CFriendNotify::DeleteFriend (0x004756fc) -> 37B: FC 24 00 07 03 "J12SociB\0..."`
- **L203**: `// 4) KUiPlayerBar::SendSomeoneMessage (0x0044d640) -> 67B: FB 42 00 20 3F 00 FF FF FF FF "J12SociA\0..." 18 "Chao bang huu truoc chan"`

### `tests/JXReborn.TestBase/WireCaptures/VirtualJxClient.cs`
- **L16**: `/// Lightweight, cross-platform virtual <c>game.exe</c> shadow client speaking encrypted TCP frames via`
- **L186**: `// Send 0x43 c2s_syncend with client anti-cheat tokens, plus real game.exe post-sync frames (0x8A, 0x65, 0x90).`

### `tests/JXReborn.TestBase/WireCaptures/WireCaptureC2sFrame.cs`
- **L4**: `/// Recorded decrypted <c>C2S</c> frame emitted by <c>game.exe</c> at <c>TickOffsetFrames</c> from the start of a step.`

### `tests/JXReborn.TestBase/WireCaptures/WireCaptureDocument.cs`
- **L4**: `/// Immutable golden wire capture recorded from a live <c>legacy server</c> + <c>game.exe</c> session for fast offline`
- **L5**: `/// TCP + Steppable-Clock replay against <c>JXReborn</c> without <c>game.exe</c> or Docker.`

### `tests/JXReborn.TestBase/WireCaptures/WireCaptureStore.cs`
- **L114**: `$"Wire capture baseline '{captureId}' was not found at '{path}'. Record it from legacy server + game.exe first.",`

### `tests/JXReborn.TestBase/WireSimulators/GameExeProtocolVerifier.cs`
- **L8**: `/// Stateful wire-level verifier modeling the exact C++ <c>game.exe</c> (<c>KNetConnectAgent::Breathe</c>,`
- **L9**: `/// <c>KProtocolProcess::SyncNpc</c>, <c>KPlayer::SyncCurPlayer</c>, <c>KProtocolProcess::SyncEnd</c>) rules.`
- **L39**: `/// Validates a decrypted GameServer S2C payload against <c>game.exe</c> framing and state-machine invariants.`
- **L52**: `$"Unknown GameServer S2C opcode 0x{opcode:X2} (absent from jx_linux_y gInitClientProtocolSize).");`
- **L65**: `// KNetConnectAgent::Breathe (NetConnectAgent.cpp:332): advance pointer by 1 + *(ushort*)(pMsg + 1)`
- **L114**: `// KProtocolProcess.cpp:1408-1409 & jx_linux_y KNpc::SendSyncData (0x080994b0):`
- **L138**: `// KPlayer::SyncCurPlayer (KPlayer.cpp:5803): this->m_nIndex = NpcSet.SearchID(PlaySync->m_dwID);`
- **L143**: `$"s2c_synccurplayer (0x44) arrived for NpcId={playerNpcId} BEFORE s2c_syncnpc (0x4C); game.exe NpcSet.SearchID returns 0 (unbound player)!");`
- **L205**: `$"s2c_syncitem (0x5F) Series at +10 must be in 0..4, got {series} (KItemGenerator rejects invalid series).");`
- **L322**: `// game.exe 0x00555770 -> 0x00578b10 & jx_linux_y 0x08091150: [0]=0x7A, [1..6]=StateInfo[6], [7..10]=NpcId`
- **L352**: `$"s2c_syncstateeffect (0x87) attribute count {attribCount} exceeds game.exe maximum {SyncStateEffectS2CPacket.MaxMagicAttribCount}.");`

### `tests/JXReborn.TestBase/WireSimulators/GameExeProtocolViolationException.cs`
- **L4**: `/// Thrown when an outbound server packet violates C++ <c>game.exe</c> framing, binary field layout, or state-machine ordering rules.`

### `tests/JXReborn.TestBase/WireSimulators/LegacyReferenceProtocolSizes.cs`
- **L6**: `/// Authoritative ground-truth <c>gInitClientProtocolSize</c> (<c>0x080ef960</c>) and <c>gInitServerProtocolSize</c> (<c>0x080ef560</c>)`
- **L7**: `/// opcode size tables extracted directly from <c>jx_linux_y</c> (Linux 6.0 GameServer binary).`
- **L13**: `/// Ground-truth GameServer S2C protocol sizes (<c>gInitClientProtocolSize</c> @ <c>0x080ef960</c>).`
- **L17**: `[0x21] = 25, // s2c_notifyplayerexchange (handled before CoreShell in KNetConnectAgent::Breathe)`
- **L40**: `/// Ground-truth GameServer C2S protocol sizes (<c>gInitServerProtocolSize</c> @ <c>0x080ef560</c>).`

### `tests/JXReborn.ZoneEngine.Tests/Combat/CombatAndMissileSimulationTests.cs`
- **L129**: `// 4. Canonical Poison DoT blending formula (KNpc::AddPoisonState @ 0x08091c10 in jx_linux_y)`

### `tests/JXReborn.ZoneEngine.Tests/Combat/CombatFormulaSimulationTests.cs`
- **L876**: `// jx_linux_y (0x0809e790 + 0x080938a0): unconditional Math.Max(resYin, rawResYan) floors 0-Yan Wood monster at 0% (300 dmg, NOT 360).`

### `tests/JXReborn.ZoneEngine.Tests/Combat/PlayerDeathAndRevivalTests.cs`
- **L18**: `/// Verifies legacy <c>KPlayer::Revive</c> (<c>0x080c7890</c>) and <c>KNpc::DoDeath</c> parity:`
- **L197**: `// Must emit 0x89 (s2c_playerrevive) BEFORE 0x49 (s2c_syncworld) matching KPlayer::Revive (0x080c7890)`
- **L207**: `$"Expected 0x89 (index {reviveEventIdx}) BEFORE 0x49 (index {transitionEventIdx}) so game.exe exits do_death before loading town.");`

### `tests/JXReborn.ZoneEngine.Tests/Combat/PlayerSitSimulationTests.cs`
- **L16**: `/// matching <c>jx_linux_y</c> <c>KProtocolProcess::NpcSitCommand</c> (<c>0x080f1fb0</c>) and <c>KNpc::DoSit</c> (<c>0x08096cc0</c>).`

### `tests/JXReborn.ZoneEngine.Tests/Combat/PvpRelationMatrixAndPkStateTests.cs`
- **L112**: `// Novice can freely switch Peace -> Fight -> Slaughter -> Peace in town matching jx_linux_y 0x080dad60`
- **L189**: `[InlineData(false, 1, EntityCamp.Justice, PlayerPkMode.Slaughter, 1, EntityCamp.Justice, PlayerPkMode.Peace, EntityRelationKind.Friend)] // Step 7b: Slaughter same-camp ({1,2,3}) immunity (0x080b667e)`

### `tests/JXReborn.ZoneEngine.Tests/Combat/PvpRelationMatrixStressTests.cs`
- **L195**: `// 7. Slaughter Priority Check (Precedes Peace; same-camp {Justice, Evil, Balance} stays Friend per 0x080b667e)`

### `tests/JXReborn.ZoneEngine.Tests/Combat/QaParityRemediationSimulationTests.cs`
- **L295**: `// Legacy KPlayer::Revive (0x080c7890): remains dead until client clicks "Về Thành Dưỡng Sức" (0x74)`

### `tests/JXReborn.ZoneEngine.Tests/Combat/SkillCastWindupTimingTests.cs`
- **L204**: `// - Tick 10: wind-up fires -> Missile 48 spawns at d=0, enters DoWait (KMissle::CreateMissle -> DoWait)`
- **L205**: `// - Tick 11..19: KMissle::OnFly checks collision before stepping (d=0, 28, ..., 224; at d=224 distance is 32 > 16)`
- **L206**: `// - Tick 20: KMissle::OnFly checks d=252 (|256-252|=4 <= 16 exact cell) -> HIT!`
- **L207**: `//   NpcLifeSync arrives on game.exe on tick 21, matching game.exe's local AnimFile4 explosion tick 21.`
- **L222**: `$"Kháng Long Hữu Hối dealt damage too early on tick {t} (expected tick 20, aligning with game.exe AnimFile4 explosion).");`

### `tests/JXReborn.ZoneEngine.Tests/Guilds/GuildCreationTests.cs`
- **L324**: `// jx_linux_y KPlayerTong::CheckCreateCondition (0x080e7eb1) & game.exe 0x005b83ae (14B 0xFD sub=1 char[9]):`
- **L346**: `// jx_linux_y KPlayerTong::CheckCreateCondition (0x080e7f14, error=9) & game.exe 0x005b838a:`

### `tests/JXReborn.ZoneEngine.Tests/Items/GroundItemDropAndBrokenEquipParityTests.cs`
- **L17**: `/// verified against <c>jx_linux_y</c> (<c>KPlayer::ServerThrowAwayItem</c>, <c>KObjSet::MakeObjItemInfo</c>,`
- **L18**: `/// <c>KObjSet::Add</c>, <c>KItemList::Abrade</c>, and <c>KItem::ModifyBrokenFlag</c>).`
- **L158**: `// KObjSet::Add (0x080bd9a0) + MakeObjItemInfo (0x080be550): newly thrown items set SoundFlag=1 | MovieFlag=2 -> 0x03, and m_nCount = StackCount (7)`
- **L164**: `// KObj::SyncAdd (0x080bca30): AOI / RequestObj sync of an already-lying ground object must set Flag = 0x00 (no repeat throw movie/sound!)`
- **L239**: `// KObjSet::MakeObjItemInfo (0x080be550):`
- **L300**: `// Cap tips at 10 per player session (m_nForbitThrowTipsCount < 10 in ServerThrowAwayItem 0x080ccce0)`
- **L377**: `// Trigger abrasion to 0 through worker helper matching KItemList::Abrade (0x0807d830)`

### `tests/JXReborn.ZoneEngine.Tests/Items/ItemDisplaySynchronizationParityTests.cs`
- **L21**: `/// Mask NPC disguise broadcast (<c>0xAD</c> <c>KNpc::ChangeFeature_Disguise</c>), and <c>ViewPlayerEquipmentZoneCommand</c> (<c>0x78 -&gt; 0x95</c>).`
- **L120**: `// KItemList::ExchangeItemHand (0x0807e180 lines 365-427):`
- **L178**: `// jx_linux_y KItemList::GetFeatureType (0x08077410), GetPlatinaEquipRes (0x08070d80), GetEquipRes (0x08070cf0):`
- **L222**: `// KNpc::ChangeFeature_Disguise (0x08096670) + KNpc::SyncFeatureData (0x080916a0):`
- **L291**: `// KProtocolProcess::c2sViewEquip (0x080f31d0) + KPlayer::SendEquipItemInfo (0x080c2560):`

### `tests/JXReborn.ZoneEngine.Tests/Movement/MovementSimulationTests.cs`
- **L435**: `// Per jx_linux_y KNpc::DoRun (0x08097060): insufficient stamina falls back to DoWalk (0x50).`

### `tests/JXReborn.ZoneEngine.Tests/Parties/PartyAuraResistanceMatrixTests.cs`
- **L14**: `/// Unit tests verifying the Five-Elements Party Resistance Matrix loaded by <c>jx_linux_y</c>`
- **L15**: `/// (<c>0x080e4ce3</c> -&gt; <c>server1/settings/npc/player/team_resist_effect.ini</c>):`
- **L19**: `/// - Distance cutoff matches <c>KTeam::GetMemberCountAround</c> (<c>0x080e4ee0</c>: <c>distanceSq &lt;= 1024 * 1024</c>).`

### `tests/JXReborn.ZoneEngine.Tests/Parties/PartyExpSharingTests.cs`
- **L17**: `/// <c>jx_linux_y</c> <c>KPlayer::ShareTeamExp</c> (<c>0x080c9be0..0x080ca0ff</c>):`
- **L18**: `/// - Share radius squared: <c>dx*dx + dy*dy &lt;= 0x100000</c> (<c>1,048,576 = 1024 * 1024</c> MPS @ <c>0x080ca005</c>, <c>0x080ca0b6</c>).`
- **L19**: `/// - Killer EXP (<c>nCount &gt;= 2</c>): <c>max(1, monsterExp * (100 + nCount) / 100)</c> (@ <c>0x080c9d5e</c>, <c>0x080c9ea5</c>).`
- **L20**: `/// - Teammate EXP: <c>sqrtScale = (int)(sqrt(nCount) * 100.0); ratio = min(60, member.Level * sqrtScale / totalLevel); max(1, monsterExp * ratio / 100)</c> (@ <c>0x080c9d32..0x080c9f0e</c>).`
- **L21**: `/// - Captain Leadership EXP: <c>((totalLevel - killer.Level) * monsterExp * (100 + 15 * nCount) * leadFactor) / (totalLevel * 10000)</c> (@ <c>0x080c9e42..0x080c9e96</c>).`
- **L64**: `// jx_linux_y 0x080ca005 & 0x080ca0b6: cmp eax, 0x100000 (1024 * 1024 = 1,048,576)`
- **L110**: `// jx_linux_y KPlayer::ShareTeamExp (0x080c9be0):`
- **L112**: `// - Killer EXP (0x080c9d5e, 0x080c9ea5): 1000 * (100 + 2) / 100 = 1020`
- **L113**: `// - Teammate EXP (0x080c9d32..0x080c9f0e): sqrtScale = (int)(sqrt(2) * 100.0) = 141;`
- **L136**: `// jx_linux_y 0x080c9f08: test eax, eax; mov edx, 1; cmovle eax, edx`
- **L160**: `// jx_linux_y 0x080c9e42..0x080c9e96:`

### `tests/JXReborn.ZoneEngine.Tests/Parties/PartyLifecycleTests.cs`
- **L14**: `/// <c>jx_linux_y</c> (<c>0x080c9790..0x080ca250</c>) and <c>server1/settings/npc/player/level_lead_exp.txt</c>.`

### `tests/JXReborn.ZoneEngine.Tests/Scripting/XiangyangQuestsAndBossesTests.cs`
- **L28**: `/// Layer 3 script and quest tests covering <c>LuaGetByte/LuaSetByte</c> 1-based packing,`
- **L55**: `// Out-of-range byteIndex (< 1 or > 4) must safely return 0 and preserve Task 17 per jx_linux_y LuaSetByte (0x081171e0)`

### `tests/JXReborn.ZoneEngine.Tests/Shops/EquipmentRepairCostBoundaryTests.cs`
- **L11**: `/// - <c>jx_linux_y</c>: <c>KItem::Script_GetRepairPrice</c> @ <c>0x0806ef10</c>, <c>KItem::GetRepairPrice</c> @ <c>0x0806f0a0</c>`
- **L83**: `// Formula (repairitem.lua:175 & KItem::GetRepairPrice @ 0x0806f0a0):`

### `tests/JXReborn.ZoneEngine.Tests/Stalls/PlayerStallBuyoutAndTaxBoundaryTests.cs`
- **L18**: `/// <c>server1/settings/taxrates.ini</c> and <c>jx_linux_y</c> <c>KPlayerStall::GetTaxRate</c> (<c>0x080e16f0</c>).`
- **L40**: `[InlineData(121, 10)] // Unlisted map -> CommonRate=10 fallback (0x080e1530)`

### `tests/JXReborn.ZoneEngine.Tests/Storage/RepositoryItemOperationsParityTests.cs`
- **L21**: `/// and <c>ExpandBox1..3</c> (<c>nPlace=9..11</c>) extracted from <c>jx_linux_y</c>:`
- **L23**: `///   <item><c>LuaOpenBox</c> (<c>0x0813ee70</c>): <c>FightMode == 0</c>, <c>do_stand</c>, open-box session gate (<c>m_nBuyIdx == -2</c>).</item>`
- **L24**: `///   <item><c>KItemList::ExchangeMoney</c> (<c>0x08076f50</c>): <c>FightMode == 0</c>, deposit allowed while PIN-locked, withdraw blocked while PIN-locked.</item>`
- **L25**: `///   <item><c>KItemList::ExchangeItemHand</c> (<c>0x0807e180</c>): <c>ExpandBox1..3</c> <c>m_nGainFlag</c> gating, PIN-lock blocking, <c>BindState</c> allowed in repository but blocked in <c>Trade</c>.</item>`
- **L26**: `///   <item><c>KItemList::StackItem</c> (<c>0x0807ba40</c>) &amp; <c>IsSameForStackWith</c> (<c>0x08070230</c>): stack merging, overflow retention, <c>BindState</c> / expiry separation.</item>`
- **L27**: `///   <item><c>KItemList::DivideItem</c> (<c>0x0807d370</c>) &amp; <c>ServerDivideItem</c> (<c>0x080c2cb0</c>): single-to-hand &amp; continuous in-room split inside repository/expand boxes.</item>`
- **L28**: `///   <item><c>KItemList::EatMecidine</c> (<c>0x0807c830</c>) &amp; <c>ThrowAwayMedicineFromEquipmentRoom</c> (<c>0x080c1b30</c>): items in repository/expand boxes cannot be directly used or thrown away.</item>`
- **L171**: `// 1. In FightMode != 0: both deposit and withdraw must be rejected (KItemList::ExchangeMoney @ 0x08076f50)`
- **L408**: `// Attempting to use medicine stored in RepositoryRoom (even by RuntimeItemId!) MUST be rejected (EatMecidine @ 0x0807c830)`

### `tests/JXReborn.ZoneEngine.Tests/Storage/RepositoryMoneyAndPinBoundaryTests.cs`
- **L65**: `player.RepositoryExpandBoxMask = 1; // ExBox1 unlocked (required by KItemList::PlayerSetPassword @ 0x0807897a)`
- **L188**: `// - jx_linux_y KItemList::PlayerSetPassword @ 0x0807897a + SyncBoxParam @ 0x08075d40`
- **L535**: `// KItemList::ExchangeItemHand (0x0807e180, case 4): locked box blocks ALL moves into/out of RepositoryRoom`
- **L577**: `// KItemList::ExchangeItemHand (0x0807e180, case 4): rejected cleanly, bagItem stays in EquipRoom (0, 0) and HandItem is null`
- **L852**: `// Step 2: KItemList::ExchangeItemHand (0x0807e180, case 2): unequipping from Equip while PIN-locked MUST BE REJECTED`

### `tests/JXReborn.ZoneEngine.Tests/Trades/PlayerTradeZoneModuleTests.cs`
- **L387**: `// 1. Open trade willingness -> jx_linux_y KPlayer::TradeApplyOpen (0x080c2950) & KPlayerMenuState::SetState (0x080d9d60)`
- **L401**: `// 2. Close trade willingness -> jx_linux_y KPlayer::TradeApplyClose (0x080c26e0)`
- **L429**: `// Only Player B opens KNB trade willingness; Player A stays Normal (jx_linux_y KPlayer::TradeApplyStart 0x080c4330)`
- **L438**: `// Both broadcast MenuState = 3 (PLAYER_MENU_STATE_TRADING) + 0x75 (state=2, partnerNpcId) (0x080cbbb0 + 0x080d9d60 case 3)`
- **L445**: `// Cancel trade -> KPlayer::CancelTrade (0x080c2740) + RestoreBackupState (0x080da1f0):`
- **L457**: `// Re-apply, accept, lock, and confirm -> KPlayer::TradeDecision (0x080ce328):`

### `tests/JXReborn.ZoneEngine.Tests/WorldZones/PlayerMovementEquipmentAndMedicineTests.cs`
- **L25**: `/// Phase 3 gameplay simulation regression tests covering all 5 in-game bugs reported with legacy <c>game.exe</c>:`
- **L29**: `/// 4. Periodic 90-tick <c>0x4D</c> NPC heartbeat refreshing <c>game.exe</c> <c>m_SyncSignal</c> (&lt; 1000 frames) &amp; <c>0x4F</c> AOI leave removal.`
- **L240**: `// 1. Use stack-of-2 potion on belt (0,0) -> emits 0xA8 ItemStackCountSync(count=1), ticks 10 frames (jx_linux_y potion.txt cadence) -> regenerates HP & sends 0x46`
