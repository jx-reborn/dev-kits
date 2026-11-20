#pragma once

#include <cstdint>

namespace jx {

// -----------------------------------------------------------------------------
// game.exe globals (ImageBase = 0x00400000, non-ASLR)
// -----------------------------------------------------------------------------
constexpr uintptr_t ADDR_G_HINST                  = 0x006de308;
constexpr uintptr_t ADDR_G_REPRESENT_SHELL        = 0x006de30c;
constexpr uintptr_t ADDR_G_SOUND_SHELL            = 0x006de310;
constexpr uintptr_t ADDR_G_CORE_SHELL             = 0x006de314;
constexpr uintptr_t ADDR_G_CORE_DATA_CHANGED_CB   = 0x006dfde0; // pointer to KUiCoreDataChangedCallback instance
constexpr uintptr_t ADDR_G_CORE_DATA_CB_INSTANCE  = 0x006de3f0;

constexpr uintptr_t ADDR_KMYAPP                   = 0x006de340;
constexpr uintptr_t OFF_KWIN32APP_SHOW_MOUSE      = 0x44;
constexpr uintptr_t OFF_KWIN32APP_ACTIVE          = 0x48;
constexpr uintptr_t OFF_KWIN32APP_MULTI_GAME      = 0x4c;
constexpr uintptr_t OFF_KMYAPP_GAMELOOP_COUNT     = 0x88; // absolute 0x006de3c8
constexpr uintptr_t ADDR_KMYAPP_GAMELOOP_COUNT    = 0x006de3c8;
constexpr uintptr_t ADDR_KMYAPP_VTABLE_GAMELOOP   = 0x00646bc8; // vtable 0x00646bbc slot [3]

constexpr uintptr_t ADDR_EXIT_AD_FN               = 0x00526750; // void __cdecl/? (int arg)

// KNetConnectAgent (0x006de818; 0x006de3e4 is g_RainbowLib)
constexpr uintptr_t ADDR_NET_CONNECT_AGENT        = 0x006de818;
constexpr uintptr_t OFF_NET_GW_CLIENT             = 0x00;  // IClient*
constexpr uintptr_t OFF_NET_GS_CLIENT             = 0x04;  // IClient*
constexpr uintptr_t OFF_NET_GW_CONNECTED          = 0x40c; // uint8_t
constexpr uintptr_t OFF_NET_GS_CONNECTED          = 0x40d; // uint8_t
constexpr uintptr_t OFF_NET_DISCONNECTING         = 0x410; // uint8_t / int32_t (param_1[0x104])
constexpr uintptr_t ADDR_GAMESVR_ICLIENT          = 0x006e1da4; // IClient* (vtable +0x10 = SendPackToServer)

// KLogin (0x006dec30)
constexpr uintptr_t ADDR_KLOGIN                   = 0x006dec30;
constexpr uintptr_t OFF_KLOGIN_STATUS             = 0x08;  // int32_t (0=Idle,1=GwConn,2=WaitAuth,4=WaitRoles,6=RolesReady,7=WaitNewRole,9=WaitSelRole,10=GsConn)
constexpr uintptr_t OFF_KLOGIN_RESULT             = 0x0c;  // int32_t
constexpr uintptr_t OFF_KLOGIN_AUTO_LOGIN         = 0x10;  // uint8_t
constexpr uintptr_t OFF_KLOGIN_NUM_ROLE           = 0x12;  // int16_t
constexpr uintptr_t OFF_KLOGIN_ROLES              = 0x14;  // RoleInfo36B[24]
constexpr uintptr_t STRIDE_KLOGIN_ROLE            = 0x24;  // 36 bytes
constexpr uintptr_t OFF_KLOGIN_SERVER_IP          = 0x71c; // uint32_t
constexpr uintptr_t OFF_KLOGIN_INV_ACCOUNT        = 0x720; // char[32] bitwise-NOT
constexpr uintptr_t OFF_KLOGIN_INV_MD5            = 0x740; // char[64] bitwise-NOT
constexpr uintptr_t OFF_KLOGIN_SEL_ROLE_NAME      = 0x780; // char[32]

// Protocol & UI globals
constexpr uintptr_t ADDR_PROTOCOL_PROCESS         = 0x006e2418;
constexpr uintptr_t ADDR_PROTOCOL_HANDLER_0X63    = 0x006e25a4; // 0x006e2418 + 0x63 * 4 -> 0x005554b0
constexpr uintptr_t ADDR_PROTOCOL_HANDLER_0X7E    = 0x006e2610; // 0x006e2418 + 0x7E * 4 -> 0x00554210
constexpr uintptr_t ADDR_PROCESS_NET_MSG_WRAPPER  = 0x005389c0; // B9 18 24 6E 00 E9 rel32 (rel32 at 0x005389c6)
constexpr uintptr_t ADDR_PROCESS_NET_MSG_JMP_REL  = 0x005389c6;
constexpr uintptr_t ADDR_UI_MSG_SEL_PTR           = 0x0069b558; // KUiMsgSel*
constexpr uintptr_t ADDR_UI_SAY_FLAG              = 0x006e1db0;
constexpr uintptr_t ADDR_UI_TALK_CALLBACK_FLAG    = 0x006e1db4; // bNeedCallback in Talk (0x63 uiId=2)
constexpr uintptr_t ADDR_UI_SAY_OPT_COUNT         = 0x006e1db8;

// -----------------------------------------------------------------------------
// World / Entity Arrays (pointers to heap buffers in .data; always null-check!)
// -----------------------------------------------------------------------------
constexpr uintptr_t ADDR_SUBWORLD_ARRAY_PTR       = 0x008e4d00;
constexpr uintptr_t ADDR_SCENE_PLACE              = 0x012c80d0; // KScenePlaceC g_ScenePlace
constexpr size_t    OFF_SCENE_FOCUS_REGION_X      = 0x00000020; // int32_t m_FocusRegion.x
constexpr size_t    OFF_SCENE_FOCUS_REGION_Y      = 0x00000024; // int32_t m_FocusRegion.y
constexpr size_t    OFF_SCENE_PROC_REGIONS_BASE   = 0x00031e4c; // KScenePlaceRegionC* m_pInProcessAreaRegions[9]
constexpr size_t    OFF_SCENE_CENTER_REGION_PTR   = 0x00031e5c; // KScenePlaceRegionC* focus region (0,0) = slot [4]
constexpr size_t    OFF_SCENE_FIRST_TO_LOAD_IDX   = 0x000324f4; // int32_t m_nFirstToLoadIndex (-1 when idle)
constexpr uintptr_t STRIDE_SUBWORLD               = 0x108;
constexpr uintptr_t OFF_SUBWORLD_ID               = 0x24; // int32_t (Map ID)
constexpr uintptr_t OFF_SUBWORLD_REGION_PTR       = 0x2c; // KRegion*
constexpr uintptr_t OFF_SUBWORLD_TOTAL_REGION     = 0x38; // int32_t
constexpr uintptr_t OFF_SUBWORLD_REGION_WIDTH     = 0x3c; // int32_t
constexpr uintptr_t OFF_SUBWORLD_REGION_HEIGHT    = 0x40; // int32_t
constexpr uintptr_t OFF_SUBWORLD_CURRENT_TIME     = 0x50; // uint32_t
constexpr uintptr_t STRIDE_KREGION                = 0x4ac;
constexpr uintptr_t OFF_KREGION_LEFT_MPS_X        = 0x98; // int32_t
constexpr uintptr_t OFF_KREGION_TOP_MPS_Y         = 0x9c; // int32_t

constexpr uintptr_t ADDR_PLAYER_ARRAY_PTR         = 0x008e48f4;
constexpr uintptr_t STRIDE_PLAYER                 = 0x8b30; // Local client = Player[1] (+0x8b30)
constexpr uintptr_t OFF_PLAYER_PAUSE_SEND         = 0x0020; // rel to Player[1]: 0x8b50 from base
constexpr uintptr_t OFF_PLAYER_RUN_STATUS         = 0x00c4; // rel to Player[1]: 0x8bf4 from base (0=Run, 1=Walk)
constexpr uintptr_t OFF_PLAYER_NPC_INDEX          = 0x0114; // rel to Player[1]: 0x8c44 from base
constexpr uintptr_t OFF_PLAYER_ITEM_LIST          = 0x011c; // rel to Player[1]: 0x8c4c from base
constexpr uintptr_t OFF_PLAYER_HAND_ITEM          = 0x013c; // rel to Player[1]: KItemList::m_Hand (int32_t item array idx)
constexpr uintptr_t OFF_PLAYER_MONEY              = 0x4d90; // rel to Player[1]: 0x011c + 0x4c74 (KItemList::SetMoney @ 0x0054c0b0)
constexpr uintptr_t OFF_PLAYER_SAVE_MONEY         = 0x4dac; // rel to Player[1]: 0x011c + 0x4c90 (KItemList::SetMoney @ 0x0054c0b0)
constexpr uintptr_t OFF_PLAYER_ATTR_POINTS        = 0x58a0; // rel to Player[1]: 0xe3d0 from base (0x0056f79a)
constexpr uintptr_t OFF_PLAYER_SKILL_POINTS       = 0x58a4; // rel to Player[1]: 0xe3d4 from base (0x0056f7a4)
constexpr uintptr_t OFF_PLAYER_STRENGTH           = 0x58a8; // int32_t base Strength (0x0056c1e0)
constexpr uintptr_t OFF_PLAYER_DEXTERITY          = 0x58ac; // int32_t base Dexterity
constexpr uintptr_t OFF_PLAYER_VITALITY           = 0x58b0; // int32_t base Vitality
constexpr uintptr_t OFF_PLAYER_ENERGY             = 0x58b4; // int32_t base Energy
constexpr uintptr_t OFF_PLAYER_CUR_STRENGTH       = 0x58c0; // int32_t current Strength (with equip)
constexpr uintptr_t OFF_PLAYER_CUR_DEXTERITY      = 0x58c4; // int32_t current Dexterity (with equip)
constexpr uintptr_t OFF_PLAYER_CUR_VITALITY       = 0x58c8; // int32_t current Vitality (with equip)
constexpr uintptr_t OFF_PLAYER_CUR_ENERGY         = 0x58cc; // int32_t current Energy (with equip)
constexpr uintptr_t OFF_PLAYER_EXP                = 0x58d8; // int64_t current Experience (KPlayer::SetExp @ 0x0056b170)
constexpr uintptr_t OFF_PLAYER_LEAD_EXP           = 0x58e8; // rel to Player[1]: 0xe418 from base (0x0056f88c)
constexpr uintptr_t OFF_PLAYER_TEAM               = 0x5910; // rel to Player[1]: KPlayerTeam m_cTeam
constexpr uintptr_t OFF_TEAM_FLAG                 = 0x0000; // int32_t m_nFlag (0=not in team, 1=in team)
constexpr uintptr_t OFF_TEAM_FIGURE               = 0x0004; // int32_t m_nFigure (0=TEAM_CAPTAIN, 1=TEAM_MEMBER)
constexpr uintptr_t OFF_TEAM_APPLY_CAPTAIN_ID     = 0x0008; // uint32_t m_nApplyCaptainID
constexpr uintptr_t OFF_TEAM_AUTO_REFUSE_INVITE   = 0x0010; // int32_t m_bAutoRefuseInvite
constexpr uintptr_t ADDR_G_TEAM                   = 0x01340ee8; // CTeam g_Team[0] client party state
constexpr uintptr_t OFF_GTEAM_STATE               = 0x0004; // int32_t m_nState (0=closed, 1=open)
constexpr uintptr_t OFF_GTEAM_CAPTAIN_ID          = 0x0008; // int32_t m_nCaptain (Npc.m_dwID, -1 when empty)
constexpr uintptr_t OFF_GTEAM_MEMBER_IDS          = 0x000c; // int32_t m_nMember[7] (Npc.m_dwID, -1 when empty)
constexpr uintptr_t OFF_GTEAM_MEM_NUM             = 0x0028; // int32_t m_nMemNum (0..7 non-captain members)
constexpr uintptr_t OFF_GTEAM_CAPTAIN_LEVEL       = 0x0030; // int32_t m_nCaptainLevel
constexpr uintptr_t OFF_GTEAM_MEMBER_LEVELS       = 0x0034; // int32_t m_nMemberLevel[7]
constexpr uintptr_t OFF_GTEAM_CAPTAIN_NAME        = 0x0050; // char m_szCaptainName[32] (TCVN3)
constexpr uintptr_t OFF_GTEAM_MEMBER_NAMES        = 0x0070; // char m_szMemberName[7][32] (TCVN3)
constexpr uintptr_t OFF_GTEAM_SERVER_ID           = 0x0150; // int32_t m_nTeamServerID
constexpr uintptr_t OFF_PLAYER_CUR_FACTION        = 0x5eb8; // rel to Player[1]: 0xe9e8 from base (0x0056f8b5)
constexpr uintptr_t OFF_PLAYER_FIRST_FACTION      = 0x5ec0; // rel to Player[1]: 0xe9f0 from base (0x0056f8bf)
constexpr uintptr_t OFF_PLAYER_CHAT               = 0x5ec8; // rel to Player[1]: KPlayerChat m_cChat (0x00566ced)
constexpr uintptr_t OFF_CHAT_CUR_CHANNEL          = 0x000c; // int32_t m_nCurChannel
constexpr uintptr_t OFF_CHAT_CHANNEL_MASK         = 0x0084; // uint32_t m_dwChannelMask
constexpr uintptr_t OFF_CHAT_TARGET_ID            = 0x0088; // uint32_t m_dwTargetID
constexpr uintptr_t OFF_CHAT_TARGET_IDX           = 0x008c; // int32_t m_nTargetIdx
constexpr uintptr_t OFF_PLAYER_PK                 = 0x6138; // rel to Player[1]: KPlayerPK m_cPK (0xec68 from base)
constexpr uintptr_t OFF_PK_NORMAL_FLAG            = 0x0000; // int32_t m_nNormalPKFlag
constexpr uintptr_t OFF_PK_ENMITY_STATE           = 0x0004; // int32_t m_nEnmityPKState (0=Close, 1=CountDown, 2=Open)
constexpr uintptr_t OFF_PK_ENMITY_AIM             = 0x0008; // int32_t m_nEnmityPKAim
constexpr uintptr_t OFF_PK_ENMITY_TIME            = 0x000c; // int32_t m_nEnmityPKTime
constexpr uintptr_t OFF_PK_EXERCISE_STATE         = 0x0030; // int32_t m_nExercisePKFlag
constexpr uintptr_t OFF_PK_EXERCISE_AIM           = 0x0034; // int32_t m_nExercisePKAim
constexpr uintptr_t OFF_PLAYER_PK_VALUE           = 0x6190; // rel to Player[1]: (0xec68 - 0x8b30) + 0x58 = 0xecc0 from base (KPlayerPK::SetPKValue @ 0x005ac73f)
constexpr uintptr_t OFF_PLAYER_WORLD_RANK         = 0x853c; // rel to Player[1]: 0x1106c from base (0x0056f8dd)

// KItemList (Player[1] + OFF_PLAYER_ITEM_LIST) & global Item[] (*ADDR_ITEM_ARRAY_PTR)
constexpr uintptr_t ADDR_ITEM_ARRAY_PTR           = 0x01341190; // KItem* (indices 1..511)
constexpr int       MAX_ITEM_COUNT                = 512;
constexpr uintptr_t STRIDE_ITEM                   = 0x057c;     // 1404 bytes
constexpr uintptr_t OFF_ITEMLIST_ITEMS            = 0x00a0;     // PlayerItem[] (stride 0x14: nIdx, nPlace, nX, nY, ...)
constexpr uintptr_t STRIDE_PLAYER_ITEM            = 0x0014;
constexpr uintptr_t OFF_ITEMLIST_USE_IDX          = 0x4c1c;     // KLinkArray m_UseIdx (0x0054ab05)
constexpr uintptr_t OFF_ITEM_GENRE                = 0x0004;     // int32_t m_CommonAttrib.nItemGenre
constexpr uintptr_t OFF_ITEM_EQUIP_TYPE           = 0x0008;     // int32_t
constexpr uintptr_t OFF_ITEM_DETAIL_TYPE          = 0x000c;     // int32_t m_CommonAttrib.nDetailType
constexpr uintptr_t OFF_ITEM_PARTICULAR_TYPE      = 0x0010;     // int32_t m_CommonAttrib.nParticularType
constexpr uintptr_t OFF_ITEM_OBJ_IDX              = 0x0014;     // int32_t m_CommonAttrib.nObjIdx
constexpr uintptr_t OFF_ITEM_STACKABLE            = 0x0018;     // int32_t m_CommonAttrib.bStack
constexpr uintptr_t OFF_ITEM_WIDTH                = 0x001c;     // int32_t m_CommonAttrib.nWidth
constexpr uintptr_t OFF_ITEM_HEIGHT               = 0x0020;     // int32_t m_CommonAttrib.nHeight
constexpr uintptr_t OFF_ITEM_PRICE                = 0x0024;     // int32_t m_CommonAttrib.nPrice
constexpr uintptr_t OFF_ITEM_LEVEL                = 0x0028;     // int32_t m_CommonAttrib.nLevel
constexpr uintptr_t OFF_ITEM_SERIES               = 0x002c;     // int32_t m_CommonAttrib.nSeries
constexpr uintptr_t OFF_ITEM_NAME                 = 0x0030;     // char[80] m_CommonAttrib.szItemName (TCVN3)
constexpr uintptr_t OFF_ITEM_SERVER_ID            = 0x03bc;     // uint32_t m_dwID
constexpr uintptr_t OFF_ITEM_CUR_DUR              = 0x03c0;     // int32_t m_nCurrentDur
constexpr uintptr_t OFF_ITEM_MAX_DUR              = 0x03c4;     // int32_t m_nMaxDur
constexpr uintptr_t OFF_ITEM_PARAM_STACK          = 0x03f4;     // int32_t stack/version param

// Ground Objects: KObjSet (0x01341c18) & global Object[] (*0x012c5588)
constexpr uintptr_t ADDR_OBJ_ARRAY_PTR            = 0x012c5588; // KObj*
constexpr uintptr_t ADDR_OBJ_SET                  = 0x01341c18; // KObjSet
constexpr uintptr_t OFF_OBJSET_USE_IDX            = 0x0088;     // KLinkArray m_UseIdx (0x005ad834)
constexpr int       MAX_OBJ_SCAN_GUARD            = 512;
constexpr uintptr_t STRIDE_OBJ                    = 0x03d4;     // 980 bytes
constexpr uintptr_t OFF_OBJ_ID                    = 0x0000;     // int32_t m_nID (server ObjectId >= 1000)
constexpr uintptr_t OFF_OBJ_DATA_ID               = 0x0004;     // int32_t m_nDataID
constexpr uintptr_t OFF_OBJ_KIND                  = 0x0008;     // int32_t m_nKind (2=Box, 3=Item, 4=Money)
constexpr uintptr_t OFF_OBJ_INDEX                 = 0x000c;     // int32_t m_nIndex (>0 when active)
constexpr uintptr_t OFF_OBJ_SUBWORLD_ID           = 0x0014;     // int32_t m_nSubWorldID
constexpr uintptr_t OFF_OBJ_REGION_IDX            = 0x0018;     // int32_t m_nRegionIdx
constexpr uintptr_t OFF_OBJ_MAP_X                 = 0x001c;     // int32_t m_nMapX
constexpr uintptr_t OFF_OBJ_MAP_Y                 = 0x0020;     // int32_t m_nMapY
constexpr uintptr_t OFF_OBJ_OFF_X                 = 0x0024;     // int32_t m_nOffX
constexpr uintptr_t OFF_OBJ_OFF_Y                 = 0x0028;     // int32_t m_nOffY
constexpr uintptr_t OFF_OBJ_NAME                  = 0x0060;     // char[32] m_szName (TCVN3)
constexpr uintptr_t OFF_OBJ_ITEM_WIDTH            = 0x008c;     // int32_t m_nItemWidth
constexpr uintptr_t OFF_OBJ_ITEM_HEIGHT           = 0x0090;     // int32_t m_nItemHeight
constexpr uintptr_t OFF_OBJ_MONEY_NUM             = 0x0094;     // int32_t m_nMoneyNum

// engine.dll IAT slots in game.exe
constexpr uintptr_t IAT_FILENAME_TO_ID            = 0x0063a5a8; // uint32_t (__cdecl*)(const char* szStr)
constexpr uintptr_t IAT_KLINKARRAY_GETNEXT        = 0x0063a5ac; // int (__thiscall*)(void* pLinkArray, int nIdx)

constexpr uintptr_t ADDR_NPC_ARRAY_PTR            = 0x012c5540;
constexpr int       MAX_NPC_COUNT                 = 256;
constexpr uintptr_t STRIDE_NPC                    = 0x113b8;
constexpr uintptr_t OFF_NPC_DW_ID                 = 0x0000;  // uint32_t
constexpr uintptr_t OFF_NPC_INDEX                 = 0x0004;  // int32_t (>0 when active)
constexpr uintptr_t OFF_NPC_LEVEL                 = 0x0020;  // int32_t
constexpr uintptr_t OFF_NPC_KIND                  = 0x0024;  // int32_t (0=fight/monster, 1=player, 3=dialoger NPC)
constexpr uintptr_t OFF_NPC_SERIES                = 0x0028;  // int32_t
constexpr uintptr_t OFF_NPC_CAMP                  = 0x00e4;  // int32_t
constexpr uintptr_t OFF_NPC_CURRENT_CAMP          = 0x00e8;  // int32_t
constexpr uintptr_t OFF_NPC_DOING                 = 0x00ec;  // int32_t
constexpr uintptr_t OFF_NPC_ACTIVE_AURA_SKILL     = 0x010c;  // int32_t m_ActiveAuraID (KNpc::SetAuraSkill @ 0x00577d90)
constexpr uintptr_t OFF_NPC_SKILL_LIST            = 0x0110;  // KSkillList m_SkillList (slots 1..79, stride 0x30)
constexpr int       MAX_NPC_SKILL_SLOT            = 80;      // 1..79
constexpr uintptr_t STRIDE_NPC_SKILL              = 0x0030;  // 48 bytes
constexpr uintptr_t OFF_NPCSKILL_ID               = 0x0008;  // int32_t SkillId
constexpr uintptr_t OFF_NPCSKILL_BASE_LEVEL       = 0x000c;  // int32_t Level
constexpr uintptr_t OFF_NPCSKILL_CUR_LEVEL        = 0x0020;  // int32_t CurrentLevel
constexpr uintptr_t OFF_NPC_SUBWORLD_INDEX        = 0x103c;  // int32_t
constexpr uintptr_t OFF_NPC_REGION_INDEX          = 0x1040;  // int32_t
constexpr uintptr_t OFF_NPC_CURRENT_LIFE          = 0x1048;  // int32_t
constexpr uintptr_t OFF_NPC_CURRENT_MANA          = 0x1058;  // int32_t
constexpr uintptr_t OFF_NPC_CURRENT_STAMINA       = 0x1060;  // int32_t
constexpr uintptr_t OFF_NPC_STAMINA_MAX           = 0x1064;  // int32_t
constexpr uintptr_t OFF_NPC_MAP_X                 = 0x12a8;  // int32_t
constexpr uintptr_t OFF_NPC_MAP_Y                 = 0x12ac;  // int32_t
constexpr uintptr_t OFF_NPC_OFF_X                 = 0x12b4;  // int32_t
constexpr uintptr_t OFF_NPC_OFF_Y                 = 0x12b8;  // int32_t
constexpr uintptr_t OFF_NPC_DES_X                 = 0x12bc;  // int32_t
constexpr uintptr_t OFF_NPC_DES_Y                 = 0x12c0;  // int32_t
constexpr uintptr_t OFF_NPC_NAME                  = 0x130d;  // char[32] TCVN3
constexpr uintptr_t OFF_NPC_PK_FLAG               = 0x1560;  // int32_t
constexpr uintptr_t OFF_NPC_FIGHT_MODE            = 0x1564;  // int32_t
constexpr uintptr_t OFF_NPC_PROCESS_AI            = 0x1728;  // int32_t (KNpc::m_ProcessAI; 0=disabled during hurt/lock, 1=enabled)
constexpr uintptr_t OFF_NPC_RIDE_HORSE            = 0x1778;  // int32_t
constexpr uintptr_t OFF_NPC_MENU_STATE_SPR        = 0x31e0;  // char[80] KNpcRes::m_cMenuStateSpr path (0x17c8 + 0x1a18 @ 0x005c5c40)
constexpr uintptr_t OFF_NPC_MENU_STATE            = 0x33d0;  // int32_t KNpcRes::m_nMenuState (0x17c8 + 0x1c08 @ 0x005c3e40)
constexpr uintptr_t OFF_NPC_CURRENT_LIFE_MAX      = 0x11300; // int32_t
constexpr uintptr_t OFF_NPC_LIFE_MAX              = 0x11304; // int32_t
constexpr uintptr_t OFF_NPC_MANA_MAX              = 0x11308; // int32_t
constexpr uintptr_t OFF_NPC_CURRENT_MANA_MAX      = 0x1130c; // int32_t

// -----------------------------------------------------------------------------
// Function addresses in game.exe
// -----------------------------------------------------------------------------
constexpr uintptr_t FN_KMYAPP_GAMELOOP            = 0x00528240; // int __thiscall (void* pApp)
constexpr uintptr_t FN_NET_CONNECT_GATEWAY        = 0x00528b40; // int __thiscall (void* pNetAgent, uint32_t ipv4NetOrder, int16_t port)
constexpr uintptr_t FN_NET_DISCONNECT_GATEWAY     = 0x00528c60; // void __fastcall (void* pNetAgent)
constexpr uintptr_t FN_NET_DISCONNECT_GAMESVR     = 0x00528ea0; // void __fastcall (void* pNetAgent)
constexpr uintptr_t FN_KLOGIN_REGISTER_TARGETS    = 0x0052bd30; // void __fastcall (void* pKLogin)
constexpr uintptr_t FN_KLOGIN_CREATE_CONN         = 0x005296f0; // void __thiscall (void* pKLogin, uint32_t ipv4NetOrder)
constexpr uintptr_t FN_KLOGIN_ACCOUNT_LOGIN       = 0x005297e0; // int __thiscall (void* pKLogin, const char* acct, const char* md5, char bSave)
constexpr uintptr_t FN_KLOGIN_SELECT_ROLE         = 0x00529890; // int __thiscall (void* pKLogin, int index)
constexpr uintptr_t FN_KLOGIN_CREATE_ROLE         = 0x00529a50; // void __thiscall (void* pKLogin, const void* role36B)

constexpr uintptr_t FN_CORESHELL_OP_REQUEST       = 0x0053b770; // int __stdcall (uint32_t uOper, uint32_t uParam, int32_t nParam)
constexpr uintptr_t FN_PROCESS_NET_MSG            = 0x00552970; // void __thiscall (void* pProto, const uint8_t* pMsg, size_t len)
constexpr uintptr_t FN_S2C_CHAT_GET_SOMEONE       = 0x00554210; // void __fastcall (void* pProto, void* edx, const uint8_t* pMsg, int32_t nParam)
constexpr uintptr_t FN_S2C_SCRIPT_ACTION_HANDLER  = 0x005554b0; // void __stdcall (const uint8_t* pMsg, int32_t nParam)
constexpr uintptr_t FN_SEND_NPC_DIALOG            = 0x00567580; // void __cdecl (int32_t npcArrayIndex)
constexpr uintptr_t FN_SUBWORLD_MAP2MPS           = 0x00570970; // void __thiscall (void* pSubWorld, int nRx, int nX, int nY, int nDx, int nDy, int* pMpsX, int* pMpsY)
constexpr uintptr_t FN_KNPC_ENABLE_PROCESS_AI     = 0x00579200; // void __thiscall (void* pNpc)
constexpr uintptr_t FN_KNPC_SEND_CMD_MOVE         = 0x00579700; // void __thiscall (void* pNpc, int32_t mpsX, int32_t mpsY, int32_t mode) [0=auto,1=walk,2=run]
constexpr uintptr_t FN_SCENE_SET_FOCUS_POSITION   = 0x005862a0; // void __thiscall (void* pScenePlace, int32_t nX, int32_t nY, int32_t nZ)
constexpr uintptr_t FN_SEND_NPC_SKILL             = 0x0058d220; // void __cdecl (int32_t skillId, int32_t mpsXOrMinus1, int32_t mpsYOrNpcDwId)
constexpr uintptr_t FN_SEND_SELL_ITEM             = 0x0058d5b0; // void __cdecl (uint32_t serverItemId) [0x5C, 13B]
constexpr uintptr_t FN_SEND_BUY_ITEM              = 0x0058d620; // void __cdecl (uint32_t buyIdx, uint8_t place, uint8_t x, uint8_t y) [0x5D, 12B]
constexpr uintptr_t FN_SEND_STORE_MONEY           = 0x0058d6e0; // void __cdecl (uint8_t direction, int32_t amount) [0x73, 14B: 0=deposit, 1=withdraw]
constexpr uintptr_t FN_SEND_MOVE_ITEM             = 0x0058d780; // void __cdecl (const void* pDownPos, const void* pUpPos)
constexpr uintptr_t FN_SEND_REPAIR_ITEM           = 0x0058d8c0; // void __cdecl (uint32_t serverItemId) [0x7A, 9B]
constexpr uintptr_t FN_BOX_SET_PASSWORD           = 0x0054ad20; // void __thiscall (void* pItemList, uint32_t pwd) [0x84 sub=0, 14B]
constexpr uintptr_t FN_BOX_CANCEL_PASSWORD        = 0x0054ad60; // void __thiscall (void* pItemList, uint32_t pwd) [0x84 sub=1, 14B]
constexpr uintptr_t FN_BOX_CHANGE_PASSWORD        = 0x0054ada0; // void __thiscall (void* pItemList, uint32_t oldPwd, uint32_t newPwd) [0x84 sub=2, 14B]
constexpr uintptr_t FN_BOX_LOCK                   = 0x0054ade0; // void __thiscall (void* pItemList) [0x84 sub=3, 14B]
constexpr uintptr_t FN_BOX_UNLOCK                 = 0x0054ae20; // void __thiscall (void* pItemList, uint32_t pwd) [0x84 sub=4, 14B]
constexpr uintptr_t FN_SEND_SWITCH_PK             = 0x005ac2a0; // void __thiscall (void* pPlayerPK, uint8_t pkMode)
constexpr uintptr_t FN_SEND_PK_TARGET             = 0x005ac390; // void __thiscall (void* pPlayerPK, uint32_t targetNpcDwId)
constexpr uintptr_t FN_UI_MSG_SEL_CLICK           = 0x00509180; // void __thiscall (void* pUiMsgSel, int32_t optionIndex)
constexpr uintptr_t FN_PLAYER_SEND_CHAT           = 0x00566c80; // void __thiscall (void* pPlayer, const void* pUiChatMsg, const char* szSentence)
constexpr uintptr_t FN_SEND_ADD_BASE_ATTR         = 0x00566d00; // void __stdcall (int32_t attr, int32_t points)
constexpr uintptr_t FN_PLAYER_ADD_SKILL_POINT     = 0x00569120; // int __thiscall (void* pPlayer, int32_t skillId, int32_t addPoints)
constexpr uintptr_t FN_PLAYER_APPLY_USE_ITEM      = 0x00569200; // int __thiscall (void* pPlayer, int32_t itemArrayIdx, int32_t place, int32_t x, int32_t y)
constexpr uintptr_t FN_PLAYER_PICKUP_OBJ          = 0x00569350; // void __thiscall (void* pPlayer, int32_t objArrayIdx)
constexpr uintptr_t FN_PLAYER_CLICK_OBJ           = 0x005694a0; // void __thiscall (void* pPlayer, int32_t objArrayIdx)
constexpr uintptr_t FN_PLAYER_THROW_AWAY_ITEM     = 0x00569780; // int __thiscall (void* pPlayer)
constexpr uintptr_t FN_CHAT_SEND_CHAT             = 0x005b54c0; // void __thiscall (void* pPlayerChat, const void* pUiChatMsg, const char* szSentence)
constexpr uintptr_t FN_CHAT_SEND_SOMEONE          = 0x005b5660; // void __thiscall (void* pPlayerChat, uint32_t targetDwId, int32_t targetIdx, const void* pUiChatMsg, const char* szSentence)

// Batch 2 (019-Party & 020-SocialRelations) functions & UI globals in game.exe
constexpr uintptr_t ADDR_UI_PLAYER_BAR_PTR        = 0x0068f94c; // KUiPlayerBar::m_pSelf
constexpr uintptr_t ADDR_UI_CHAT_CENTRE_PTR       = 0x006932a8; // KUiChatCentre::m_pSelf
constexpr uintptr_t ADDR_UI_MSG_CENTRE_PAD_PTR    = 0x00696410; // KUiMsgCentrePad::m_pSelf (@ 0x00490683)
constexpr uintptr_t ADDR_ACTIVATE_CHANNEL_COUNT   = 0x00696414; // int32_t m_nActivateChannels (@ 0x00490695)
constexpr uintptr_t ADDR_ACTIVATE_CHANNEL_ARRAY   = 0x00696418; // KChannelActivateInfo* (stride 0x2c2c, +0x000 cTitle, +0x410 uint32_t nChannelID)
constexpr size_t    STRIDE_ACTIVATE_CHANNEL       = 0x2c2c;     // (@ 0x004906a0)
constexpr size_t    OFF_ACTIVATE_CHANNEL_ID       = 0x0410;     // uint32_t nChannelID (@ 0x004906ac)
constexpr uintptr_t FN_UI_SEND_CHANNEL_MESSAGE    = 0x0044d300; // void __cdecl (uint32_t dwChannelId, const char* pBuf, int32_t nLen) [0xFB 0x21]
constexpr uintptr_t FN_UI_QUERY_ALL_CHANNEL       = 0x0048f790; // void __cdecl ()
constexpr uintptr_t FN_UI_CHECK_CHANNEL           = 0x0048fd40; // void __cdecl (int32_t nType) [0=TEAM, 1=Faction#, 2=Tong#]
constexpr uintptr_t FN_SEND_NEW_DATA_TO_CORE      = 0x00539c40; // int __thiscall (void* pCoreShell, const void* pBuf, uint32_t len)
constexpr uintptr_t FN_TEAM_APPLY_INFO            = 0x00566a50; // void __stdcall (uint32_t targetNpcDwId) [0x53 sub=0x01, 8B]
constexpr uintptr_t FN_TEAM_CREATE                = 0x00566ab0; // int __thiscall (void* pPlayer) [0x53 sub=0x02, 8B]
constexpr uintptr_t FN_TEAM_OPEN_CLOSE            = 0x0056ae10; // void __thiscall (void* pTeam, int32_t bOpen) via GOI 9 [0x53 sub=0x03, 8B]
constexpr uintptr_t FN_TEAM_APPLY_JOIN            = 0x00568fe0; // void __thiscall (void* pPlayer, int32_t targetNpcArrayIdx) [0x53 sub=0x04, 8B]
constexpr uintptr_t FN_TEAM_ACCEPT_MEMBER         = 0x00566ae0; // void __thiscall (void* pPlayer, uint32_t applicantNpcDwId) [0x53 sub=0x05, 8B]
constexpr uintptr_t FN_TEAM_LEAVE                 = 0x00566b80; // void __cdecl () [0x53 sub=0x06, 8B]
constexpr uintptr_t FN_TEAM_KICK_MEMBER           = 0x00566bc0; // void __thiscall (void* pPlayer, uint32_t memberNpcDwId) [0x53 sub=0x07, 8B]
constexpr uintptr_t FN_TEAM_APPOINT_CAPTAIN       = 0x00566c10; // void __thiscall (void* pPlayer, uint32_t newCaptainNpcDwId) [0x53 sub=0x08, 8B]
constexpr uintptr_t FN_TEAM_INVITE_ADD            = 0x0058d960; // void __stdcall (uint32_t targetNpcDwId) [0x53 sub=0x0A, 8B]
constexpr uintptr_t FN_TEAM_REPLY_INVITE          = 0x0058d9a0; // void __stdcall (int32_t inviterPlayerIdx, int32_t bAccept) [0x53 sub=0x0B, 9B]
constexpr uintptr_t FN_NPC_SET_AURA_SKILL         = 0x00577d90; // void __thiscall (void* pNpc, int32_t skillId) [0x6F, 5B]
constexpr uintptr_t FN_UI_SEND_SOMEONE_CHAT       = 0x0044d640; // void __cdecl (const char* szDestName, const char* szSentence, int32_t nLen) [0xFB 0x20]
constexpr uintptr_t FN_UI_DELETE_FRIEND           = 0x004797b0; // void __cdecl (int32_t nUnitIdx, const char* szRoleName, uint8_t bNotify)
constexpr uintptr_t FN_BLACKLIST_ADD              = 0x00405660; // void __cdecl (const char* szName, const char* szGroup, uint8_t bNotify)
constexpr uintptr_t FN_BLACKLIST_REMOVE           = 0x004057b0; // void __cdecl (const char* szName)
constexpr uintptr_t FN_BLACKLIST_CONTAINS         = 0x004058d0; // bool __cdecl (const char* szName)
constexpr uint32_t  GOI_FRIEND_INVITE             = 0x25;       // 37: uParam = const char* roleName -> 0xFC (pf_friend=7, id=0, 38B)
constexpr uint32_t  GOI_FRIEND_INVITE_RESPONSE    = 0x2A;       // 42: uParam = const char* roleName, nParam = (0=agree, 1=refuse) -> 0xFC (pf_friend=7, id=1, 39B)

// Batch 3 (021-P2PTrade & 022-PlayerStalls) & Batch 4 (023-PurpleCrafting & 025-Guilds) in game.exe
constexpr size_t    OFF_PLAYER_TRADE_STATE        = 0x5750;     // int32_t (0=closed, 2=open, 3=trading @ 0x005672c0)
constexpr size_t    OFF_PLAYER_TRADE_DEST_NPC     = 0x5754;     // uint32_t partner NPC DwId (@ 0x0056b075)
constexpr size_t    OFF_PLAYER_TONG               = 0x6194;     // KPlayerTong m_cTong (Player[1]+0x6194 = g_Player+0xecc4 @ 0x00556eab)
constexpr size_t    OFF_TONG_IN_TONG_FLAG         = 0x002c;     // int32_t m_bFlag (1 when in Tong @ 0x005b84ab)
constexpr size_t    OFF_TONG_FIGURE               = 0x0030;     // int32_t m_nFigure (0=Member, 1=Manager, 2=Director, 3=Master @ 0x005b88a6)
constexpr size_t    OFF_PLAYER_STALL_STATE        = 0x67b8;     // int32_t (Player[1]+0x67b8 = g_Player+0xf2e8 @ 0x0053d11a)
constexpr uintptr_t ADDR_STALL_VIEW_VEC_BEGIN     = 0x00680774; // Viewed stall 92B entries begin (@ 0x00548ef6)
constexpr uintptr_t ADDR_STALL_VIEW_VEC_END       = 0x00680778; // Viewed stall 92B entries end (@ 0x00548ef0)
constexpr uintptr_t ADDR_STALL_VIEW_SELLER_DWID   = 0x00680780; // uint32_t viewed stall seller DwId (@ 0x005480f2)
constexpr uintptr_t ADDR_STALL_TAX_RATE           = 0x00680788; // int32_t city stall tax rate (@ 0x0054912b)

constexpr uintptr_t FN_TRADE_OPEN                 = 0x005697e0; // void __thiscall (void* pPlayer, const char* msg, int32_t len) [0x69]
constexpr uintptr_t FN_TRADE_CLOSE                = 0x00566de0; // void __thiscall (void* pPlayer) [0x6A, 1B]
constexpr uintptr_t FN_TRADE_APPLY_START          = 0x00566e00; // void __stdcall (int32_t targetNpcArrayIdx) [0x6B, 9B w/ 0x566480 hash]
constexpr uintptr_t FN_TRADE_REPLY_START          = 0x005b60e0; // void __cdecl (int32_t inviterPlayerIdx, int32_t bAccept) [0x75, 10B w/ 0x566480 hash]
constexpr uintptr_t FN_TRADE_MOVE_MONEY           = 0x005698c0; // int __thiscall (void* pPlayer, int32_t money) [0x6C, 5B]
constexpr uintptr_t FN_TRADE_LOCK                 = 0x00569990; // void __thiscall (void* pPlayer, int32_t bLock) [0x6D 0x02, 2B]
constexpr uintptr_t FN_TRADE_DECISION             = 0x00569940; // void __thiscall (void* pPlayer, int32_t bOk) [0x6D 0x01/0x00, 2B]
constexpr uintptr_t FN_TONG_APPLY_CREATE          = 0x005b8250; // int __thiscall (void* pPlayerTong, int32_t camp, const char* szTongName) [0xFD sub=1, 14B]
constexpr uintptr_t FN_TONG_APPLY_ADD             = 0x005b8450; // int __thiscall (void* pPlayerTong, uint32_t targetNpcDwId) [0xFD sub=2, 8B]
constexpr uintptr_t FN_TONG_ACCEPT_MEMBER         = 0x005b88c0; // void __thiscall (void* pPlayerTong, int32_t playerIdx, uint32_t nameId, int32_t bAccept) [0xFD sub=3, 13B]
constexpr uintptr_t FN_TONG_APPLY_LEAVE           = 0x005b8e40; // void __thiscall (void* pPlayerTong) [0xFD sub=9, 4B]

constexpr uint32_t  GOI_TEAM_MANAGE_OPEN          = 0x09;       // 9: nParam = bOpen -> KPlayerTeam::ApplyManage (0x0056ae10)
constexpr uint32_t  GOI_ENCHASER_COMPOUND         = 0x3D;       // 61: uParam = const int32_t[5] -> C2S 0x7F (50B @ 0x0053cf7d)
constexpr uint32_t  GOI_STALL_SET_ADV             = 0x41;       // 65: uParam = const char* advTcvn3 -> C2S 0x7C sub=1
constexpr uint32_t  GOI_STALL_VIEW_ITEMS          = 0x42;       // 66: uParam = sellerNpcDwId -> C2S 0x7C sub=4 (8B)
constexpr uint32_t  GOI_STALL_VIEW_ADV            = 0x43;       // 67: uParam = sellerNpcDwId -> C2S 0x7C sub=5 (8B)
constexpr uint32_t  GOI_STALL_MARK_PRICE          = 0x45;       // 69: uParam = itemArrayIdx, nParam = price
constexpr uint32_t  GOI_STALL_CLOSE               = 0x46;       // 70: -> C2S 0x7C sub=3 (4B)
constexpr uint32_t  GOI_STALL_TOGGLE_OPEN         = 0x47;       // 71: -> C2S 0x7C sub=1 + sub=0
constexpr uint32_t  GOI_STALL_BUY                 = 0x48;       // 72: uParam = clientItemIdx -> C2S 0x7C sub=2 (28B)
constexpr uint32_t  GOI_FOUNDRY_REQUEST           = 0x65;       // 101: uParam = FoundryReq80B* -> C2S 0x92 (204B @ 0x00538ae0)
constexpr uint32_t  GOI_TONG_CREATE               = 0x68;       // 104: uParam = { uint8_t camp; char name[32]; }, nParam = 0 -> C2S 0x99 (37B @ 0x0053dbaa)
constexpr uint32_t  GOI_TONG_DISMISS              = 0x72;       // 114: -> C2S 0xA4 sub=8 (4B)
constexpr uint32_t  GOI_TONG_JOIN                 = 0x73;       // 115: uParam = const char* targetName -> C2S 0xA4 sub=9 (36B)
constexpr uint32_t  GOI_TONG_LEAVE                = 0x74;       // 116: uParam = const char* selfName -> C2S 0xA4 sub=10 (36B)
constexpr uint32_t  GOI_TONG_ACCEPT               = 0x75;       // 117: uParam = const char* memberName -> C2S 0xA4 sub=2 (52B)
constexpr uint32_t  GOI_TONG_KICK                 = 0x76;       // 118: uParam = const char* memberName -> C2S 0xA4 sub=3 (36B)

struct FoundryReq80B {
    uint8_t Action;
    uint8_t Pad[3];
    int32_t MainCount;
    int32_t MainItemIdx[8];
    int32_t CatalystCount;
    int32_t CatalystItemIdx[8];
    int32_t Option;
};
static_assert(sizeof(FoundryReq80B) == 80, "FoundryReq80B must match game.exe 0x00538ae0 offsets (+0, +4, +8, +0x28, +0x2C, +0x4C)");

struct ItemPos12B {
    int32_t Place;
    int32_t X;
    int32_t Y;
};
static_assert(sizeof(ItemPos12B) == 12, "ItemPos12B must be 12 bytes");

#pragma pack(push, 1)
struct RoleInfo36B {
    char Name[32];
    uint8_t Gender;
    uint8_t Series;
    uint16_t NativePlaceId;
};
#pragma pack(pop)
static_assert(sizeof(RoleInfo36B) == 36, "RoleInfo36B must be exactly 36 bytes");

} // namespace jx
