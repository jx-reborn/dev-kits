#include "AgentState.h"
#include "JsonMini.h"
#include "../include/jx_offsets.h"

#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <sstream>
#include <string>
#include <vector>

namespace jxagent {

namespace {

using PfnNetConnectGateway = int(__thiscall*)(void* pAgent, uint32_t ipv4NetOrder, int16_t port);
using PfnNetDisconnect = void(__fastcall*)(void* pAgent);
using PfnKLoginRegisterTargets = void(__fastcall*)(void* pKLogin);
using PfnKLoginAccountLogin = int(__thiscall*)(void* pKLogin, const char* acct, const char* md5, char bSave);
using PfnKLoginSelectRole = int(__thiscall*)(void* pKLogin, int index);
using PfnKLoginCreateRole = void(__thiscall*)(void* pKLogin, const void* pRole36B);
using PfnCoreShellOpRequest = int(__stdcall*)(uint32_t uOper, uint32_t uParam, int32_t nParam);
using PfnSendNpcDialog = void(__cdecl*)(int32_t npcArrayIndex);
using PfnSubWorldMap2Mps = void(__thiscall*)(void* pSubWorld, int32_t nRx, int32_t nX, int32_t nY, int32_t nDx, int32_t nDy, int32_t* pMpsX, int32_t* pMpsY);
using PfnNpcEnableProcessAi = void(__thiscall*)(void* pNpc);
using PfnNpcSendCmdMove = void(__thiscall*)(void* pNpc, int32_t mpsX, int32_t mpsY, int32_t mode);
using PfnSceneSetFocusPosition = void(__thiscall*)(void* pScenePlace, int32_t nX, int32_t nY, int32_t nZ);
using PfnSendNpcSkill = void(__cdecl*)(int32_t skillId, int32_t mpsXOrMinus1, int32_t mpsYOrNpcDwId);
using PfnSendSwitchPk = void(__thiscall*)(void* pPlayerPk, uint8_t pkMode);
using PfnSendPkTarget = void(__thiscall*)(void* pPlayerPk, uint32_t targetDwId);
using PfnUiMsgSelClick = void(__thiscall*)(void* pUiMsgSel, int32_t optionIndex);
using PfnStringToMd5 = int(__cdecl*)(char* out64, const char* rawPwd);
using PfnLinkArrayGetNext = int32_t(__thiscall*)(void* pLinkArray, int32_t nIdx);
using PfnSendMoveItem = void(__cdecl*)(const jx::ItemPos12B* pDownPos, const jx::ItemPos12B* pUpPos);
using PfnSendSellItem = void(__cdecl*)(uint32_t serverItemId);
using PfnSendBuyItem = void(__cdecl*)(uint32_t buyIdx, uint8_t place, uint8_t gridX, uint8_t gridY);
using PfnSendStoreMoney = void(__cdecl*)(uint8_t direction, int32_t amount);
using PfnSendRepairItem = void(__cdecl*)(uint32_t serverItemId);
using PfnBoxPass1 = void(__thiscall*)(void* pItemList, uint32_t pwd);
using PfnBoxPass2 = void(__thiscall*)(void* pItemList, uint32_t oldPwd, uint32_t newPwd);
using PfnBoxLock = void(__thiscall*)(void* pItemList);
using PfnPlayerSendChat = void(__thiscall*)(void* pPlayer, const void* pUiChatMsg, const char* szSentence);
using PfnSendAddBaseAttr = void(__stdcall*)(int32_t attr, int32_t points);
using PfnPlayerAddSkillPoint = int32_t(__thiscall*)(void* pPlayer, int32_t skillId, int32_t addPoints);
using PfnPlayerApplyUseItem = int32_t(__thiscall*)(void* pPlayer, int32_t itemArrayIdx, int32_t place, int32_t x, int32_t y);
using PfnPlayerPickUpObj = void(__thiscall*)(void* pPlayer, int32_t objArrayIdx);
using PfnPlayerClickObj = void(__thiscall*)(void* pPlayer, int32_t objArrayIdx);
using PfnPlayerThrowAwayItem = int32_t(__thiscall*)(void* pPlayer);
using PfnChatSendChat = void(__thiscall*)(void* pPlayerChat, const void* pUiChatMsg, const char* szSentence);
using PfnChatSendSomeone = void(__thiscall*)(void* pPlayerChat, uint32_t targetDwId, int32_t targetIdx, const void* pUiChatMsg, const char* szSentence);
using PfnIClientSendPack = int(__thiscall*)(void* pClient, const void* pBuf, uint32_t len);
using PfnSendNewDataToCore = int(__thiscall*)(void* pCoreShell, const void* pBuf, uint32_t len);
using PfnTeamApplyInfo = void(__stdcall*)(uint32_t targetNpcDwId);
using PfnTeamCreate = int(__thiscall*)(void* pPlayer);
using PfnTeamOpenClose = void(__thiscall*)(void* pPlayer, int32_t bOpen);
using PfnTeamApplyJoin = void(__thiscall*)(void* pPlayer, int32_t targetNpcArrayIdx);
using PfnTeamAcceptMember = void(__thiscall*)(void* pPlayer, uint32_t applicantNpcDwId);
using PfnTeamLeave = void(__cdecl*)();
using PfnTeamKickMember = void(__thiscall*)(void* pPlayer, uint32_t memberNpcDwId);
using PfnTeamAppointCaptain = void(__thiscall*)(void* pPlayer, uint32_t newCaptainNpcDwId);
using PfnTeamInviteAdd = void(__stdcall*)(uint32_t targetNpcDwId);
using PfnTeamReplyInvite = void(__stdcall*)(int32_t inviterPlayerIdx, int32_t bAccept);
using PfnNpcSetAuraSkill = void(__thiscall*)(void* pNpc, int32_t skillId);
using PfnUiSendChannelMsgToCore = void(__thiscall*)(
    void* pPlayerBar, uint32_t dwChannelId, uint32_t cost, uint32_t figure, const char* szSentence, int32_t nLen);
using PfnUiQueryAllChannel = void(__cdecl*)();
using PfnUiCheckChannel = void(__cdecl*)(int32_t nType);
using PfnUiFindActivateChannel = int32_t(__thiscall*)(void* pPad, const char* szType);
using PfnUiGetActivateChannelId = uint32_t(__cdecl*)(int32_t nActivateIdx);
using PfnUiSendSomeoneChat = void(__cdecl*)(const char* szDestName, const char* szSentence, int32_t nLen);
using PfnUiDeleteFriend = void(__cdecl*)(int32_t nUnitIdx, const char* szRoleName, uint8_t bNotify);
using PfnBlacklistAdd = void(__cdecl*)(const char* szName, const char* szGroup, uint8_t bNotify);
using PfnBlacklistRemove = void(__cdecl*)(const char* szName);
using PfnBlacklistContains = bool(__cdecl*)(const char* szName);
using PfnTradeOpen = void(__thiscall*)(void* pPlayer, const char* msg, int32_t len);
using PfnTradeClose = void(__thiscall*)(void* pPlayer);
using PfnTradeApplyStart = void(__stdcall*)(int32_t targetNpcArrayIdx);
using PfnTradeReplyStart = void(__cdecl*)(int32_t inviterPlayerIdx, int32_t bAccept);
using PfnTradeMoveMoney = int32_t(__thiscall*)(void* pPlayer, int32_t money);
using PfnTradeLock = void(__thiscall*)(void* pPlayer, int32_t bLock);
using PfnTradeDecision = void(__thiscall*)(void* pPlayer, int32_t bOk);
using PfnTongApplyCreate = int32_t(__thiscall*)(void* pPlayerTong, int32_t camp, const char* szTongName);
using PfnTongApplyAdd = int32_t(__thiscall*)(void* pPlayerTong, uint32_t targetNpcDwId);
using PfnTongAcceptMember = void(__thiscall*)(void* pPlayerTong, int32_t playerIdx, uint32_t nameId, int32_t bAccept);
using PfnTongApplyLeave = void(__thiscall*)(void* pPlayerTong);
using PfnFileNameToId = uint32_t(__cdecl*)(const char* szStr);

__declspec(noinline) bool SafeCopyMem(void* dst, const void* src, size_t size) {
    if (!dst || !src || size == 0) return false;
    __try {
        std::memcpy(dst, src, size);
        return true;
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        return false;
    }
}

template <typename T>
bool SafeRead(uintptr_t addr, T& out) {
    return SafeCopyMem(&out, reinterpret_cast<const void*>(addr), sizeof(T));
}

__declspec(noinline) bool SafeCallMap2Mps(
    void* pSubWorld, int32_t nRx, int32_t nX, int32_t nY, int32_t nDx, int32_t nDy, int32_t* outMpsX, int32_t* outMpsY) {
    if (!pSubWorld || !outMpsX || !outMpsY) return false;
    __try {
        auto fn = reinterpret_cast<PfnSubWorldMap2Mps>(jx::FN_SUBWORLD_MAP2MPS);
        fn(pSubWorld, nRx, nX, nY, nDx, nDy, outMpsX, outMpsY);
        return true;
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        *outMpsX = 0;
        *outMpsY = 0;
        return false;
    }
}

__declspec(noinline) int32_t SafeLinkArrayGetNext(void* pLinkArray, int32_t curIdx) {
    if (!pLinkArray) return 0;
    __try {
        auto fnPtr = *reinterpret_cast<PfnLinkArrayGetNext*>(jx::IAT_KLINKARRAY_GETNEXT);
        if (!fnPtr) return 0;
        return fnPtr(pLinkArray, curIdx);
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        return 0;
    }
}

uint32_t HashTongMemberNameToId(const char* s) {
    // Exact port of jx_linux_y g_HashString2Id(const char*) @ 0x08218030 (case-sensitive!)
    if (!s) return 0xFFFFFFFFu;
    if (s[0] == '\0') return 0x12345678u;
    uint32_t id = 0;
    for (uint32_t i = 0; s[i] != '\0'; ++i) {
        uint32_t ch = static_cast<uint32_t>(static_cast<int32_t>(static_cast<int8_t>(s[i])));
        uint32_t d = id + (i + 1u) * ch;
        if (d >= 0x8000000Bu) {
            d -= 0x8000000Bu;
        }
        id = d * 0xFFFFFFEFu;
    }
    return id ^ 0x12345678u;
}

bool ParseIpv4NetOrder(const std::string& ipStr, uint32_t& outNetOrder) {
    unsigned int a = 0, b = 0, c = 0, d = 0;
    char trailing = 0;
    if (std::sscanf(ipStr.c_str(), "%u.%u.%u.%u%c", &a, &b, &c, &d, &trailing) != 4) {
        return false;
    }
    if (a > 255 || b > 255 || c > 255 || d > 255) {
        return false;
    }
    outNetOrder = static_cast<uint32_t>(a | (b << 8) | (c << 16) | (d << 24));
    return true;
}

bool TryResolveNpcWorldCoords(
    const uint8_t* pNpc, uintptr_t subWorldBase,
    int32_t& outMapId, int32_t& outSubworldIdx, int32_t& outRegionIdx,
    int32_t& outMapX, int32_t& outMapY, int32_t& outMpsX, int32_t& outMpsY) {
    outMapId = 0;
    outSubworldIdx = -1;
    outRegionIdx = -1;
    outMapX = 0;
    outMapY = 0;
    outMpsX = 0;
    outMpsY = 0;

    uintptr_t npcAddr = reinterpret_cast<uintptr_t>(pNpc);
    int32_t offX = 0, offY = 0;
    if (!SafeRead(npcAddr + jx::OFF_NPC_SUBWORLD_INDEX, outSubworldIdx) ||
        !SafeRead(npcAddr + jx::OFF_NPC_REGION_INDEX, outRegionIdx) ||
        !SafeRead(npcAddr + jx::OFF_NPC_MAP_X, outMapX) ||
        !SafeRead(npcAddr + jx::OFF_NPC_MAP_Y, outMapY) ||
        !SafeRead(npcAddr + jx::OFF_NPC_OFF_X, offX) ||
        !SafeRead(npcAddr + jx::OFF_NPC_OFF_Y, offY)) {
        return false;
    }

    if (subWorldBase == 0 || outSubworldIdx < 0 || outSubworldIdx >= 8) {
        return false;
    }

    uintptr_t subWorldAddr = subWorldBase + static_cast<size_t>(outSubworldIdx) * jx::STRIDE_SUBWORLD;
    int32_t totalRegion = 0;
    if (!SafeRead(subWorldAddr + jx::OFF_SUBWORLD_ID, outMapId) ||
        !SafeRead(subWorldAddr + jx::OFF_SUBWORLD_TOTAL_REGION, totalRegion)) {
        return false;
    }

    if (outRegionIdx >= 0 && outRegionIdx < totalRegion) {
        SafeCallMap2Mps(
            reinterpret_cast<void*>(subWorldAddr),
            outRegionIdx, outMapX, outMapY, offX, offY,
            &outMpsX, &outMpsY);
    }
    return true;
}

std::string BuildGetStateJson() {
    auto& g = GetGlobals();

    uint32_t tickCount = 0;
    SafeRead(jx::ADDR_KMYAPP_GAMELOOP_COUNT, tickCount);

    uint8_t gwConnected = 0;
    uint8_t gsConnected = 0;
    uint8_t disconnecting = 0;
    SafeRead(jx::ADDR_NET_CONNECT_AGENT + jx::OFF_NET_GW_CONNECTED, gwConnected);
    SafeRead(jx::ADDR_NET_CONNECT_AGENT + jx::OFF_NET_GS_CONNECTED, gsConnected);
    SafeRead(jx::ADDR_NET_CONNECT_AGENT + jx::OFF_NET_DISCONNECTING, disconnecting);

    int32_t loginStatus = 0;
    int32_t loginResult = 0;
    int16_t numRoles = 0;
    char selRoleRaw[32] = {};
    SafeRead(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_STATUS, loginStatus);
    SafeRead(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_RESULT, loginResult);
    SafeRead(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_NUM_ROLE, numRoles);
    SafeCopyMem(selRoleRaw, reinterpret_cast<const void*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_SEL_ROLE_NAME), sizeof(selRoleRaw));
    if (numRoles < 0) numRoles = 0;
    if (numRoles > 24) numRoles = 24;

    uintptr_t playerBase = 0;
    uintptr_t npcBase = 0;
    uintptr_t subWorldBase = 0;
    SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
    SafeRead(jx::ADDR_NPC_ARRAY_PTR, npcBase);
    SafeRead(jx::ADDR_SUBWORLD_ARRAY_PTR, subWorldBase);

    int32_t playerNpcIndex = 0;
    int32_t playerMoney = 0;
    int32_t playerSaveMoney = 0;
    int32_t playerRunStatus = 0;
    int32_t playerPkValue = 0;
    int32_t playerLeadExp = 0;
    int32_t playerAttrPoints = 0;
    int32_t playerSkillPoints = 0;
    int32_t playerStrength = 0;
    int32_t playerDexterity = 0;
    int32_t playerVitality = 0;
    int32_t playerEnergy = 0;
    int32_t playerCurStrength = 0;
    int32_t playerCurDexterity = 0;
    int32_t playerCurVitality = 0;
    int32_t playerCurEnergy = 0;
    int64_t playerExp = 0;
    int32_t playerHandItemIdx = 0;
    int32_t playerCurFaction = -1;
    int32_t playerFirstFaction = -1;
    int32_t playerWorldRank = 0;
    int32_t playerLockMove = 0;
    uint8_t playerPauseSend = 0;
    int32_t playerPkNormalFlag = 0;
    int32_t playerEnmityState = 0;
    int32_t playerEnmityAim = 0;
    int32_t playerEnmityTime = 0;
    int32_t playerExerciseState = 0;
    int32_t playerExerciseAim = 0;
    uint32_t playerChatChannelMask = 0;

    if (playerBase != 0) {
        uintptr_t pPlayer1 = playerBase + jx::STRIDE_PLAYER;
        SafeRead(pPlayer1 + jx::OFF_PLAYER_NPC_INDEX, playerNpcIndex);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_PAUSE_SEND, playerPauseSend);
        SafeRead(pPlayer1 + 0x003c, playerLockMove);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_HAND_ITEM, playerHandItemIdx);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_MONEY, playerMoney);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_SAVE_MONEY, playerSaveMoney);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_RUN_STATUS, playerRunStatus);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_PK_VALUE, playerPkValue);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_EXP, playerExp);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_LEAD_EXP, playerLeadExp);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_ATTR_POINTS, playerAttrPoints);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_SKILL_POINTS, playerSkillPoints);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_STRENGTH, playerStrength);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_DEXTERITY, playerDexterity);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_VITALITY, playerVitality);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_ENERGY, playerEnergy);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_CUR_STRENGTH, playerCurStrength);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_CUR_DEXTERITY, playerCurDexterity);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_CUR_VITALITY, playerCurVitality);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_CUR_ENERGY, playerCurEnergy);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_CUR_FACTION, playerCurFaction);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_FIRST_FACTION, playerFirstFaction);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_WORLD_RANK, playerWorldRank);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_PK + jx::OFF_PK_NORMAL_FLAG, playerPkNormalFlag);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_PK + jx::OFF_PK_ENMITY_STATE, playerEnmityState);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_PK + jx::OFF_PK_ENMITY_AIM, playerEnmityAim);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_PK + jx::OFF_PK_ENMITY_TIME, playerEnmityTime);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_PK + jx::OFF_PK_EXERCISE_STATE, playerExerciseState);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_PK + jx::OFF_PK_EXERCISE_AIM, playerExerciseAim);
        SafeRead(pPlayer1 + jx::OFF_PLAYER_CHAT + jx::OFF_CHAT_CHANNEL_MASK, playerChatChannelMask);
    }

    bool inWorld = (npcBase != 0 && subWorldBase != 0 && playerNpcIndex > 0 && playerNpcIndex < jx::MAX_NPC_COUNT);
    uint32_t selfDwId = 0;
    char selfNameRaw[32] = {};
    int32_t selfLevel = 0, selfKind = 0, selfSeries = 0, selfCamp = 0, selfCurCamp = 0, selfDoing = 0;
    int32_t selfLife = 0, selfLifeMax = 0, selfMana = 0, selfManaMax = 0, selfStamina = 0, selfStaminaMax = 0;
    int32_t selfPkFlag = 0, selfFightMode = 0, selfProcessAi = 1, selfRideHorse = 0;
    int32_t selfMapId = 0, selfSubIdx = -1, selfRegionIdx = -1, selfMapX = 0, selfMapY = 0, selfMpsX = 0, selfMpsY = 0;
    int32_t selfDesX = 0, selfDesY = 0;
    int32_t selfNodeRef = -1, selfFreezeTime = 0, selfSubCenterSlot = -1, selfRegConLeft = -2;
    int32_t selfMenuState = 0;
    char selfMenuStateSpr[80] = {};
    uint32_t selfRegDwId = 0;
    uint8_t selfNoMoveByte = 0;

    if (inWorld) {
        uintptr_t selfNpcAddr = npcBase + static_cast<size_t>(playerNpcIndex) * jx::STRIDE_NPC;
        int32_t selfActiveIdx = 0;
        if (SafeRead(selfNpcAddr + jx::OFF_NPC_INDEX, selfActiveIdx) && selfActiveIdx > 0) {
            SafeRead(selfNpcAddr + jx::OFF_NPC_DW_ID, selfDwId);
            SafeRead(selfNpcAddr + 0x001c, selfNodeRef);
            SafeRead(selfNpcAddr + 0x1295, selfNoMoveByte);
            SafeRead(selfNpcAddr + 0x1424, selfFreezeTime);
            SafeCopyMem(selfNameRaw, reinterpret_cast<const void*>(selfNpcAddr + jx::OFF_NPC_NAME), sizeof(selfNameRaw));
            SafeRead(selfNpcAddr + jx::OFF_NPC_MENU_STATE, selfMenuState);
            SafeCopyMem(selfMenuStateSpr, reinterpret_cast<const void*>(selfNpcAddr + jx::OFF_NPC_MENU_STATE_SPR), sizeof(selfMenuStateSpr) - 1);
            SafeRead(selfNpcAddr + jx::OFF_NPC_LEVEL, selfLevel);
            SafeRead(selfNpcAddr + jx::OFF_NPC_KIND, selfKind);
            SafeRead(selfNpcAddr + jx::OFF_NPC_SERIES, selfSeries);
            SafeRead(selfNpcAddr + jx::OFF_NPC_CAMP, selfCamp);
            SafeRead(selfNpcAddr + jx::OFF_NPC_CURRENT_CAMP, selfCurCamp);
            SafeRead(selfNpcAddr + jx::OFF_NPC_DOING, selfDoing);
            SafeRead(selfNpcAddr + jx::OFF_NPC_CURRENT_LIFE, selfLife);
            SafeRead(selfNpcAddr + jx::OFF_NPC_CURRENT_LIFE_MAX, selfLifeMax);
            SafeRead(selfNpcAddr + jx::OFF_NPC_CURRENT_MANA, selfMana);
            SafeRead(selfNpcAddr + jx::OFF_NPC_CURRENT_MANA_MAX, selfManaMax);
            SafeRead(selfNpcAddr + jx::OFF_NPC_CURRENT_STAMINA, selfStamina);
            SafeRead(selfNpcAddr + jx::OFF_NPC_STAMINA_MAX, selfStaminaMax);
            SafeRead(selfNpcAddr + jx::OFF_NPC_PK_FLAG, selfPkFlag);
            SafeRead(selfNpcAddr + jx::OFF_NPC_FIGHT_MODE, selfFightMode);
            SafeRead(selfNpcAddr + jx::OFF_NPC_PROCESS_AI, selfProcessAi);
            SafeRead(selfNpcAddr + jx::OFF_NPC_RIDE_HORSE, selfRideHorse);
            SafeRead(selfNpcAddr + jx::OFF_NPC_DES_X, selfDesX);
            SafeRead(selfNpcAddr + jx::OFF_NPC_DES_Y, selfDesY);
            TryResolveNpcWorldCoords(
                reinterpret_cast<const uint8_t*>(selfNpcAddr), subWorldBase,
                selfMapId, selfSubIdx, selfRegionIdx, selfMapX, selfMapY, selfMpsX, selfMpsY);
            if (selfSubIdx >= 0 && selfSubIdx < 8) {
                uintptr_t swAddr = subWorldBase + static_cast<size_t>(selfSubIdx) * jx::STRIDE_SUBWORLD;
                SafeRead(swAddr + 0x006c, selfSubCenterSlot);
                uintptr_t regBase = 0;
                int32_t totalReg = 0;
                if (SafeRead(swAddr + jx::OFF_SUBWORLD_REGION_PTR, regBase) &&
                    SafeRead(swAddr + jx::OFF_SUBWORLD_TOTAL_REGION, totalReg) &&
                    regBase != 0 && selfRegionIdx >= 0 && selfRegionIdx < totalReg) {
                    uintptr_t regAddr = regBase + static_cast<size_t>(selfRegionIdx) * jx::STRIDE_KREGION;
                    SafeRead(regAddr + 0x0004, selfRegDwId);
                    SafeRead(regAddr + 0x0040, selfRegConLeft);
                }
            }
        } else {
            inWorld = false;
        }
    }

    uintptr_t centerRegionPtr = 0;
    bool regionLoaded = SafeRead(jx::ADDR_SCENE_PLACE + jx::OFF_SCENE_CENTER_REGION_PTR, centerRegionPtr) && (centerRegionPtr != 0);
    int32_t sceneFocusRx = 0, sceneFocusRy = 0, sceneFirstToLoad = -1, sceneProcMask = 0;
    SafeRead(jx::ADDR_SCENE_PLACE + jx::OFF_SCENE_FOCUS_REGION_X, sceneFocusRx);
    SafeRead(jx::ADDR_SCENE_PLACE + jx::OFF_SCENE_FOCUS_REGION_Y, sceneFocusRy);
    SafeRead(jx::ADDR_SCENE_PLACE + jx::OFF_SCENE_FIRST_TO_LOAD_IDX, sceneFirstToLoad);
    for (int i = 0; i < 9; ++i) {
        uintptr_t pReg = 0;
        if (SafeRead(jx::ADDR_SCENE_PLACE + jx::OFF_SCENE_PROC_REGIONS_BASE + static_cast<size_t>(i) * 4, pReg) && pReg != 0) {
            sceneProcMask |= (1 << i);
        }
    }
    bool syncReady = g.worldSyncReady.load(std::memory_order_acquire);

    std::ostringstream oss;
    oss << "{";
    oss << "\"pid\":" << GetCurrentProcessId() << ",";
    oss << "\"tick_count\":" << tickCount << ",";
    oss << "\"hwnd\":" << reinterpret_cast<uintptr_t>(g.mainHwnd) << ",";
    oss << "\"window_visible\":" << (g.mainHwnd && IsWindowVisible(g.mainHwnd) ? "true" : "false") << ",";

    oss << "\"net\":{";
    oss << "\"gw_connected\":" << (gwConnected ? "true" : "false") << ",";
    oss << "\"gs_connected\":" << (gsConnected ? "true" : "false") << ",";
    oss << "\"disconnecting\":" << (disconnecting ? "true" : "false");
    oss << "},";

    oss << "\"login\":{";
    oss << "\"status\":" << loginStatus << ",";
    oss << "\"result\":" << loginResult << ",";
    oss << "\"num_roles\":" << numRoles << ",";
    oss << "\"selected_role_tcvn3_hex\":" << jxjson::QuoteJson(jxjson::ToHexCStr(selRoleRaw, sizeof(selRoleRaw))) << ",";
    oss << "\"roles\":[";
    for (int16_t i = 0; i < numRoles; ++i) {
        jx::RoleInfo36B role{};
        SafeCopyMem(
            &role,
            reinterpret_cast<const void*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_ROLES + static_cast<size_t>(i) * jx::STRIDE_KLOGIN_ROLE),
            sizeof(role));
        if (i > 0) oss << ",";
        oss << "{";
        oss << "\"index\":" << i << ",";
        oss << "\"name_tcvn3_hex\":" << jxjson::QuoteJson(jxjson::ToHexCStr(role.Name, sizeof(role.Name))) << ",";
        oss << "\"gender\":" << static_cast<int>(role.Gender) << ",";
        oss << "\"series\":" << static_cast<int>(role.Series) << ",";
        oss << "\"native_place_id\":" << role.NativePlaceId;
        oss << "}";
    }
    oss << "]},";

    oss << "\"player\":{";
    oss << "\"in_world\":" << (inWorld ? "true" : "false") << ",";
    oss << "\"npc_index\":" << playerNpcIndex << ",";
    oss << "\"dw_id\":" << selfDwId << ",";
    oss << "\"name_tcvn3_hex\":" << jxjson::QuoteJson(jxjson::ToHexCStr(selfNameRaw, sizeof(selfNameRaw))) << ",";
    oss << "\"level\":" << selfLevel << ",";
    oss << "\"kind\":" << selfKind << ",";
    oss << "\"series\":" << selfSeries << ",";
    oss << "\"camp\":" << selfCamp << ",";
    oss << "\"current_camp\":" << selfCurCamp << ",";
    oss << "\"doing\":" << selfDoing << ",";
    oss << "\"life\":" << selfLife << ",";
    oss << "\"life_max\":" << selfLifeMax << ",";
    oss << "\"mana\":" << selfMana << ",";
    oss << "\"mana_max\":" << selfManaMax << ",";
    oss << "\"stamina\":" << selfStamina << ",";
    oss << "\"stamina_max\":" << selfStaminaMax << ",";
    oss << "\"money\":" << playerMoney << ",";
    oss << "\"save_money\":" << playerSaveMoney << ",";
    oss << "\"run_status\":" << playerRunStatus << ",";
    oss << "\"pk_flag\":" << playerPkNormalFlag << ",";
    oss << "\"pk_value\":" << playerPkValue << ",";
    oss << "\"enmity_state\":" << playerEnmityState << ",";
    oss << "\"enmity_aim\":" << playerEnmityAim << ",";
    oss << "\"enmity_time\":" << playerEnmityTime << ",";
    oss << "\"exercise_state\":" << playerExerciseState << ",";
    oss << "\"exercise_aim\":" << playerExerciseAim << ",";
    oss << "\"chat_channel_mask\":" << playerChatChannelMask << ",";
    oss << "\"exp\":" << playerExp << ",";
    oss << "\"lead_exp\":" << playerLeadExp << ",";
    oss << "\"attr_points\":" << playerAttrPoints << ",";
    oss << "\"skill_points\":" << playerSkillPoints << ",";
    oss << "\"strength\":" << playerStrength << ",";
    oss << "\"dexterity\":" << playerDexterity << ",";
    oss << "\"vitality\":" << playerVitality << ",";
    oss << "\"energy\":" << playerEnergy << ",";
    oss << "\"cur_strength\":" << playerCurStrength << ",";
    oss << "\"cur_dexterity\":" << playerCurDexterity << ",";
    oss << "\"cur_vitality\":" << playerCurVitality << ",";
    oss << "\"cur_energy\":" << playerCurEnergy << ",";
    oss << "\"hand_item_idx\":" << playerHandItemIdx << ",";
    oss << "\"cur_faction\":" << playerCurFaction << ",";
    oss << "\"first_faction\":" << playerFirstFaction << ",";
    oss << "\"world_rank\":" << playerWorldRank << ",";
    oss << "\"fight_mode\":" << selfFightMode << ",";
    oss << "\"ride_horse\":" << selfRideHorse << ",";
    oss << "\"map_id\":" << selfMapId << ",";
    oss << "\"subworld_idx\":" << selfSubIdx << ",";
    oss << "\"region_idx\":" << selfRegionIdx << ",";
    oss << "\"map_x\":" << selfMapX << ",";
    oss << "\"map_y\":" << selfMapY << ",";
    oss << "\"mps_x\":" << selfMpsX << ",";
    oss << "\"mps_y\":" << selfMpsY << ",";
    oss << "\"des_x\":" << selfDesX << ",";
    oss << "\"des_y\":" << selfDesY << ",";
    oss << "\"scene_focus_rx\":" << sceneFocusRx << ",";
    oss << "\"scene_focus_ry\":" << sceneFocusRy << ",";
    oss << "\"scene_first_to_load\":" << sceneFirstToLoad << ",";
    oss << "\"scene_proc_mask\":" << sceneProcMask << ",";
    oss << "\"process_ai\":" << selfProcessAi << ",";
    oss << "\"lock_move\":" << playerLockMove << ",";
    oss << "\"pause_send\":" << static_cast<int>(playerPauseSend) << ",";
    oss << "\"npc_node_ref\":" << selfNodeRef << ",";
    oss << "\"no_move_byte\":" << static_cast<int>(selfNoMoveByte) << ",";
    oss << "\"freeze_time\":" << selfFreezeTime << ",";
    oss << "\"subworld_center_slot\":" << selfSubCenterSlot << ",";
    oss << "\"reg_dw_id\":" << selfRegDwId << ",";
    oss << "\"reg_con_left\":" << selfRegConLeft << ",";
    oss << "\"sync_ready\":" << (syncReady ? "true" : "false") << ",";
    oss << "\"region_loaded\":" << (regionLoaded ? "true" : "false") << ",";
    oss << "\"menu_state\":" << selfMenuState << ",";
    oss << "\"menu_state_spr\":" << jxjson::QuoteJson(selfMenuStateSpr);
    oss << "},";

    oss << "\"npcs\":[";
    if (npcBase != 0) {
        bool firstNpc = true;
        for (int i = 1; i < jx::MAX_NPC_COUNT; ++i) {
            uintptr_t npcAddr = npcBase + static_cast<size_t>(i) * jx::STRIDE_NPC;
            int32_t idxVal = 0;
            uint32_t dwId = 0;
            if (!SafeRead(npcAddr + jx::OFF_NPC_INDEX, idxVal) || idxVal <= 0) continue;
            if (!SafeRead(npcAddr + jx::OFF_NPC_DW_ID, dwId) || dwId == 0) continue;

            char nName[32] = {};
            char nMenuSpr[80] = {};
            int32_t nKind = 0, nLevel = 0, nSeries = 0, nCamp = 0, nCurCamp = 0, nDoing = 0;
            int32_t nLife = 0, nLifeMax = 0, nPkFlag = 0, nFightMode = 0, nMenuState = 0;
            int32_t nMapId = 0, nSubIdx = -1, nRegIdx = -1, nMapX = 0, nMapY = 0, nMpsX = 0, nMpsY = 0;

            SafeCopyMem(nName, reinterpret_cast<const void*>(npcAddr + jx::OFF_NPC_NAME), sizeof(nName));
            SafeRead(npcAddr + jx::OFF_NPC_MENU_STATE, nMenuState);
            SafeCopyMem(nMenuSpr, reinterpret_cast<const void*>(npcAddr + jx::OFF_NPC_MENU_STATE_SPR), sizeof(nMenuSpr) - 1);
            SafeRead(npcAddr + jx::OFF_NPC_KIND, nKind);
            SafeRead(npcAddr + jx::OFF_NPC_LEVEL, nLevel);
            SafeRead(npcAddr + jx::OFF_NPC_SERIES, nSeries);
            SafeRead(npcAddr + jx::OFF_NPC_CAMP, nCamp);
            SafeRead(npcAddr + jx::OFF_NPC_CURRENT_CAMP, nCurCamp);
            SafeRead(npcAddr + jx::OFF_NPC_DOING, nDoing);
            SafeRead(npcAddr + jx::OFF_NPC_CURRENT_LIFE, nLife);
            SafeRead(npcAddr + jx::OFF_NPC_CURRENT_LIFE_MAX, nLifeMax);
            SafeRead(npcAddr + jx::OFF_NPC_PK_FLAG, nPkFlag);
            SafeRead(npcAddr + jx::OFF_NPC_FIGHT_MODE, nFightMode);
            TryResolveNpcWorldCoords(
                reinterpret_cast<const uint8_t*>(npcAddr), subWorldBase,
                nMapId, nSubIdx, nRegIdx, nMapX, nMapY, nMpsX, nMpsY);

            if (!firstNpc) oss << ",";
            firstNpc = false;
            oss << "{";
            oss << "\"array_index\":" << i << ",";
            oss << "\"is_self\":" << (i == playerNpcIndex ? "true" : "false") << ",";
            oss << "\"dw_id\":" << dwId << ",";
            oss << "\"name_tcvn3_hex\":" << jxjson::QuoteJson(jxjson::ToHexCStr(nName, sizeof(nName))) << ",";
            oss << "\"kind\":" << nKind << ",";
            oss << "\"level\":" << nLevel << ",";
            oss << "\"series\":" << nSeries << ",";
            oss << "\"camp\":" << nCamp << ",";
            oss << "\"current_camp\":" << nCurCamp << ",";
            oss << "\"doing\":" << nDoing << ",";
            oss << "\"life\":" << nLife << ",";
            oss << "\"life_max\":" << nLifeMax << ",";
            oss << "\"pk_flag\":" << nPkFlag << ",";
            oss << "\"fight_mode\":" << nFightMode << ",";
            oss << "\"mps_x\":" << nMpsX << ",";
            oss << "\"mps_y\":" << nMpsY << ",";
            oss << "\"menu_state\":" << nMenuState << ",";
            oss << "\"menu_state_spr\":" << jxjson::QuoteJson(nMenuSpr);
            oss << "}";
        }
    }
    oss << "],";

    oss << "\"ground_objects\":[";
    uintptr_t objBase = 0;
    SafeRead(jx::ADDR_OBJ_ARRAY_PTR, objBase);
    if (objBase != 0 && subWorldBase != 0) {
        bool firstObj = true;
        void* pObjUseIdx = reinterpret_cast<void*>(jx::ADDR_OBJ_SET + jx::OFF_OBJSET_USE_IDX);
        int32_t objIdx = SafeLinkArrayGetNext(pObjUseIdx, 0);
        for (int guard = 0; objIdx > 0 && guard < jx::MAX_OBJ_SCAN_GUARD; ++guard) {
            uintptr_t objAddr = objBase + static_cast<size_t>(objIdx) * jx::STRIDE_OBJ;
            int32_t activeIdx = 0, objId = 0, dataId = 0, kind = 0;
            if (SafeRead(objAddr + jx::OFF_OBJ_INDEX, activeIdx) && activeIdx > 0 &&
                SafeRead(objAddr + jx::OFF_OBJ_ID, objId) && objId > 0) {
                int32_t swIdx = 0, regIdx = -1, mapX = 0, mapY = 0, offX = 0, offY = 0;
                int32_t itemW = 0, itemH = 0, moneyNum = 0, mpsX = 0, mpsY = 0;
                char objName[32] = {};
                SafeRead(objAddr + jx::OFF_OBJ_DATA_ID, dataId);
                SafeRead(objAddr + jx::OFF_OBJ_KIND, kind);
                SafeRead(objAddr + jx::OFF_OBJ_SUBWORLD_ID, swIdx);
                SafeRead(objAddr + jx::OFF_OBJ_REGION_IDX, regIdx);
                SafeRead(objAddr + jx::OFF_OBJ_MAP_X, mapX);
                SafeRead(objAddr + jx::OFF_OBJ_MAP_Y, mapY);
                SafeRead(objAddr + jx::OFF_OBJ_OFF_X, offX);
                SafeRead(objAddr + jx::OFF_OBJ_OFF_Y, offY);
                SafeCopyMem(objName, reinterpret_cast<const void*>(objAddr + jx::OFF_OBJ_NAME), sizeof(objName));
                SafeRead(objAddr + jx::OFF_OBJ_ITEM_WIDTH, itemW);
                SafeRead(objAddr + jx::OFF_OBJ_ITEM_HEIGHT, itemH);
                SafeRead(objAddr + jx::OFF_OBJ_MONEY_NUM, moneyNum);

                if (swIdx >= 0 && swIdx < 8) {
                    uintptr_t swAddr = subWorldBase + static_cast<size_t>(swIdx) * jx::STRIDE_SUBWORLD;
                    int32_t totalReg = 0;
                    if (SafeRead(swAddr + jx::OFF_SUBWORLD_TOTAL_REGION, totalReg) &&
                        regIdx >= 0 && regIdx < totalReg) {
                        SafeCallMap2Mps(
                            reinterpret_cast<void*>(swAddr),
                            regIdx, mapX, mapY, offX, offY,
                            &mpsX, &mpsY);
                    }
                }

                if (!firstObj) oss << ",";
                firstObj = false;
                oss << "{";
                oss << "\"array_index\":" << objIdx << ",";
                oss << "\"object_id\":" << objId << ",";
                oss << "\"data_id\":" << dataId << ",";
                oss << "\"kind\":" << kind << ",";
                oss << "\"name_tcvn3_hex\":" << jxjson::QuoteJson(jxjson::ToHexCStr(objName, sizeof(objName))) << ",";
                oss << "\"item_width\":" << itemW << ",";
                oss << "\"item_height\":" << itemH << ",";
                oss << "\"money_amount\":" << moneyNum << ",";
                oss << "\"mps_x\":" << mpsX << ",";
                oss << "\"mps_y\":" << mpsY;
                oss << "}";
            }
            objIdx = SafeLinkArrayGetNext(pObjUseIdx, objIdx);
        }
    }
    oss << "],";

    oss << "\"items\":[";
    uintptr_t itemBase = 0;
    SafeRead(jx::ADDR_ITEM_ARRAY_PTR, itemBase);
    if (playerBase != 0 && itemBase != 0) {
        uintptr_t pItemList = playerBase + jx::STRIDE_PLAYER + jx::OFF_PLAYER_ITEM_LIST;
        void* pItemUseIdx = reinterpret_cast<void*>(pItemList + jx::OFF_ITEMLIST_USE_IDX);
        bool firstItem = true;
        bool handListed = false;

        auto appendItemJson = [&](int32_t itemIdx, int32_t place, int32_t gridX, int32_t gridY) {
            if (itemIdx <= 0 || itemIdx >= jx::MAX_ITEM_COUNT) return;
            uintptr_t itemAddr = itemBase + static_cast<size_t>(itemIdx) * jx::STRIDE_ITEM;
            uint32_t serverId = 0;
            if (!SafeRead(itemAddr + jx::OFF_ITEM_SERVER_ID, serverId) || serverId == 0) return;

            int32_t genre = 0, detail = 0, particular = 0, level = 0, series = 0;
            int32_t width = 0, height = 0, curDur = -1, maxDur = -1, stackParam = 0;
            char itemName[80] = {};
            SafeRead(itemAddr + jx::OFF_ITEM_GENRE, genre);
            SafeRead(itemAddr + jx::OFF_ITEM_DETAIL_TYPE, detail);
            SafeRead(itemAddr + jx::OFF_ITEM_PARTICULAR_TYPE, particular);
            SafeRead(itemAddr + jx::OFF_ITEM_LEVEL, level);
            SafeRead(itemAddr + jx::OFF_ITEM_SERIES, series);
            SafeRead(itemAddr + jx::OFF_ITEM_WIDTH, width);
            SafeRead(itemAddr + jx::OFF_ITEM_HEIGHT, height);
            SafeRead(itemAddr + jx::OFF_ITEM_CUR_DUR, curDur);
            SafeRead(itemAddr + jx::OFF_ITEM_MAX_DUR, maxDur);
            SafeRead(itemAddr + jx::OFF_ITEM_PARAM_STACK, stackParam);
            SafeCopyMem(itemName, reinterpret_cast<const void*>(itemAddr + jx::OFF_ITEM_NAME), sizeof(itemName));

            if (!firstItem) oss << ",";
            firstItem = false;
            oss << "{";
            oss << "\"array_index\":" << itemIdx << ",";
            oss << "\"server_item_id\":" << serverId << ",";
            oss << "\"place\":" << place << ",";
            oss << "\"grid_x\":" << gridX << ",";
            oss << "\"grid_y\":" << gridY << ",";
            oss << "\"genre\":" << genre << ",";
            oss << "\"detail_type\":" << detail << ",";
            oss << "\"particular_type\":" << particular << ",";
            oss << "\"level\":" << level << ",";
            oss << "\"series\":" << series << ",";
            oss << "\"width\":" << width << ",";
            oss << "\"height\":" << height << ",";
            oss << "\"cur_durability\":" << curDur << ",";
            oss << "\"max_durability\":" << maxDur << ",";
            oss << "\"stack_count\":" << stackParam << ",";
            oss << "\"name_tcvn3_hex\":" << jxjson::QuoteJson(jxjson::ToHexCStr(itemName, sizeof(itemName)));
            oss << "}";
        };

        int32_t slotIdx = SafeLinkArrayGetNext(pItemUseIdx, 0);
        for (int guard = 0; slotIdx > 0 && guard < jx::MAX_ITEM_COUNT; ++guard) {
            uintptr_t entryAddr = pItemList + jx::OFF_ITEMLIST_ITEMS + static_cast<size_t>(slotIdx) * jx::STRIDE_PLAYER_ITEM;
            int32_t itemIdx = 0, place = 0, gx = 0, gy = 0;
            if (SafeRead(entryAddr + 0x00, itemIdx) && itemIdx > 0 && itemIdx < jx::MAX_ITEM_COUNT) {
                SafeRead(entryAddr + 0x04, place);
                SafeRead(entryAddr + 0x08, gx);
                SafeRead(entryAddr + 0x0c, gy);
                if (itemIdx == playerHandItemIdx) handListed = true;
                appendItemJson(itemIdx, place, gx, gy);
            }
            slotIdx = SafeLinkArrayGetNext(pItemUseIdx, slotIdx);
        }

        if (playerHandItemIdx > 0 && !handListed) {
            appendItemJson(playerHandItemIdx, 1, 0, 0);
        }
    }
    oss << "],";

    oss << "\"skills\":[";
    if (inWorld) {
        uintptr_t selfNpcAddr = npcBase + static_cast<size_t>(playerNpcIndex) * jx::STRIDE_NPC;
        uintptr_t skillListBase = selfNpcAddr + jx::OFF_NPC_SKILL_LIST;
        bool firstSkill = true;
        for (int k = 1; k < jx::MAX_NPC_SKILL_SLOT; ++k) {
            uintptr_t slotAddr = skillListBase + static_cast<size_t>(k) * jx::STRIDE_NPC_SKILL;
            int32_t skillId = 0, baseLv = 0, curLv = 0;
            if (SafeRead(slotAddr + jx::OFF_NPCSKILL_ID, skillId) && skillId > 0) {
                SafeRead(slotAddr + jx::OFF_NPCSKILL_BASE_LEVEL, baseLv);
                SafeRead(slotAddr + jx::OFF_NPCSKILL_CUR_LEVEL, curLv);
                if (!firstSkill) oss << ",";
                firstSkill = false;
                oss << "{";
                oss << "\"slot_index\":" << k << ",";
                oss << "\"skill_id\":" << skillId << ",";
                oss << "\"base_level\":" << baseLv << ",";
                oss << "\"cur_level\":" << curLv;
                oss << "}";
            }
        }
    }
    oss << "],";

    DialogState dlgCopy;
    std::vector<SystemMessageRecord> sysMsgsCopy;
    std::vector<ChatMessageRecord> chatMsgsCopy;
    std::vector<std::string> msgBoxesCopy;
    std::vector<PendingTeamInviteRecord> teamInvitesCopy;
    std::vector<PendingTeamApplyRecord> teamAppliesCopy;
    std::vector<SocialRelationRecord> socialCopy;
    std::vector<std::string> friendInvitesCopy;
    std::vector<std::string> blacklistCopy;
    std::vector<PendingTradeInviteRecord> tradeInvitesCopy;
    TradeWireState tradeWireCopy{};
    CraftingResultRecord lastCraftingCopy{};
    std::string stallAdvHexCopy;
    {
        std::lock_guard<std::mutex> lk(g.uiMutex);
        dlgCopy = g.dialog;
        sysMsgsCopy = g.systemMessages;
        chatMsgsCopy = g.chatMessages;
        msgBoxesCopy = g.messageBoxes;
        teamInvitesCopy = g.pendingTeamInvites;
        teamAppliesCopy = g.pendingTeamApplies;
        socialCopy = g.socialRelations;
        friendInvitesCopy = g.pendingFriendInvitesHex;
        blacklistCopy = g.blacklistNamesHex;
        tradeInvitesCopy = g.pendingTradeInvites;
        tradeWireCopy = g.tradeWire;
        lastCraftingCopy = g.lastCrafting;
        stallAdvHexCopy = g.lastViewedStallAdvHex;
    }

    int32_t teamFlag = 0, teamFigure = 1, gTeamState = 0, gTeamCaptain = -1, gTeamMemNum = 0, gTeamCapLv = 0, gTeamSvrId = -1;
    int32_t selfAuraSkillId = 0;
    char gTeamCapName[32] = {};
    if (playerBase != 0) {
        uintptr_t pTeam = playerBase + jx::STRIDE_PLAYER + jx::OFF_PLAYER_TEAM;
        SafeRead(pTeam + jx::OFF_TEAM_FLAG, teamFlag);
        SafeRead(pTeam + jx::OFF_TEAM_FIGURE, teamFigure);
    }
    if (inWorld) {
        uintptr_t selfNpcAddr = npcBase + static_cast<size_t>(playerNpcIndex) * jx::STRIDE_NPC;
        SafeRead(selfNpcAddr + jx::OFF_NPC_ACTIVE_AURA_SKILL, selfAuraSkillId);
    }
    SafeRead(jx::ADDR_G_TEAM + jx::OFF_GTEAM_STATE, gTeamState);
    SafeRead(jx::ADDR_G_TEAM + jx::OFF_GTEAM_CAPTAIN_ID, gTeamCaptain);
    SafeRead(jx::ADDR_G_TEAM + jx::OFF_GTEAM_MEM_NUM, gTeamMemNum);
    SafeRead(jx::ADDR_G_TEAM + jx::OFF_GTEAM_CAPTAIN_LEVEL, gTeamCapLv);
    SafeRead(jx::ADDR_G_TEAM + jx::OFF_GTEAM_SERVER_ID, gTeamSvrId);
    SafeCopyMem(gTeamCapName, reinterpret_cast<const void*>(jx::ADDR_G_TEAM + jx::OFF_GTEAM_CAPTAIN_NAME), sizeof(gTeamCapName));
    if (gTeamMemNum < 0) gTeamMemNum = 0;
    if (gTeamMemNum > 7) gTeamMemNum = 7;

    bool inTeam = (teamFlag != 0);
    oss << "\"team\":{";
    oss << "\"in_team\":" << (inTeam ? "true" : "false") << ",";
    oss << "\"is_captain\":" << ((inTeam && teamFigure == 0) ? "true" : "false") << ",";
    oss << "\"is_open\":" << ((inTeam && gTeamState != 0) ? "true" : "false") << ",";
    oss << "\"team_server_id\":" << (inTeam ? gTeamSvrId : -1) << ",";
    oss << "\"captain_npc_dw_id\":" << ((inTeam && gTeamCaptain > 0) ? static_cast<uint32_t>(gTeamCaptain) : 0u) << ",";
    oss << "\"captain_level\":" << (inTeam ? gTeamCapLv : 0) << ",";
    oss << "\"captain_name_tcvn3_hex\":" << jxjson::QuoteJson(inTeam ? jxjson::ToHexCStr(gTeamCapName, sizeof(gTeamCapName)) : std::string()) << ",";
    oss << "\"member_count\":" << (inTeam ? (1 + gTeamMemNum) : 0) << ",";
    oss << "\"active_aura_skill_id\":" << selfAuraSkillId << ",";
    oss << "\"members\":[";
    if (inTeam) {
        bool firstMem = true;
        for (int i = 0; i < 7; ++i) {
            int32_t mId = -1;
            int32_t mLv = 0;
            char mName[32] = {};
            SafeRead(jx::ADDR_G_TEAM + jx::OFF_GTEAM_MEMBER_IDS + static_cast<size_t>(i) * 4, mId);
            if (mId <= 0) continue;
            SafeRead(jx::ADDR_G_TEAM + jx::OFF_GTEAM_MEMBER_LEVELS + static_cast<size_t>(i) * 4, mLv);
            SafeCopyMem(mName, reinterpret_cast<const void*>(jx::ADDR_G_TEAM + jx::OFF_GTEAM_MEMBER_NAMES + static_cast<size_t>(i) * 32), sizeof(mName));
            if (!firstMem) oss << ",";
            firstMem = false;
            oss << "{";
            oss << "\"slot\":" << i << ",";
            oss << "\"npc_dw_id\":" << static_cast<uint32_t>(mId) << ",";
            oss << "\"level\":" << mLv << ",";
            oss << "\"name_tcvn3_hex\":" << jxjson::QuoteJson(jxjson::ToHexCStr(mName, sizeof(mName)));
            oss << "}";
        }
    }
    oss << "],";
    oss << "\"pending_invites\":[";
    for (size_t i = 0; i < teamInvitesCopy.size(); ++i) {
        if (i > 0) oss << ",";
        oss << "{";
        oss << "\"seq\":" << teamInvitesCopy[i].seq << ",";
        oss << "\"tick_count\":" << teamInvitesCopy[i].tickCount << ",";
        oss << "\"inviter_player_idx\":" << teamInvitesCopy[i].inviterPlayerIdx << ",";
        oss << "\"inviter_name_tcvn3_hex\":" << jxjson::QuoteJson(teamInvitesCopy[i].inviterNameHex);
        oss << "}";
    }
    oss << "],";
    oss << "\"pending_applies\":[";
    for (size_t i = 0; i < teamAppliesCopy.size(); ++i) {
        if (i > 0) oss << ",";
        oss << "{";
        oss << "\"seq\":" << teamAppliesCopy[i].seq << ",";
        oss << "\"tick_count\":" << teamAppliesCopy[i].tickCount << ",";
        oss << "\"applicant_npc_dw_id\":" << teamAppliesCopy[i].applicantNpcDwId << ",";
        oss << "\"applicant_name_tcvn3_hex\":" << jxjson::QuoteJson(teamAppliesCopy[i].applicantNameHex);
        oss << "}";
    }
    oss << "]},";

    oss << "\"social\":{";
    oss << "\"friends\":[";
    bool firstFriend = true;
    for (size_t i = 0; i < socialCopy.size(); ++i) {
        if (socialCopy[i].isAssociate) continue;
        if (!firstFriend) oss << ",";
        firstFriend = false;
        oss << "{";
        oss << "\"name_tcvn3_hex\":" << jxjson::QuoteJson(socialCopy[i].nameHex) << ",";
        oss << "\"unit_tcvn3_hex\":" << jxjson::QuoteJson(socialCopy[i].unitHex) << ",";
        oss << "\"group_tcvn3_hex\":" << jxjson::QuoteJson(socialCopy[i].groupHex) << ",";
        oss << "\"state\":" << static_cast<unsigned>(socialCopy[i].state);
        oss << "}";
    }
    oss << "],";
    oss << "\"associates\":[";
    bool firstAssoc = true;
    for (size_t i = 0; i < socialCopy.size(); ++i) {
        if (!socialCopy[i].isAssociate) continue;
        if (!firstAssoc) oss << ",";
        firstAssoc = false;
        oss << "{";
        oss << "\"name_tcvn3_hex\":" << jxjson::QuoteJson(socialCopy[i].nameHex) << ",";
        oss << "\"unit_tcvn3_hex\":" << jxjson::QuoteJson(socialCopy[i].unitHex) << ",";
        oss << "\"group_tcvn3_hex\":" << jxjson::QuoteJson(socialCopy[i].groupHex) << ",";
        oss << "\"state\":" << static_cast<unsigned>(socialCopy[i].state);
        oss << "}";
    }
    oss << "],";
    oss << "\"pending_friend_invites_tcvn3_hex\":[";
    for (size_t i = 0; i < friendInvitesCopy.size(); ++i) {
        if (i > 0) oss << ",";
        oss << jxjson::QuoteJson(friendInvitesCopy[i]);
    }
    oss << "],";
    oss << "\"blacklist_tcvn3_hex\":[";
    for (size_t i = 0; i < blacklistCopy.size(); ++i) {
        if (i > 0) oss << ",";
        oss << jxjson::QuoteJson(blacklistCopy[i]);
    }
    oss << "]},";

    {
        int32_t tradeState = 0;
        uint32_t tradeDestNpc = 0;
        int32_t stallState = 0;
        if (inWorld && playerBase != 0) {
            uintptr_t pPlayer1 = playerBase + jx::STRIDE_PLAYER;
            SafeRead(pPlayer1 + jx::OFF_PLAYER_TRADE_STATE, tradeState);
            SafeRead(pPlayer1 + jx::OFF_PLAYER_TRADE_DEST_NPC, tradeDestNpc);
            SafeRead(pPlayer1 + jx::OFF_PLAYER_STALL_STATE, stallState);
        }
        oss << "\"trade\":{";
        oss << "\"state\":" << tradeState << ",";
        oss << "\"partner_npc_dw_id\":" << tradeDestNpc << ",";
        oss << "\"self_locked\":" << (tradeWireCopy.selfLocked != 0 ? "true" : "false") << ",";
        oss << "\"other_locked\":" << (tradeWireCopy.otherLocked != 0 ? "true" : "false") << ",";
        oss << "\"self_confirmed\":" << (tradeWireCopy.selfConfirmed != 0 ? "true" : "false") << ",";
        oss << "\"other_confirmed\":" << (tradeWireCopy.otherConfirmed != 0 ? "true" : "false") << ",";
        oss << "\"other_money\":" << tradeWireCopy.otherMoney << ",";
        oss << "\"last_decision_result\":" << tradeWireCopy.lastDecisionResult << ",";
        oss << "\"decision_seq\":" << tradeWireCopy.decisionSeq << ",";
        oss << "\"pending_invites\":[";
        for (size_t i = 0; i < tradeInvitesCopy.size(); ++i) {
            if (i > 0) oss << ",";
            oss << "{\"seq\":" << tradeInvitesCopy[i].seq
                << ",\"tick_count\":" << tradeInvitesCopy[i].tickCount
                << ",\"inviter_player_idx\":" << tradeInvitesCopy[i].inviterPlayerIdx
                << ",\"inviter_npc_dw_id\":" << tradeInvitesCopy[i].inviterNpcDwId << "}";
        }
        oss << "]},";

        uint32_t viewedSellerDwId = 0;
        int32_t stallTaxRate = 0;
        uintptr_t vecBegin = 0, vecEnd = 0;
        SafeRead(jx::ADDR_STALL_VIEW_SELLER_DWID, viewedSellerDwId);
        SafeRead(jx::ADDR_STALL_TAX_RATE, stallTaxRate);
        SafeRead(jx::ADDR_STALL_VIEW_VEC_BEGIN, vecBegin);
        SafeRead(jx::ADDR_STALL_VIEW_VEC_END, vecEnd);

        oss << "\"stall\":{";
        oss << "\"stall_state\":" << stallState << ",";
        oss << "\"viewed_seller_npc_dw_id\":" << viewedSellerDwId << ",";
        oss << "\"tax_rate\":" << stallTaxRate << ",";
        oss << "\"viewed_adv_tcvn3_hex\":" << jxjson::QuoteJson(stallAdvHexCopy) << ",";
        oss << "\"viewed_items\":[";
        if (vecBegin != 0 && vecEnd > vecBegin && (vecEnd - vecBegin) <= 92u * 64u) {
            bool firstViewed = true;
            for (uintptr_t p = vecBegin; p + 92u <= vecEnd; p += 92u) {
                uint32_t vDwId = 0;
                int32_t vPrice = 0, vGx = 0, vGy = 0;
                if (SafeRead(p + 0x00, vDwId) && vDwId != 0) {
                    SafeRead(p + 0x40, vGx);
                    SafeRead(p + 0x44, vGy);
                    SafeRead(p + 0x50, vPrice);
                    int32_t clientArrIdx = -1;
                    if (itemBase != 0) {
                        for (int ii = 1; ii < jx::MAX_ITEM_COUNT; ++ii) {
                            uint32_t candDw = 0;
                            if (SafeRead(itemBase + static_cast<size_t>(ii) * jx::STRIDE_ITEM + jx::OFF_ITEM_SERVER_ID, candDw) &&
                                candDw == vDwId) {
                                clientArrIdx = ii;
                                break;
                            }
                        }
                    }
                    if (!firstViewed) oss << ",";
                    firstViewed = false;
                    oss << "{\"server_item_id\":" << vDwId
                        << ",\"array_index\":" << clientArrIdx
                        << ",\"price\":" << vPrice
                        << ",\"grid_x\":" << vGx
                        << ",\"grid_y\":" << vGy << "}";
                }
            }
        }
        oss << "]},";

        oss << "\"crafting\":{";
        oss << "\"seq\":" << lastCraftingCopy.seq << ",";
        oss << "\"action_kind\":" << static_cast<unsigned>(lastCraftingCopy.actionKind) << ",";
        oss << "\"item_dw_id\":" << lastCraftingCopy.itemDwId << ",";
        oss << "\"result_code\":" << static_cast<unsigned>(lastCraftingCopy.resultCode);
        oss << "},";
    }

    oss << "\"dialog\":{";
    oss << "\"is_open\":" << (dlgCopy.isOpen ? "true" : "false") << ",";
    oss << "\"is_talk\":" << (dlgCopy.isTalk ? "true" : "false") << ",";
    oss << "\"talk_need_callback\":" << (dlgCopy.talkNeedCallback ? "true" : "false") << ",";
    oss << "\"sequence\":" << dlgCopy.sequence << ",";
    oss << "\"opened_tick\":" << dlgCopy.openedTick << ",";
    oss << "\"prompt_tcvn3_hex\":" << jxjson::QuoteJson(dlgCopy.promptHex) << ",";
    oss << "\"options_tcvn3_hex\":[";
    for (size_t i = 0; i < dlgCopy.optionsHex.size(); ++i) {
        if (i > 0) oss << ",";
        oss << jxjson::QuoteJson(dlgCopy.optionsHex[i]);
    }
    oss << "]},";

    oss << "\"system_messages\":[";
    for (size_t i = 0; i < sysMsgsCopy.size(); ++i) {
        if (i > 0) oss << ",";
        oss << "{";
        oss << "\"seq\":" << sysMsgsCopy[i].seq << ",";
        oss << "\"tick_count\":" << sysMsgsCopy[i].tickCount << ",";
        oss << "\"text_tcvn3_hex\":" << jxjson::QuoteJson(sysMsgsCopy[i].textHex);
        oss << "}";
    }
    oss << "],";

    oss << "\"chat_messages\":[";
    for (size_t i = 0; i < chatMsgsCopy.size(); ++i) {
        if (i > 0) oss << ",";
        oss << "{";
        oss << "\"seq\":" << chatMsgsCopy[i].seq << ",";
        oss << "\"tick_count\":" << chatMsgsCopy[i].tickCount << ",";
        oss << "\"channel_id\":" << chatMsgsCopy[i].channelId << ",";
        oss << "\"sender_tcvn3_hex\":" << jxjson::QuoteJson(chatMsgsCopy[i].senderHex) << ",";
        oss << "\"text_tcvn3_hex\":" << jxjson::QuoteJson(chatMsgsCopy[i].textHex);
        oss << "}";
    }
    oss << "],";

    oss << "\"message_boxes\":[";
    for (size_t i = 0; i < msgBoxesCopy.size(); ++i) {
        if (i > 0) oss << ",";
        oss << jxjson::QuoteJson(msgBoxesCopy[i]);
    }
    oss << "],";

    uint32_t headSeq = g.s2cSeq.load(std::memory_order_acquire);
    uint32_t count = headSeq < kS2cRingSize ? headSeq : static_cast<uint32_t>(kS2cRingSize);
    uint32_t startSeq = headSeq - count;
    oss << "\"recent_s2c\":[";
    bool firstPkt = true;
    for (uint32_t s = startSeq + 1; s <= headSeq; ++s) {
        const S2cPacketRecord& rec = g.s2cRing[(s - 1) % kS2cRingSize];
        if (rec.seq != s) continue;
        if (!firstPkt) oss << ",";
        firstPkt = false;
        oss << "{";
        oss << "\"seq\":" << rec.seq << ",";
        oss << "\"opcode\":" << static_cast<unsigned>(rec.opcode) << ",";
        oss << "\"length\":" << rec.length << ",";
        oss << "\"tick_count\":" << rec.tickCount << ",";
        oss << "\"timestamp_ms\":" << rec.timestampMs << ",";
        oss << "\"hex_preview\":" << jxjson::QuoteJson(jxjson::ToHex(rec.bytes, rec.capturedLen));
        oss << "}";
    }
    oss << "],";

    uint32_t headC2sSeq = g.c2sSeq.load(std::memory_order_acquire);
    uint32_t countC2s = headC2sSeq < kC2sRingSize ? headC2sSeq : static_cast<uint32_t>(kC2sRingSize);
    uint32_t startC2sSeq = headC2sSeq - countC2s;
    oss << "\"recent_c2s\":[";
    bool firstC2sPkt = true;
    for (uint32_t s = startC2sSeq + 1; s <= headC2sSeq; ++s) {
        const C2sPacketRecord& rec = g.c2sRing[(s - 1) % kC2sRingSize];
        if (rec.seq != s) continue;
        if (!firstC2sPkt) oss << ",";
        firstC2sPkt = false;
        oss << "{";
        oss << "\"seq\":" << rec.seq << ",";
        oss << "\"opcode\":" << static_cast<unsigned>(rec.opcode) << ",";
        oss << "\"length\":" << rec.length << ",";
        oss << "\"tick_count\":" << rec.tickCount << ",";
        oss << "\"timestamp_ms\":" << rec.timestampMs << ",";
        oss << "\"hex_preview\":" << jxjson::QuoteJson(jxjson::ToHex(rec.bytes, rec.capturedLen));
        oss << "}";
    }
    oss << "]";

    oss << "}";
    return oss.str();
}

int32_t ResolveLocalPlayerNpcIndex(uintptr_t& outNpcAddr) {
    outNpcAddr = 0;
    uintptr_t playerBase = 0;
    uintptr_t npcBase = 0;
    if (!SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase) || playerBase == 0) return -1;
    if (!SafeRead(jx::ADDR_NPC_ARRAY_PTR, npcBase) || npcBase == 0) return -1;

    int32_t npcIdx = 0;
    if (!SafeRead(playerBase + jx::STRIDE_PLAYER + jx::OFF_PLAYER_NPC_INDEX, npcIdx)) return -1;
    if (npcIdx <= 0 || npcIdx >= jx::MAX_NPC_COUNT) return -1;

    uintptr_t npcAddr = npcBase + static_cast<size_t>(npcIdx) * jx::STRIDE_NPC;
    int32_t activeIdx = 0;
    if (!SafeRead(npcAddr + jx::OFF_NPC_INDEX, activeIdx) || activeIdx <= 0) return -1;

    outNpcAddr = npcAddr;
    return npcIdx;
}

__declspec(noinline) bool SafeCallSetFocusPosition(void* pScenePlace, int32_t mpsX, int32_t mpsY) {
    if (!pScenePlace || mpsX <= 0 || mpsY <= 0) return false;
    __try {
        auto fn = reinterpret_cast<PfnSceneSetFocusPosition>(jx::FN_SCENE_SET_FOCUS_POSITION);
        fn(pScenePlace, mpsX, mpsY, 0);
        return true;
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        return false;
    }
}

std::string RpcError(const std::string& msg) {
    return "!ERR:" + msg;
}

} // namespace

void SyncSceneFocusToLocalPlayer() {
    uintptr_t selfNpcAddr = 0;
    if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return;

    uintptr_t subWorldBase = 0;
    if (!SafeRead(jx::ADDR_SUBWORLD_ARRAY_PTR, subWorldBase) || subWorldBase == 0) return;

    int32_t mapId = 0, subIdx = -1, regIdx = -1, mapX = 0, mapY = 0, mpsX = 0, mpsY = 0;
    if (TryResolveNpcWorldCoords(
            reinterpret_cast<const uint8_t*>(selfNpcAddr), subWorldBase,
            mapId, subIdx, regIdx, mapX, mapY, mpsX, mpsY) &&
        mpsX > 0 && mpsY > 0) {
        SafeCallSetFocusPosition(reinterpret_cast<void*>(jx::ADDR_SCENE_PLACE), mpsX, mpsY);
    }
}

std::string HandleRpcMethod(const std::string& method, const std::string& paramsRawJson) {
    EnsureDynamicHooksInstalled();
    jxjson::JsonValue params;
    params.type = jxjson::JsonValue::Object;
    if (!paramsRawJson.empty()) {
        jxjson::Parser parser(paramsRawJson);
        if (!parser.Parse(params) || params.type != jxjson::JsonValue::Object) {
            return RpcError("Invalid params JSON object");
        }
    }

    if (method == "get_state") {
        return BuildGetStateJson();
    }

    if (method == "connect_gateway") {
        std::string ip = params.GetString("ip", "127.0.0.1");
        int64_t portVal = params.GetInt("port", 5622);
        uint32_t ipv4NetOrder = 0;
        if (!ParseIpv4NetOrder(ip, ipv4NetOrder)) {
            return RpcError("Invalid IPv4 address: " + ip);
        }
        if (portVal <= 0 || portVal > 65535) {
            return RpcError("Invalid port");
        }

        void* pAgent = reinterpret_cast<void*>(jx::ADDR_NET_CONNECT_AGENT);
        auto fnDiscGs = reinterpret_cast<PfnNetDisconnect>(jx::FN_NET_DISCONNECT_GAMESVR);
        auto fnDiscGw = reinterpret_cast<PfnNetDisconnect>(jx::FN_NET_DISCONNECT_GATEWAY);
        fnDiscGs(pAgent);
        fnDiscGw(pAgent);

        GetGlobals().worldSyncReady.store(false, std::memory_order_release);
        {
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            g.pendingTeamInvites.clear();
            g.pendingTeamApplies.clear();
            g.socialRelations.clear();
            g.pendingFriendInvitesHex.clear();
            g.blacklistNamesHex.clear();
            g.lastTongApplicantPlayerIdx = -1;
            g.lastTongApplicantNameHex.clear();
        }
        *reinterpret_cast<uint32_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_SERVER_IP) = ipv4NetOrder;

        auto fnConnGw = reinterpret_cast<PfnNetConnectGateway>(jx::FN_NET_CONNECT_GATEWAY);
        int ok = fnConnGw(pAgent, ipv4NetOrder, static_cast<int16_t>(portVal));
        if (ok == 0) {
            return RpcError("KNetConnectAgent::ConnectToGateway returned 0");
        }

        auto fnRegTargets = reinterpret_cast<PfnKLoginRegisterTargets>(jx::FN_KLOGIN_REGISTER_TARGETS);
        void* pKLogin = reinterpret_cast<void*>(jx::ADDR_KLOGIN);
        fnRegTargets(pKLogin);
        *reinterpret_cast<int32_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_STATUS) = 1;
        *reinterpret_cast<int32_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_RESULT) = 0;
        *reinterpret_cast<uint8_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_AUTO_LOGIN) = 0;

        std::ostringstream oss;
        oss << "{\"connected\":true,\"ip\":" << jxjson::QuoteJson(ip) << ",\"port\":" << portVal << ",\"login_status\":1}";
        return oss.str();
    }

    if (method == "account_login") {
        std::string account = params.GetString("account");
        std::string password = params.GetString("password");
        if (account.empty() || account.size() >= 32) {
            return RpcError("Invalid account length");
        }

        int32_t loginStatus = *reinterpret_cast<const int32_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_STATUS);
        if (loginStatus != 1) {
            return RpcError("KLogin status must be 1 (GatewayConnected) before account_login");
        }

        HMODULE hEngine = GetModuleHandleA("engine.dll");
        if (!hEngine) {
            return RpcError("engine.dll is not loaded");
        }
        auto fnMd5 = reinterpret_cast<PfnStringToMd5>(GetProcAddress(hEngine, "?KSG_StringToMD5String@@YAHQADQBD@Z"));
        if (!fnMd5) {
            return RpcError("KSG_StringToMD5String export not found in engine.dll");
        }

        char md5Buf[68] = {};
        fnMd5(md5Buf, password.c_str());

        auto fnAcctLogin = reinterpret_cast<PfnKLoginAccountLogin>(jx::FN_KLOGIN_ACCOUNT_LOGIN);
        int sent = fnAcctLogin(reinterpret_cast<void*>(jx::ADDR_KLOGIN), account.c_str(), md5Buf, 1);
        std::memset(md5Buf, 0, sizeof(md5Buf));

        int32_t newStatus = *reinterpret_cast<const int32_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_STATUS);
        int32_t newResult = *reinterpret_cast<const int32_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_RESULT);
        std::ostringstream oss;
        oss << "{\"sent\":" << (sent ? "true" : "false") << ",\"login_status\":" << newStatus << ",\"login_result\":" << newResult << "}";
        return oss.str();
    }

    if (method == "create_role") {
        std::string nameHex = params.GetString("name_tcvn3_hex");
        std::vector<uint8_t> nameBytes = jxjson::FromHex(nameHex);
        if (nameBytes.empty() || nameBytes.size() >= 32) {
            return RpcError("create_role requires 1..31 TCVN3 bytes in name_tcvn3_hex");
        }
        int64_t gender = params.GetInt("gender", 0);
        int64_t series = params.GetInt("series", 0);
        int64_t nativePlaceId = params.GetInt("native_place_id", 53);
        if (series < 0 || series >= 5) {
            return RpcError("create_role series must be in 0..4");
        }

        jx::RoleInfo36B role{};
        std::memcpy(role.Name, nameBytes.data(), nameBytes.size());
        role.Gender = static_cast<uint8_t>(gender);
        role.Series = static_cast<uint8_t>(series);
        role.NativePlaceId = static_cast<uint16_t>(nativePlaceId);

        auto fnCreateRole = reinterpret_cast<PfnKLoginCreateRole>(jx::FN_KLOGIN_CREATE_ROLE);
        fnCreateRole(reinterpret_cast<void*>(jx::ADDR_KLOGIN), &role);

        int32_t newStatus = *reinterpret_cast<const int32_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_STATUS);
        int32_t newResult = *reinterpret_cast<const int32_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_RESULT);
        if (newStatus != 7 && newResult == 1) {
            return RpcError("KLogin::CreateRole rejected request (check login_status == 6 and num_roles < 3)");
        }

        std::ostringstream oss;
        oss << "{\"sent\":true,\"login_status\":" << newStatus << ",\"login_result\":" << newResult << "}";
        return oss.str();
    }

    if (method == "select_role") {
        int32_t index = static_cast<int32_t>(params.GetInt("index", 0));
        auto fnSelectRole = reinterpret_cast<PfnKLoginSelectRole>(jx::FN_KLOGIN_SELECT_ROLE);
        int ok = fnSelectRole(reinterpret_cast<void*>(jx::ADDR_KLOGIN), index);

        int32_t newStatus = *reinterpret_cast<const int32_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_STATUS);
        int32_t newResult = *reinterpret_cast<const int32_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_RESULT);
        std::ostringstream oss;
        oss << "{\"sent\":" << (ok ? "true" : "false") << ",\"login_status\":" << newStatus << ",\"login_result\":" << newResult << "}";
        return oss.str();
    }

    if (method == "move_to") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        int32_t mpsX = static_cast<int32_t>(params.GetInt("mps_x", 0));
        int32_t mpsY = static_cast<int32_t>(params.GetInt("mps_y", 0));
        int32_t mode = static_cast<int32_t>(params.GetInt("mode", 1));

        int32_t doingBefore = 0;
        int32_t processAiBefore = 1;
        SafeRead(selfNpcAddr + jx::OFF_NPC_DOING, doingBefore);
        SafeRead(selfNpcAddr + jx::OFF_NPC_PROCESS_AI, processAiBefore);

        // If m_ProcessAI (+0x1728) was orphaned at 0 (e.g. s2c_syncnpcminplayer 0x4E overwrote do_hurt before OnHurt called EnableProcessAI),
        // restore m_ProcessAI when m_Doing is already in normal stand/walk/run (1..3) so KNpc::ProcCommand processes the queued move command.
        bool repairedOrphanedProcessAi = false;
        if (processAiBefore == 0 && (doingBefore == 1 || doingBefore == 2 || doingBefore == 3)) {
            auto fnEnableAi = reinterpret_cast<PfnNpcEnableProcessAi>(jx::FN_KNPC_ENABLE_PROCESS_AI);
            fnEnableAi(reinterpret_cast<void*>(selfNpcAddr));
            repairedOrphanedProcessAi = true;
        }

        auto fnMove = reinterpret_cast<PfnNpcSendCmdMove>(jx::FN_KNPC_SEND_CMD_MOVE);
        fnMove(reinterpret_cast<void*>(selfNpcAddr), mpsX, mpsY, mode);

        std::ostringstream oss;
        oss << "{\"sent\":true,\"mps_x\":" << mpsX << ",\"mps_y\":" << mpsY << ",\"mode\":" << mode
            << ",\"doing_before\":" << doingBefore << ",\"process_ai_before\":" << processAiBefore
            << ",\"repaired_orphaned_process_ai\":" << (repairedOrphanedProcessAi ? "true" : "false") << "}";
        return oss.str();
    }

    if (method == "interact_npc") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }

        int32_t arrayIdx = static_cast<int32_t>(params.GetInt("array_index", -1));
        if (arrayIdx <= 0) {
            uint32_t targetDwId = static_cast<uint32_t>(params.GetInt("dw_id", 0));
            if (targetDwId == 0) {
                return RpcError("interact_npc requires array_index or dw_id");
            }
            uintptr_t npcBase = 0;
            SafeRead(jx::ADDR_NPC_ARRAY_PTR, npcBase);
            if (npcBase == 0) return RpcError("Npc array not initialized");
            for (int i = 1; i < jx::MAX_NPC_COUNT; ++i) {
                uintptr_t npcAddr = npcBase + static_cast<size_t>(i) * jx::STRIDE_NPC;
                int32_t idxVal = 0;
                uint32_t dwId = 0;
                if (SafeRead(npcAddr + jx::OFF_NPC_INDEX, idxVal) && idxVal > 0 &&
                    SafeRead(npcAddr + jx::OFF_NPC_DW_ID, dwId) && dwId == targetDwId) {
                    arrayIdx = i;
                    break;
                }
            }
        }
        if (arrayIdx <= 0 || arrayIdx >= jx::MAX_NPC_COUNT) {
            return RpcError("Target NPC not found in active client NPC array");
        }

        auto fnDialog = reinterpret_cast<PfnSendNpcDialog>(jx::FN_SEND_NPC_DIALOG);
        fnDialog(arrayIdx);

        std::ostringstream oss;
        oss << "{\"sent\":true,\"array_index\":" << arrayIdx << "}";
        return oss.str();
    }

    if (method == "select_dialog") {
        int32_t optionIndex = static_cast<int32_t>(params.GetInt("option_index", 0));
        bool wasTalk = false;
        bool needTalkCb = false;
        {
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            wasTalk = g.dialog.isTalk;
            needTalkCb = g.dialog.talkNeedCallback;
            g.dialog.isOpen = false;
            g.dialog.isTalk = false;
            g.dialog.talkNeedCallback = false;
        }

        bool usedUi = false;
        if (wasTalk) {
            if (needTalkCb || (*reinterpret_cast<const int32_t*>(jx::ADDR_UI_TALK_CALLBACK_FLAG) != 0)) {
                auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
                fnCoreOp(8, 0, 0);
                *reinterpret_cast<int32_t*>(jx::ADDR_UI_TALK_CALLBACK_FLAG) = 0;
            }
        } else {
            void* pUiMsgSel = *reinterpret_cast<void**>(jx::ADDR_UI_MSG_SEL_PTR);
            if (pUiMsgSel != nullptr) {
                usedUi = true;
                auto fnUiClick = reinterpret_cast<PfnUiMsgSelClick>(jx::FN_UI_MSG_SEL_CLICK);
                fnUiClick(pUiMsgSel, optionIndex);
            } else {
                auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
                fnCoreOp(9, 0, optionIndex);
            }
        }

        std::ostringstream oss;
        oss << "{\"sent\":true,\"option_index\":" << optionIndex << ",\"used_ui_msgsel\":" << (usedUi ? "true" : "false") << "}";
        return oss.str();
    }

    if (method == "confirm_talk") {
        bool needTalkCb = false;
        {
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            needTalkCb = g.dialog.talkNeedCallback || (*reinterpret_cast<const int32_t*>(jx::ADDR_UI_TALK_CALLBACK_FLAG) != 0);
            g.dialog.isOpen = false;
            g.dialog.isTalk = false;
            g.dialog.talkNeedCallback = false;
        }

        bool sentCallback = false;
        bool forceCb = params.GetInt("force_callback", 0) != 0;
        if (needTalkCb || forceCb) {
            if (forceCb && (*reinterpret_cast<const int32_t*>(jx::ADDR_UI_TALK_CALLBACK_FLAG) == 0)) {
                *reinterpret_cast<int32_t*>(jx::ADDR_UI_TALK_CALLBACK_FLAG) = 1;
            }
            auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
            fnCoreOp(8, 0, 0);
            *reinterpret_cast<int32_t*>(jx::ADDR_UI_TALK_CALLBACK_FLAG) = 0;
            sentCallback = true;
        }

        std::ostringstream oss;
        oss << "{\"sent\":true,\"sent_callback\":" << (sentCallback ? "true" : "false") << "}";
        return oss.str();
    }

    if (method == "use_skill") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }

        int32_t skillId = static_cast<int32_t>(params.GetInt("skill_id", 0));
        auto fnSkill = reinterpret_cast<PfnSendNpcSkill>(jx::FN_SEND_NPC_SKILL);

        const jxjson::JsonValue* targetNpcField = params.Get("target_npc_dw_id");
        if (targetNpcField && targetNpcField->type == jxjson::JsonValue::Int && targetNpcField->intVal != 0) {
            int32_t targetDwId = static_cast<int32_t>(targetNpcField->intVal);
            fnSkill(skillId, -1, targetDwId);
            std::ostringstream oss;
            oss << "{\"sent\":true,\"skill_id\":" << skillId << ",\"target_npc_dw_id\":" << targetDwId << "}";
            return oss.str();
        }

        int32_t mpsX = static_cast<int32_t>(params.GetInt("mps_x", 0));
        int32_t mpsY = static_cast<int32_t>(params.GetInt("mps_y", 0));
        fnSkill(skillId, mpsX, mpsY);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"skill_id\":" << skillId << ",\"mps_x\":" << mpsX << ",\"mps_y\":" << mpsY << "}";
        return oss.str();
    }

    if (method == "switch_pk") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");

        uint8_t pkMode = static_cast<uint8_t>(params.GetInt("pk_mode", 0));
        void* pPlayerPk = reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER + jx::OFF_PLAYER_PK);
        auto fnSwitchPk = reinterpret_cast<PfnSendSwitchPk>(jx::FN_SEND_SWITCH_PK);
        fnSwitchPk(pPlayerPk, pkMode);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"pk_mode\":" << static_cast<unsigned>(pkMode) << "}";
        return oss.str();
    }

    if (method == "pk_target") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");

        uint32_t targetDwId = static_cast<uint32_t>(params.GetInt("target_npc_dw_id", 0));
        if (targetDwId == 0) {
            return RpcError("pk_target requires target_npc_dw_id");
        }
        void* pPlayerPk = reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER + jx::OFF_PLAYER_PK);
        auto fnPkTarget = reinterpret_cast<PfnSendPkTarget>(jx::FN_SEND_PK_TARGET);
        fnPkTarget(pPlayerPk, targetDwId);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"target_npc_dw_id\":" << targetDwId << "}";
        return oss.str();
    }

    if (method == "pickup_obj") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        uintptr_t objBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        SafeRead(jx::ADDR_OBJ_ARRAY_PTR, objBase);
        if (playerBase == 0 || objBase == 0) {
            return RpcError("Player or Object array not initialized");
        }

        int32_t objArrayIdx = static_cast<int32_t>(params.GetInt("array_index", -1));
        if (objArrayIdx <= 0) {
            int32_t targetObjId = static_cast<int32_t>(params.GetInt("object_id", 0));
            if (targetObjId <= 0) {
                return RpcError("pickup_obj requires array_index or object_id");
            }
            void* pObjUseIdx = reinterpret_cast<void*>(jx::ADDR_OBJ_SET + jx::OFF_OBJSET_USE_IDX);
            int32_t cur = SafeLinkArrayGetNext(pObjUseIdx, 0);
            for (int guard = 0; cur > 0 && guard < jx::MAX_OBJ_SCAN_GUARD; ++guard) {
                uintptr_t objAddr = objBase + static_cast<size_t>(cur) * jx::STRIDE_OBJ;
                int32_t activeIdx = 0, idVal = 0;
                if (SafeRead(objAddr + jx::OFF_OBJ_INDEX, activeIdx) && activeIdx > 0 &&
                    SafeRead(objAddr + jx::OFF_OBJ_ID, idVal) && idVal == targetObjId) {
                    objArrayIdx = cur;
                    break;
                }
                cur = SafeLinkArrayGetNext(pObjUseIdx, cur);
            }
        }
        if (objArrayIdx <= 0) {
            return RpcError("Target ground object not found in active client Object array");
        }

        uintptr_t objAddr = objBase + static_cast<size_t>(objArrayIdx) * jx::STRIDE_OBJ;
        int32_t kind = 0;
        SafeRead(objAddr + jx::OFF_OBJ_KIND, kind);

        int32_t idVal = 0, regIdx = -1, swIdx = 0;
        SafeRead(objAddr + jx::OFF_OBJ_ID, idVal);
        SafeRead(objAddr + jx::OFF_OBJ_REGION_IDX, regIdx);
        SafeRead(objAddr + jx::OFF_OBJ_SUBWORLD_ID, swIdx);
        uint32_t regionId = 0;
        uintptr_t subWorldBase = 0;
        SafeRead(jx::ADDR_SUBWORLD_ARRAY_PTR, subWorldBase);
        if (subWorldBase != 0 && swIdx >= 0 && swIdx < 8) {
            uintptr_t swAddr = subWorldBase + static_cast<size_t>(swIdx) * jx::STRIDE_SUBWORLD;
            uintptr_t pRegions = 0;
            if (SafeRead(swAddr + jx::OFF_SUBWORLD_REGION_PTR, pRegions) && pRegions != 0 && regIdx >= 0) {
                uintptr_t regAddr = pRegions + static_cast<size_t>(regIdx) * jx::STRIDE_KREGION;
                SafeRead(regAddr + 4, regionId);
            }
        }

        // Always invoke SendObjMouseClick directly to ensure c2s_objmouseclick (0x72) is dispatched to server
        using PfnSendObjMouseClick = void(__cdecl*)(int32_t nObjId, uint32_t dwRegionId);
        auto fnSendObjClick = reinterpret_cast<PfnSendObjMouseClick>(0x0058d310);
        fnSendObjClick(idVal, regionId);

        void* pPlayer1 = reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER);
        if (kind == 2 || kind == 10) {
            auto fnClickObj = reinterpret_cast<PfnPlayerClickObj>(jx::FN_PLAYER_CLICK_OBJ);
            fnClickObj(pPlayer1, objArrayIdx);
        } else {
            auto fnPickup = reinterpret_cast<PfnPlayerPickUpObj>(jx::FN_PLAYER_PICKUP_OBJ);
            fnPickup(pPlayer1, objArrayIdx);
        }

        std::ostringstream oss;
        oss << "{\"sent\":true,\"array_index\":" << objArrayIdx << ",\"kind\":" << kind << "}";
        return oss.str();
    }

    if (method == "move_item") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        jx::ItemPos12B downPos{
            static_cast<int32_t>(params.GetInt("src_place", 0)),
            static_cast<int32_t>(params.GetInt("src_x", 0)),
            static_cast<int32_t>(params.GetInt("src_y", 0))
        };
        jx::ItemPos12B upPos{
            static_cast<int32_t>(params.GetInt("dst_place", 0)),
            static_cast<int32_t>(params.GetInt("dst_x", 0)),
            static_cast<int32_t>(params.GetInt("dst_y", 0))
        };

        auto fnMoveItem = reinterpret_cast<PfnSendMoveItem>(jx::FN_SEND_MOVE_ITEM);
        fnMoveItem(&downPos, &upPos);

        std::ostringstream oss;
        oss << "{\"sent\":true,\"src_place\":" << downPos.Place << ",\"src_x\":" << downPos.X << ",\"src_y\":" << downPos.Y
            << ",\"dst_place\":" << upPos.Place << ",\"dst_x\":" << upPos.X << ",\"dst_y\":" << upPos.Y << "}";
        return oss.str();
    }

    if (method == "use_item") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");

        int32_t itemArrayIdx = static_cast<int32_t>(params.GetInt("array_index", 0));
        int32_t place = static_cast<int32_t>(params.GetInt("place", 3));
        int32_t gridX = static_cast<int32_t>(params.GetInt("grid_x", 0));
        int32_t gridY = static_cast<int32_t>(params.GetInt("grid_y", 0));
        if (itemArrayIdx <= 0 || itemArrayIdx >= jx::MAX_ITEM_COUNT) {
            return RpcError("use_item requires valid item array_index (1..511)");
        }

        auto fnUse = reinterpret_cast<PfnPlayerApplyUseItem>(jx::FN_PLAYER_APPLY_USE_ITEM);
        int32_t ret = fnUse(reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER), itemArrayIdx, place, gridX, gridY);

        std::ostringstream oss;
        oss << "{\"sent\":true,\"return_value\":" << ret << ",\"array_index\":" << itemArrayIdx
            << ",\"place\":" << place << ",\"grid_x\":" << gridX << ",\"grid_y\":" << gridY << "}";
        return oss.str();
    }

    if (method == "throw_item") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");

        auto fnThrow = reinterpret_cast<PfnPlayerThrowAwayItem>(jx::FN_PLAYER_THROW_AWAY_ITEM);
        int32_t ret = fnThrow(reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER));

        std::ostringstream oss;
        oss << "{\"sent\":" << (ret != 0 ? "true" : "false") << ",\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "add_base_attr") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        int32_t attr = static_cast<int32_t>(params.GetInt("attribute", 0));
        int32_t points = static_cast<int32_t>(params.GetInt("points", 1));
        auto fnAddAttr = reinterpret_cast<PfnSendAddBaseAttr>(jx::FN_SEND_ADD_BASE_ATTR);
        fnAddAttr(attr, points);

        std::ostringstream oss;
        oss << "{\"sent\":true,\"attribute\":" << attr << ",\"points\":" << points << "}";
        return oss.str();
    }

    if (method == "add_skill_point") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");

        int32_t skillId = static_cast<int32_t>(params.GetInt("skill_id", 0));
        int32_t points = static_cast<int32_t>(params.GetInt("points", 1));
        auto fnAddSkill = reinterpret_cast<PfnPlayerAddSkillPoint>(jx::FN_PLAYER_ADD_SKILL_POINT);
        int32_t ret = fnAddSkill(reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER), skillId, points);

        std::ostringstream oss;
        oss << "{\"sent\":true,\"return_value\":" << ret << ",\"skill_id\":" << skillId << ",\"points\":" << points << "}";
        return oss.str();
    }

    if (method == "send_chat") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");

        std::string msgHex = params.GetString("message_tcvn3_hex");
        std::vector<uint8_t> msgBytes = jxjson::FromHex(msgHex);
        if (msgBytes.empty() || msgBytes.size() >= 255) {
            return RpcError("send_chat requires 1..254 TCVN3 bytes in message_tcvn3_hex");
        }

        int32_t channel = static_cast<int32_t>(params.GetInt("channel", 2));
        uint32_t targetDwId = static_cast<uint32_t>(params.GetInt("target_dw_id", 0));
        int32_t targetIdx = static_cast<int32_t>(params.GetInt("target_npc_idx", 0));

        uintptr_t pPlayer1 = playerBase + jx::STRIDE_PLAYER;
        uintptr_t pChat = pPlayer1 + jx::OFF_PLAYER_CHAT;

        char sentenceBuf[260] = {};
        std::memcpy(sentenceBuf, msgBytes.data(), msgBytes.size());
        sentenceBuf[msgBytes.size()] = '\0';

        // KUiMsgParam: +0 uType=0, +1 nPrefixLen=0, +2 uint16_t nMsgLen, +4 char szSender[32], +0x24 char cChatPrefix[16]
        uint8_t uiChatMsg[0x128] = {};
        uiChatMsg[0] = 0;
        uiChatMsg[1] = 0;
        uint16_t contentLen = static_cast<uint16_t>(msgBytes.size());
        std::memcpy(uiChatMsg + 2, &contentLen, sizeof(contentLen));
        SafeCopyMem(uiChatMsg + 4, reinterpret_cast<const void*>(selfNpcAddr + jx::OFF_NPC_NAME), 32);
        uiChatMsg[4 + 31] = '\0';

        if (channel == 3 && targetDwId != 0) {
            auto fnSomeone = reinterpret_cast<PfnChatSendSomeone>(jx::FN_CHAT_SEND_SOMEONE);
            fnSomeone(reinterpret_cast<void*>(pChat), targetDwId, targetIdx, uiChatMsg, sentenceBuf);
        } else if (channel == 6) {
            // Tong chat (\O<TongNameID>) uses KUiMsgCentrePad::FindActivateChannelType("Tong#") @ 0x00490290
            // + GetChannelID(activateIdx) @ 0x00490680 -> Relay PLAYERCOMM_CHANNELCHAT (0xFB 0x21) via 0x0044d3f0
            uint32_t tongChId = 0xFFFFFFFFu;
            uint8_t tongCost = 3;
            uint8_t tongSomeFlag = 6;
            void* pPad = *reinterpret_cast<void**>(jx::ADDR_UI_MSG_CENTRE_PAD_PTR);
            if (pPad != nullptr) {
                int32_t actIdx = reinterpret_cast<PfnUiFindActivateChannel>(0x00490290)(pPad, "Tong#");
                if (actIdx >= 0) {
                    tongChId = reinterpret_cast<PfnUiGetActivateChannelId>(0x00490680)(actIdx);
                }
            }
            {
                auto& g = GetGlobals();
                std::lock_guard<std::mutex> lk(g.uiMutex);
                if (tongChId == 0xFFFFFFFFu) {
                    tongChId = g.tongChannelId;
                }
                tongCost = g.tongChannelCost;
                tongSomeFlag = g.tongChannelSomeFlag;
            }
            if (tongChId != 0xFFFFFFFFu) {
                void* pBar = *reinterpret_cast<void**>(jx::ADDR_UI_PLAYER_BAR_PTR);
                reinterpret_cast<PfnUiSendChannelMsgToCore>(0x0044d3f0)(
                    pBar,
                    tongChId,
                    static_cast<uint32_t>(tongCost),
                    static_cast<uint32_t>(tongSomeFlag),
                    sentenceBuf,
                    static_cast<int32_t>(contentLen));
            } else {
                // Trigger Tong# subscription check if UI hasn't queried yet, and fall back to Core 0x56 channel=6
                if (pPad != nullptr) {
                    reinterpret_cast<PfnUiCheckChannel>(jx::FN_UI_CHECK_CHANNEL)(2);
                    reinterpret_cast<PfnUiQueryAllChannel>(jx::FN_UI_QUERY_ALL_CHANNEL)();
                }
                *reinterpret_cast<int32_t*>(pChat + jx::OFF_CHAT_CUR_CHANNEL) = channel;
                *reinterpret_cast<uint32_t*>(pChat + jx::OFF_CHAT_TARGET_ID) = targetDwId;
                *reinterpret_cast<int32_t*>(pChat + jx::OFF_CHAT_TARGET_IDX) = targetIdx;
                auto fnChat = reinterpret_cast<PfnChatSendChat>(jx::FN_CHAT_SEND_CHAT);
                fnChat(reinterpret_cast<void*>(pChat), uiChatMsg, sentenceBuf);
            }
        } else {
            *reinterpret_cast<int32_t*>(pChat + jx::OFF_CHAT_CUR_CHANNEL) = channel;
            *reinterpret_cast<uint32_t*>(pChat + jx::OFF_CHAT_TARGET_ID) = targetDwId;
            *reinterpret_cast<int32_t*>(pChat + jx::OFF_CHAT_TARGET_IDX) = targetIdx;
            auto fnChat = reinterpret_cast<PfnChatSendChat>(jx::FN_CHAT_SEND_CHAT);
            fnChat(reinterpret_cast<void*>(pChat), uiChatMsg, sentenceBuf);
        }

        std::ostringstream oss;
        oss << "{\"sent\":true,\"channel\":" << channel << ",\"target_dw_id\":" << targetDwId << ",\"len\":" << contentLen << "}";
        return oss.str();
    }

    if (method == "set_chat_channel") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");

        uint32_t mask = static_cast<uint32_t>(params.GetInt("channel_mask", 0xFFFFFFFFu));
        uintptr_t pChat = playerBase + jx::STRIDE_PLAYER + jx::OFF_PLAYER_CHAT;
        *reinterpret_cast<uint32_t*>(pChat + jx::OFF_CHAT_CHANNEL_MASK) = mask;

        void* pGsClient = *reinterpret_cast<void**>(jx::ADDR_GAMESVR_ICLIENT);
        bool sent = false;
        if (pGsClient != nullptr) {
            uint8_t pkt[5] = {};
            pkt[0] = 0x60; // c2s_chatsetchannel
            std::memcpy(pkt + 1, &mask, sizeof(mask));
            void** vtable = *reinterpret_cast<void***>(pGsClient);
            if (vtable && vtable[4]) {
                auto fnSend = reinterpret_cast<PfnIClientSendPack>(vtable[4]);
                fnSend(pGsClient, pkt, sizeof(pkt));
                sent = true;
            }
        }

        std::ostringstream oss;
        oss << "{\"sent\":" << (sent ? "true" : "false") << ",\"channel_mask\":" << mask << "}";
        return oss.str();
    }

    if (method == "buy_shop_item") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uint32_t buyIdx = static_cast<uint32_t>(params.GetInt("buy_index", 0));
        uint8_t place = static_cast<uint8_t>(params.GetInt("place", 3));
        uint8_t gridX = static_cast<uint8_t>(params.GetInt("grid_x", 0));
        uint8_t gridY = static_cast<uint8_t>(params.GetInt("grid_y", 0));
        auto fnBuy = reinterpret_cast<PfnSendBuyItem>(jx::FN_SEND_BUY_ITEM);
        fnBuy(buyIdx, place, gridX, gridY);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"buy_index\":" << buyIdx << ",\"place\":" << static_cast<unsigned>(place)
            << ",\"grid_x\":" << static_cast<unsigned>(gridX) << ",\"grid_y\":" << static_cast<unsigned>(gridY) << "}";
        return oss.str();
    }

    if (method == "sell_shop_item") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uint32_t serverItemId = static_cast<uint32_t>(params.GetInt("server_item_id", 0));
        if (serverItemId == 0) {
            return RpcError("server_item_id must be > 0");
        }
        auto fnSell = reinterpret_cast<PfnSendSellItem>(jx::FN_SEND_SELL_ITEM);
        fnSell(serverItemId);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"server_item_id\":" << serverItemId << "}";
        return oss.str();
    }

    if (method == "repair_item") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uint32_t serverItemId = static_cast<uint32_t>(params.GetInt("server_item_id", 0));
        if (serverItemId == 0) {
            return RpcError("server_item_id must be > 0");
        }
        auto fnRepair = reinterpret_cast<PfnSendRepairItem>(jx::FN_SEND_REPAIR_ITEM);
        fnRepair(serverItemId);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"server_item_id\":" << serverItemId << "}";
        return oss.str();
    }

    if (method == "store_money") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uint8_t direction = static_cast<uint8_t>(params.GetInt("direction", 0));
        int32_t amount = static_cast<int32_t>(params.GetInt("amount", 0));
        auto fnStore = reinterpret_cast<PfnSendStoreMoney>(jx::FN_SEND_STORE_MONEY);
        fnStore(direction, amount);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"direction\":" << static_cast<unsigned>(direction) << ",\"amount\":" << amount << "}";
        return oss.str();
    }

    if (method == "box_operate") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");
        void* pItemList = reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER + jx::OFF_PLAYER_ITEM_LIST);

        uint32_t subOp = static_cast<uint32_t>(params.GetInt("sub_opcode", 0));
        uint32_t pwd = static_cast<uint32_t>(params.GetInt("password", 0));
        uint32_t newPwd = static_cast<uint32_t>(params.GetInt("new_password", 0));

        switch (subOp) {
        case 0:
            reinterpret_cast<PfnBoxPass1>(jx::FN_BOX_SET_PASSWORD)(pItemList, pwd);
            break;
        case 1:
            reinterpret_cast<PfnBoxPass1>(jx::FN_BOX_CANCEL_PASSWORD)(pItemList, pwd);
            break;
        case 2:
            reinterpret_cast<PfnBoxPass2>(jx::FN_BOX_CHANGE_PASSWORD)(pItemList, pwd, newPwd);
            break;
        case 3:
            reinterpret_cast<PfnBoxLock>(jx::FN_BOX_LOCK)(pItemList);
            break;
        case 4:
            reinterpret_cast<PfnBoxPass1>(jx::FN_BOX_UNLOCK)(pItemList, pwd);
            break;
        default:
            return RpcError("Invalid box_operate sub_opcode (expected 0..4)");
        }

        std::ostringstream oss;
        oss << "{\"sent\":true,\"sub_opcode\":" << subOp << "}";
        return oss.str();
    }

    if (method == "team_create") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");
        int ret = reinterpret_cast<PfnTeamCreate>(jx::FN_TEAM_CREATE)(reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER));
        std::ostringstream oss;
        oss << "{\"sent\":true,\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "team_open_close") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");
        int32_t bOpen = params.GetInt("open", 1) != 0 ? 1 : 0;
        auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
        fnCoreOp(jx::GOI_TEAM_MANAGE_OPEN, 0, bOpen);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"open\":" << (bOpen ? "true" : "false") << "}";
        return oss.str();
    }

    if (method == "team_invite") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uint32_t targetDwId = static_cast<uint32_t>(params.GetInt("target_npc_dw_id", 0));
        if (targetDwId == 0) {
            return RpcError("team_invite requires target_npc_dw_id > 0");
        }
        reinterpret_cast<PfnTeamInviteAdd>(jx::FN_TEAM_INVITE_ADD)(targetDwId);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"target_npc_dw_id\":" << targetDwId << "}";
        return oss.str();
    }

    if (method == "team_reply_invite") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        int32_t inviterIdx = static_cast<int32_t>(params.GetInt("inviter_player_idx", -1));
        int32_t bAccept = params.GetInt("accept", 1) != 0 ? 1 : 0;
        {
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            if (inviterIdx < 0 && !g.pendingTeamInvites.empty()) {
                inviterIdx = g.pendingTeamInvites[g.pendingTeamInvites.size() - 1].inviterPlayerIdx;
            }
            for (auto it = g.pendingTeamInvites.begin(); it != g.pendingTeamInvites.end();) {
                if (it->inviterPlayerIdx == inviterIdx) it = g.pendingTeamInvites.erase(it);
                else ++it;
            }
        }
        if (inviterIdx < 0) {
            return RpcError("team_reply_invite requires inviter_player_idx or a pending team invite");
        }
        reinterpret_cast<PfnTeamReplyInvite>(jx::FN_TEAM_REPLY_INVITE)(inviterIdx, bAccept);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"inviter_player_idx\":" << inviterIdx << ",\"accept\":" << (bAccept ? "true" : "false") << "}";
        return oss.str();
    }

    if (method == "team_apply_join") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        uintptr_t npcBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        SafeRead(jx::ADDR_NPC_ARRAY_PTR, npcBase);
        if (playerBase == 0 || npcBase == 0) return RpcError("Player or Npc array not initialized");

        int32_t targetIdx = static_cast<int32_t>(params.GetInt("array_index", -1));
        uint32_t targetDwId = static_cast<uint32_t>(params.GetInt("target_npc_dw_id", 0));
        if (targetIdx <= 0 && targetDwId != 0) {
            for (int i = 1; i < jx::MAX_NPC_COUNT; ++i) {
                uintptr_t nAddr = npcBase + static_cast<size_t>(i) * jx::STRIDE_NPC;
                int32_t active = 0;
                uint32_t dw = 0;
                if (SafeRead(nAddr + jx::OFF_NPC_INDEX, active) && active > 0 &&
                    SafeRead(nAddr + jx::OFF_NPC_DW_ID, dw) && dw == targetDwId) {
                    targetIdx = i;
                    break;
                }
            }
        }
        if (targetIdx <= 0 || targetIdx >= jx::MAX_NPC_COUNT) {
            return RpcError("Target captain NPC not found in active client NPC array");
        }
        reinterpret_cast<PfnTeamApplyJoin>(jx::FN_TEAM_APPLY_JOIN)(reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER), targetIdx);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"array_index\":" << targetIdx << "}";
        return oss.str();
    }

    if (method == "team_accept_member") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");
        uint32_t applicantDwId = static_cast<uint32_t>(params.GetInt("applicant_npc_dw_id", 0));
        {
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            if (applicantDwId == 0 && !g.pendingTeamApplies.empty()) {
                applicantDwId = g.pendingTeamApplies[g.pendingTeamApplies.size() - 1].applicantNpcDwId;
            }
            for (auto it = g.pendingTeamApplies.begin(); it != g.pendingTeamApplies.end();) {
                if (it->applicantNpcDwId == applicantDwId) it = g.pendingTeamApplies.erase(it);
                else ++it;
            }
        }
        if (applicantDwId == 0) {
            return RpcError("team_accept_member requires applicant_npc_dw_id > 0");
        }
        reinterpret_cast<PfnTeamAcceptMember>(jx::FN_TEAM_ACCEPT_MEMBER)(reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER), applicantDwId);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"applicant_npc_dw_id\":" << applicantDwId << "}";
        return oss.str();
    }

    if (method == "team_leave") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        reinterpret_cast<PfnTeamLeave>(jx::FN_TEAM_LEAVE)();
        return "{\"sent\":true}";
    }

    if (method == "team_kick") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");
        uint32_t memberDwId = static_cast<uint32_t>(params.GetInt("member_npc_dw_id", 0));
        if (memberDwId == 0) return RpcError("team_kick requires member_npc_dw_id > 0");
        reinterpret_cast<PfnTeamKickMember>(jx::FN_TEAM_KICK_MEMBER)(reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER), memberDwId);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"member_npc_dw_id\":" << memberDwId << "}";
        return oss.str();
    }

    if (method == "team_appoint") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");
        uint32_t newCapDwId = static_cast<uint32_t>(params.GetInt("new_captain_npc_dw_id", 0));
        if (newCapDwId == 0) return RpcError("team_appoint requires new_captain_npc_dw_id > 0");
        reinterpret_cast<PfnTeamAppointCaptain>(jx::FN_TEAM_APPOINT_CAPTAIN)(reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER), newCapDwId);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"new_captain_npc_dw_id\":" << newCapDwId << "}";
        return oss.str();
    }

    if (method == "change_aura_skill") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        int32_t skillId = static_cast<int32_t>(params.GetInt("skill_id", 0));
        reinterpret_cast<PfnNpcSetAuraSkill>(jx::FN_NPC_SET_AURA_SKILL)(reinterpret_cast<void*>(selfNpcAddr), skillId);
        int32_t activeAfter = 0;
        SafeRead(selfNpcAddr + jx::OFF_NPC_ACTIVE_AURA_SKILL, activeAfter);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"skill_id\":" << skillId << ",\"active_aura_skill_id\":" << activeAfter << "}";
        return oss.str();
    }

    if (method == "friend_invite") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        std::vector<uint8_t> nameBytes = jxjson::FromHex(params.GetString("name_tcvn3_hex"));
        if (nameBytes.empty() || nameBytes.size() >= 32) {
            return RpcError("friend_invite requires 1..31 TCVN3 bytes in name_tcvn3_hex");
        }
        char roleName[32] = {};
        std::memcpy(roleName, nameBytes.data(), nameBytes.size());
        auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
        int ret = fnCoreOp(jx::GOI_FRIEND_INVITE, reinterpret_cast<uint32_t>(roleName), 0);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "friend_reply") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        std::string nameHex = params.GetString("name_tcvn3_hex");
        int32_t bAccept = params.GetInt("accept", 1) != 0 ? 1 : 0;
        {
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            if (nameHex.empty() && !g.pendingFriendInvitesHex.empty()) {
                nameHex = g.pendingFriendInvitesHex[g.pendingFriendInvitesHex.size() - 1];
            }
            for (auto it = g.pendingFriendInvitesHex.begin(); it != g.pendingFriendInvitesHex.end();) {
                if (*it == nameHex) it = g.pendingFriendInvitesHex.erase(it);
                else ++it;
            }
        }
        std::vector<uint8_t> nameBytes = jxjson::FromHex(nameHex);
        if (nameBytes.empty() || nameBytes.size() >= 32) {
            return RpcError("friend_reply requires 1..31 TCVN3 bytes in name_tcvn3_hex or a pending invite");
        }
        char roleName[32] = {};
        std::memcpy(roleName, nameBytes.data(), nameBytes.size());
        auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
        // game.exe 0x0053ca69 (GOI_FRIEND_INVITE_RESPONSE = 0x2A): test esi, esi; sete al; mov [edi+0x26], al
        // -> nParam == 0 encodes rep_yes (1), nParam != 0 encodes rep_no (0)
        int ret = fnCoreOp(jx::GOI_FRIEND_INVITE_RESPONSE, reinterpret_cast<uint32_t>(roleName), bAccept ? 0 : 1);
        if (bAccept != 0) {
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            bool found = false;
            for (auto& s : g.socialRelations) {
                if (s.nameHex == nameHex && !s.isAssociate) {
                    s.state = 1;
                    found = true;
                    break;
                }
            }
            if (!found) {
                SocialRelationRecord rec{};
                rec.nameHex = nameHex;
                rec.state = 1;
                rec.isAssociate = false;
                g.socialRelations.push_back(std::move(rec));
            }
        }
        std::ostringstream oss;
        oss << "{\"sent\":true,\"accept\":" << (bAccept ? "true" : "false") << ",\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "friend_delete") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        std::string nameHex = params.GetString("name_tcvn3_hex");
        std::vector<uint8_t> nameBytes = jxjson::FromHex(nameHex);
        if (nameBytes.empty() || nameBytes.size() >= 32) {
            return RpcError("friend_delete requires 1..31 TCVN3 bytes in name_tcvn3_hex");
        }
        char roleName[32] = {};
        std::memcpy(roleName, nameBytes.data(), nameBytes.size());

        auto& g = GetGlobals();
        uint32_t beforeSeq = g.c2sSeq.load(std::memory_order_acquire);
        if (*reinterpret_cast<void**>(jx::ADDR_UI_CHAT_CENTRE_PTR) != nullptr) {
            reinterpret_cast<PfnUiDeleteFriend>(jx::FN_UI_DELETE_FRIEND)(0, roleName, 1);
        }
        if (g.c2sSeq.load(std::memory_order_acquire) == beforeSeq) {
            // Exact CFriendNotify::DeleteFriend packet @ 0x004756fc..0x0047576a (37B: 0xFC, wLen=36, pf=7, id=3, role[32])
            void* pCoreShell = *reinterpret_cast<void**>(jx::ADDR_G_CORE_SHELL);
            if (pCoreShell != nullptr) {
                uint8_t pkt[37] = {};
                pkt[0] = 0xFC;
                uint16_t wLen = 36;
                std::memcpy(pkt + 1, &wLen, sizeof(wLen));
                pkt[3] = 7;
                pkt[4] = 3;
                std::memcpy(pkt + 5, roleName, sizeof(roleName));
                reinterpret_cast<PfnSendNewDataToCore>(jx::FN_SEND_NEW_DATA_TO_CORE)(pCoreShell, pkt, sizeof(pkt));
            }
        }
        {
            std::lock_guard<std::mutex> lk(g.uiMutex);
            for (auto it = g.socialRelations.begin(); it != g.socialRelations.end();) {
                if (it->nameHex == nameHex && !it->isAssociate) it = g.socialRelations.erase(it);
                else ++it;
            }
        }
        return "{\"sent\":true}";
    }

    if (method == "whisper_send") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) {
            return RpcError("Local player is not in world");
        }
        std::vector<uint8_t> destBytes = jxjson::FromHex(params.GetString("target_name_tcvn3_hex"));
        std::vector<uint8_t> msgBytes = jxjson::FromHex(params.GetString("message_tcvn3_hex"));
        if (destBytes.empty() || destBytes.size() >= 32) {
            return RpcError("whisper_send requires 1..31 TCVN3 bytes in target_name_tcvn3_hex");
        }
        if (msgBytes.empty() || msgBytes.size() >= 200) {
            return RpcError("whisper_send requires 1..199 TCVN3 bytes in message_tcvn3_hex");
        }
        char destName[32] = {};
        std::memcpy(destName, destBytes.data(), destBytes.size());
        char sentence[204] = {};
        std::memcpy(sentence, msgBytes.data(), msgBytes.size());

        auto& g = GetGlobals();
        uint32_t beforeSeq = g.c2sSeq.load(std::memory_order_acquire);
        if (*reinterpret_cast<void**>(jx::ADDR_UI_PLAYER_BAR_PTR) != nullptr) {
            reinterpret_cast<PfnUiSendSomeoneChat>(jx::FN_UI_SEND_SOMEONE_CHAT)(
                destName, sentence, static_cast<int32_t>(msgBytes.size()));
        }
        if (g.c2sSeq.load(std::memory_order_acquire) == beforeSeq) {
            // Exact KUiPlayerBar::SendSomeoneMessage packet @ 0x0044d6bc..0x0044d7da
            void* pCoreShell = *reinterpret_cast<void**>(jx::ADDR_G_CORE_SHELL);
            if (pCoreShell != nullptr) {
                uint8_t sentLen = static_cast<uint8_t>(msgBytes.size());
                uint16_t innerTotal = static_cast<uint16_t>(0x28u + sentLen);
                uint16_t outerTotal = static_cast<uint16_t>(3u + innerTotal);
                uint8_t pkt[256] = {};
                pkt[0] = 0xFB;
                uint16_t outerWLen = static_cast<uint16_t>(outerTotal - 1u);
                std::memcpy(pkt + 1, &outerWLen, sizeof(outerWLen));
                pkt[3] = 0x20;
                uint16_t innerWLen = static_cast<uint16_t>(innerTotal - 1u);
                std::memcpy(pkt + 4, &innerWLen, sizeof(innerWLen));
                uint32_t pkgId = 0xFFFFFFFFu;
                std::memcpy(pkt + 6, &pkgId, sizeof(pkgId));
                std::memcpy(pkt + 10, destName, std::strlen(destName) + 1);
                pkt[3 + 0x27] = sentLen;
                std::memcpy(pkt + 3 + 0x28, msgBytes.data(), sentLen);
                reinterpret_cast<PfnSendNewDataToCore>(jx::FN_SEND_NEW_DATA_TO_CORE)(pCoreShell, pkt, outerTotal);
            }
        }
        std::ostringstream oss;
        oss << "{\"sent\":true,\"len\":" << msgBytes.size() << "}";
        return oss.str();
    }

    if (method == "blacklist_add") {
        std::string nameHex = params.GetString("name_tcvn3_hex");
        std::vector<uint8_t> nameBytes = jxjson::FromHex(nameHex);
        if (nameBytes.empty() || nameBytes.size() >= 32) {
            return RpcError("blacklist_add requires 1..31 TCVN3 bytes in name_tcvn3_hex");
        }
        char roleName[32] = {};
        std::memcpy(roleName, nameBytes.data(), nameBytes.size());
        const char* groupStr = "";
        reinterpret_cast<PfnBlacklistAdd>(jx::FN_BLACKLIST_ADD)(roleName, groupStr, 0);
        bool inBlack = reinterpret_cast<PfnBlacklistContains>(jx::FN_BLACKLIST_CONTAINS)(roleName);
        if (inBlack) {
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            bool exists = false;
            for (const auto& h : g.blacklistNamesHex) {
                if (h == nameHex) { exists = true; break; }
            }
            if (!exists) g.blacklistNamesHex.push_back(nameHex);
        }
        std::ostringstream oss;
        oss << "{\"added\":" << (inBlack ? "true" : "false") << "}";
        return oss.str();
    }

    if (method == "blacklist_remove") {
        std::string nameHex = params.GetString("name_tcvn3_hex");
        std::vector<uint8_t> nameBytes = jxjson::FromHex(nameHex);
        if (nameBytes.empty() || nameBytes.size() >= 32) {
            return RpcError("blacklist_remove requires 1..31 TCVN3 bytes in name_tcvn3_hex");
        }
        char roleName[32] = {};
        std::memcpy(roleName, nameBytes.data(), nameBytes.size());
        reinterpret_cast<PfnBlacklistRemove>(jx::FN_BLACKLIST_REMOVE)(roleName);
        bool inBlack = reinterpret_cast<PfnBlacklistContains>(jx::FN_BLACKLIST_CONTAINS)(roleName);
        if (!inBlack) {
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            for (auto it = g.blacklistNamesHex.begin(); it != g.blacklistNamesHex.end();) {
                if (*it == nameHex) it = g.blacklistNamesHex.erase(it);
                else ++it;
            }
        }
        std::ostringstream oss;
        oss << "{\"in_blacklist\":" << (inBlack ? "true" : "false") << "}";
        return oss.str();
    }

    if (method == "trade_set_open") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");
        void* pPlayer1 = reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER);
        int32_t bOpen = params.GetInt("open", 1) != 0 ? 1 : 0;
        if (bOpen) {
            std::vector<uint8_t> msgBytes = jxjson::FromHex(params.GetString("message_tcvn3_hex"));
            char msgBuf[256] = {};
            size_t copyLen = msgBytes.size() < 255 ? msgBytes.size() : 255;
            if (copyLen > 0) std::memcpy(msgBuf, msgBytes.data(), copyLen);
            reinterpret_cast<PfnTradeOpen>(jx::FN_TRADE_OPEN)(pPlayer1, msgBuf, static_cast<int32_t>(copyLen));
        } else {
            reinterpret_cast<PfnTradeClose>(jx::FN_TRADE_CLOSE)(pPlayer1);
        }
        std::ostringstream oss;
        oss << "{\"sent\":true,\"open\":" << (bOpen ? "true" : "false") << "}";
        return oss.str();
    }

    if (method == "trade_invite") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        uintptr_t npcBase = 0;
        SafeRead(jx::ADDR_NPC_ARRAY_PTR, npcBase);
        if (npcBase == 0) return RpcError("Npc array not initialized");
        int32_t targetIdx = static_cast<int32_t>(params.GetInt("array_index", -1));
        uint32_t targetDwId = static_cast<uint32_t>(params.GetInt("target_npc_dw_id", 0));
        if (targetIdx <= 0 && targetDwId != 0) {
            for (int i = 1; i < jx::MAX_NPC_COUNT; ++i) {
                uintptr_t nAddr = npcBase + static_cast<size_t>(i) * jx::STRIDE_NPC;
                int32_t active = 0;
                uint32_t dw = 0;
                if (SafeRead(nAddr + jx::OFF_NPC_INDEX, active) && active > 0 &&
                    SafeRead(nAddr + jx::OFF_NPC_DW_ID, dw) && dw == targetDwId) {
                    targetIdx = i;
                    break;
                }
            }
        }
        if (targetIdx <= 0 || targetIdx >= jx::MAX_NPC_COUNT) {
            return RpcError("Target trade partner NPC not found in active client NPC array");
        }
        reinterpret_cast<PfnTradeApplyStart>(jx::FN_TRADE_APPLY_START)(targetIdx);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"array_index\":" << targetIdx << "}";
        return oss.str();
    }

    if (method == "trade_reply") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        int32_t inviterIdx = static_cast<int32_t>(params.GetInt("inviter_player_idx", -1));
        int32_t bAccept = params.GetInt("accept", 1) != 0 ? 1 : 0;
        {
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            if (inviterIdx < 0 && !g.pendingTradeInvites.empty()) {
                inviterIdx = g.pendingTradeInvites[g.pendingTradeInvites.size() - 1].inviterPlayerIdx;
            }
            for (auto it = g.pendingTradeInvites.begin(); it != g.pendingTradeInvites.end();) {
                if (it->inviterPlayerIdx == inviterIdx) it = g.pendingTradeInvites.erase(it);
                else ++it;
            }
        }
        if (inviterIdx < 0) return RpcError("trade_reply requires inviter_player_idx or a pending trade invite");
        reinterpret_cast<PfnTradeReplyStart>(jx::FN_TRADE_REPLY_START)(inviterIdx, bAccept);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"inviter_player_idx\":" << inviterIdx << ",\"accept\":" << (bAccept ? "true" : "false") << "}";
        return oss.str();
    }

    if (method == "trade_set_money") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");
        int32_t money = static_cast<int32_t>(params.GetInt("money", 0));
        int32_t ret = reinterpret_cast<PfnTradeMoveMoney>(jx::FN_TRADE_MOVE_MONEY)(
            reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER), money);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"money\":" << money << ",\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "trade_lock") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");
        int32_t bLock = params.GetInt("lock", 1) != 0 ? 1 : 0;
        reinterpret_cast<PfnTradeLock>(jx::FN_TRADE_LOCK)(
            reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER), bLock);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"lock\":" << (bLock ? "true" : "false") << "}";
        return oss.str();
    }

    if (method == "trade_confirm") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        uintptr_t playerBase = 0;
        SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase);
        if (playerBase == 0) return RpcError("Player array not initialized");
        int32_t bOk = params.GetInt("accept", 1) != 0 ? 1 : 0;
        reinterpret_cast<PfnTradeDecision>(jx::FN_TRADE_DECISION)(
            reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER), bOk);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"accept\":" << (bOk ? "true" : "false") << "}";
        return oss.str();
    }

    if (method == "stall_set_banner") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        std::vector<uint8_t> advBytes = jxjson::FromHex(params.GetString("adv_tcvn3_hex"));
        if (advBytes.empty() || advBytes.size() >= 64) {
            return RpcError("stall_set_banner requires 1..63 TCVN3 bytes in adv_tcvn3_hex");
        }
        char advBuf[64] = {};
        std::memcpy(advBuf, advBytes.data(), advBytes.size());
        auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
        int ret = fnCoreOp(jx::GOI_STALL_SET_ADV, reinterpret_cast<uint32_t>(advBuf), 0);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "stall_mark_price") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        int32_t arrIdx = static_cast<int32_t>(params.GetInt("array_index", -1));
        uint32_t serverItemId = static_cast<uint32_t>(params.GetInt("server_item_id", 0));
        int32_t price = static_cast<int32_t>(params.GetInt("price", 0));
        if (arrIdx <= 0 && serverItemId != 0) {
            uintptr_t itemBase = 0;
            SafeRead(jx::ADDR_ITEM_ARRAY_PTR, itemBase);
            if (itemBase != 0) {
                for (int i = 1; i < jx::MAX_ITEM_COUNT; ++i) {
                    uint32_t cand = 0;
                    if (SafeRead(itemBase + static_cast<size_t>(i) * jx::STRIDE_ITEM + jx::OFF_ITEM_SERVER_ID, cand) &&
                        cand == serverItemId) {
                        arrIdx = i;
                        break;
                    }
                }
            }
        }
        if (arrIdx <= 0 || arrIdx >= jx::MAX_ITEM_COUNT) {
            return RpcError("stall_mark_price requires valid array_index or server_item_id");
        }
        auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
        int ret = fnCoreOp(jx::GOI_STALL_MARK_PRICE, static_cast<uint32_t>(arrIdx), price);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"array_index\":" << arrIdx << ",\"price\":" << price << ",\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "stall_open") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
        int ret = fnCoreOp(jx::GOI_STALL_TOGGLE_OPEN, 0, 0);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "stall_close") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
        int ret = fnCoreOp(jx::GOI_STALL_CLOSE, 0, 0);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "stall_view") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        uint32_t sellerDwId = static_cast<uint32_t>(params.GetInt("seller_npc_dw_id", 0));
        if (sellerDwId == 0) return RpcError("stall_view requires seller_npc_dw_id > 0");
        auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
        // GOI 66 (0x42) sends 0x7C sub=4 (RequestAdv); upon receiving 0x9D sub=1, client 0x00548020 sends 0x7C sub=5 (RequestItems)
        int ret = fnCoreOp(jx::GOI_STALL_VIEW_ITEMS, sellerDwId, 0);
        if (params.GetInt("also_request_items", 0) != 0) {
            fnCoreOp(jx::GOI_STALL_VIEW_ADV, sellerDwId, 0);
        }
        std::ostringstream oss;
        oss << "{\"sent\":true,\"seller_npc_dw_id\":" << sellerDwId << ",\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "stall_buy") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        int32_t arrIdx = static_cast<int32_t>(params.GetInt("array_index", -1));
        uint32_t serverItemId = static_cast<uint32_t>(params.GetInt("server_item_id", 0));
        if (arrIdx <= 0 && serverItemId != 0) {
            uintptr_t itemBase = 0;
            SafeRead(jx::ADDR_ITEM_ARRAY_PTR, itemBase);
            if (itemBase != 0) {
                for (int i = 1; i < jx::MAX_ITEM_COUNT; ++i) {
                    uint32_t cand = 0;
                    if (SafeRead(itemBase + static_cast<size_t>(i) * jx::STRIDE_ITEM + jx::OFF_ITEM_SERVER_ID, cand) &&
                        cand == serverItemId) {
                        arrIdx = i;
                        break;
                    }
                }
            }
        }
        if (arrIdx <= 0 || arrIdx >= jx::MAX_ITEM_COUNT) {
            return RpcError("stall_buy requires valid client Item[] array_index or viewed server_item_id");
        }
        auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
        int ret = fnCoreOp(jx::GOI_STALL_BUY, static_cast<uint32_t>(arrIdx), 0);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"array_index\":" << arrIdx << ",\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "enchaser_operate") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        int32_t arr[5] = {};
        arr[0] = static_cast<int32_t>(params.GetInt("equip_idx", 0));
        arr[1] = static_cast<int32_t>(params.GetInt("mineral_idx", 0));
        arr[2] = static_cast<int32_t>(params.GetInt("slot_one_based", 1));
        arr[3] = static_cast<int32_t>(params.GetInt("catalyst2_idx", 0));
        arr[4] = static_cast<int32_t>(params.GetInt("huyen_tinh_idx", 0));
        auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
        int ret = fnCoreOp(jx::GOI_ENCHASER_COMPOUND, reinterpret_cast<uint32_t>(arr), 0);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "foundry_operate") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        // Set active blacksmith foundry scheme at ds:0x008e48ec (@ 0x00538bc6 -> [ResScheme_1] in foundryresdemand.ini)
        *reinterpret_cast<uint8_t*>(0x008e48ec) = static_cast<uint8_t>(params.GetInt("res_scheme", 1) & 0xFF);
        jx::FoundryReq80B req{};
        req.Action = static_cast<uint8_t>(params.GetInt("action", 0) & 0xFF);
        req.MainCount = static_cast<int32_t>(params.GetInt("main_count", 0));
        req.CatalystCount = static_cast<int32_t>(params.GetInt("catalyst_count", 0));
        req.Option = static_cast<int32_t>(params.GetInt("option", 0));
        for (int i = 0; i < 8; ++i) {
            char mk[16] = {};
            char ck[16] = {};
            std::snprintf(mk, sizeof(mk), "main_%d", i);
            std::snprintf(ck, sizeof(ck), "cat_%d", i);
            req.MainItemIdx[i] = static_cast<int32_t>(params.GetInt(mk, 0));
            req.CatalystItemIdx[i] = static_cast<int32_t>(params.GetInt(ck, 0));
        }
        auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
        int ret = fnCoreOp(jx::GOI_FOUNDRY_REQUEST, reinterpret_cast<uint32_t>(&req), 0);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"action\":" << static_cast<unsigned>(req.Action) << ",\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "tong_create") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        uintptr_t playerBase = 0;
        if (!SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase) || playerBase == 0) return RpcError("Player array is null");
        std::vector<uint8_t> nameBytes = jxjson::FromHex(params.GetString("tong_name_tcvn3_hex"));
        int32_t camp = static_cast<int32_t>(params.GetInt("camp", 1));
        if (nameBytes.empty() || nameBytes.size() > 8) {
            return RpcError("tong_create requires 1..8 TCVN3 bytes in tong_name_tcvn3_hex (jx_linux_y 0x080e7eb1)");
        }
        char nameBuf[12] = {};
        std::memcpy(nameBuf, nameBytes.data(), nameBytes.size());
        void* pPlayerTong = reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER + jx::OFF_PLAYER_TONG);
        auto fnCreate = reinterpret_cast<PfnTongApplyCreate>(jx::FN_TONG_APPLY_CREATE);
        int32_t ret = fnCreate(pPlayerTong, camp, nameBuf);
        std::ostringstream oss;
        oss << "{\"sent\":true,\"camp\":" << camp << ",\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "tong_action") {
        uintptr_t selfNpcAddr = 0;
        if (ResolveLocalPlayerNpcIndex(selfNpcAddr) <= 0) return RpcError("Local player is not in world");
        uintptr_t playerBase = 0;
        if (!SafeRead(jx::ADDR_PLAYER_ARRAY_PTR, playerBase) || playerBase == 0) return RpcError("Player array is null");
        void* pPlayerTong = reinterpret_cast<void*>(playerBase + jx::STRIDE_PLAYER + jx::OFF_PLAYER_TONG);
        std::string kind = params.GetString("kind");
        std::vector<uint8_t> nameBytes = jxjson::FromHex(params.GetString("target_name_tcvn3_hex"));
        char nameBuf[32] = {};
        if (!nameBytes.empty() && nameBytes.size() < 32) {
            std::memcpy(nameBuf, nameBytes.data(), nameBytes.size());
        }
        int32_t ret = 1;
        if (kind == "join") {
            uint32_t targetDwId = static_cast<uint32_t>(params.GetInt("target_npc_dw_id", 0));
            if (targetDwId == 0 && nameBuf[0] != '\0') {
                uintptr_t npcBase = 0;
                if (SafeRead(jx::ADDR_NPC_ARRAY_PTR, npcBase) && npcBase != 0) {
                    for (int i = 1; i < jx::MAX_NPC_COUNT; ++i) {
                        uintptr_t na = npcBase + static_cast<size_t>(i) * jx::STRIDE_NPC;
                        int32_t act = 0;
                        char candName[32] = {};
                        if (SafeRead(na + jx::OFF_NPC_INDEX, act) && act > 0 &&
                            SafeCopyMem(candName, reinterpret_cast<const void*>(na + jx::OFF_NPC_NAME), sizeof(candName)) &&
                            std::strncmp(candName, nameBuf, 31) == 0) {
                            SafeRead(na + jx::OFF_NPC_DW_ID, targetDwId);
                            break;
                        }
                    }
                }
            }
            if (targetDwId == 0) return RpcError("tong_action join requires target_npc_dw_id or visible target_name_tcvn3_hex");
            auto fnAdd = reinterpret_cast<PfnTongApplyAdd>(jx::FN_TONG_APPLY_ADD);
            ret = fnAdd(pPlayerTong, targetDwId);
        } else if (kind == "accept" || kind == "refuse") {
            int32_t appIdx = static_cast<int32_t>(params.GetInt("applicant_player_idx", -1));
            if (appIdx <= 0 || nameBuf[0] == '\0') {
                auto& g = GetGlobals();
                std::lock_guard<std::mutex> lk(g.uiMutex);
                if (appIdx <= 0) appIdx = g.lastTongApplicantPlayerIdx;
                if (nameBuf[0] == '\0' && !g.lastTongApplicantNameHex.empty()) {
                    std::vector<uint8_t> saved = jxjson::FromHex(g.lastTongApplicantNameHex);
                    if (!saved.empty() && saved.size() < 32) std::memcpy(nameBuf, saved.data(), saved.size());
                }
            }
            if (appIdx <= 0 || nameBuf[0] == '\0') {
                return RpcError("tong_action accept/refuse requires pending S2C 0xFD sub=1 applicant or explicit applicant_player_idx + target_name_tcvn3_hex");
            }
            uint32_t nameId = HashTongMemberNameToId(nameBuf);
            auto fnAccept = reinterpret_cast<PfnTongAcceptMember>(jx::FN_TONG_ACCEPT_MEMBER);
            fnAccept(pPlayerTong, appIdx, nameId, kind == "accept" ? 1 : 0);
            {
                auto& g = GetGlobals();
                std::lock_guard<std::mutex> lk(g.uiMutex);
                if (g.lastTongApplicantPlayerIdx == appIdx) {
                    g.lastTongApplicantPlayerIdx = -1;
                    g.lastTongApplicantNameHex.clear();
                }
            }
        } else if (kind == "leave") {
            auto fnLeave = reinterpret_cast<PfnTongApplyLeave>(jx::FN_TONG_APPLY_LEAVE);
            fnLeave(pPlayerTong);
        } else {
            return RpcError("tong_action kind must be join|accept|refuse|leave");
        }
        std::ostringstream oss;
        oss << "{\"sent\":true,\"kind\":" << jxjson::QuoteJson(kind) << ",\"return_value\":" << ret << "}";
        return oss.str();
    }

    if (method == "begin_wire_capture") {
        auto& g = GetGlobals();
        uint32_t curTick = *reinterpret_cast<const uint32_t*>(jx::ADDR_KMYAPP_GAMELOOP_COUNT);
        {
            std::lock_guard<std::mutex> lk(g.wireCaptureMutex);
            g.wireCaptureFrames.clear();
            g.wireCaptureFrames.reserve(256);
            g.wireCaptureBaseTick = curTick;
            g.wireCaptureActive.store(true, std::memory_order_release);
        }
        std::ostringstream oss;
        oss << "{\"capturing\":true,\"base_tick\":" << curTick << "}";
        return oss.str();
    }

    if (method == "end_wire_capture") {
        auto& g = GetGlobals();
        uint32_t endTick = *reinterpret_cast<const uint32_t*>(jx::ADDR_KMYAPP_GAMELOOP_COUNT);
        std::vector<CapturedWireFrameRecord> captured;
        uint32_t baseTick = 0;
        {
            std::lock_guard<std::mutex> lk(g.wireCaptureMutex);
            g.wireCaptureActive.store(false, std::memory_order_release);
            baseTick = g.wireCaptureBaseTick;
            captured = std::move(g.wireCaptureFrames);
        }
        std::ostringstream oss;
        oss << "{\"capturing\":false,\"base_tick\":" << baseTick << ",\"end_tick\":" << endTick << ",\"frames\":[";
        for (size_t i = 0; i < captured.size(); ++i) {
            if (i > 0) oss << ",";
            const auto& f = captured[i];
            int64_t relTick = static_cast<int64_t>(f.tickCount) - static_cast<int64_t>(baseTick);
            oss << "{\"dir\":" << (f.isC2s ? "\"c2s\"" : "\"s2c\"")
                << ",\"seq\":" << f.seq
                << ",\"tick_count\":" << f.tickCount
                << ",\"relative_tick\":" << relTick
                << ",\"timestamp_ms\":" << f.timestampMs
                << ",\"opcode\":" << static_cast<uint32_t>(f.opcode)
                << ",\"length\":" << f.bytes.size()
                << ",\"raw_hex\":" << jxjson::QuoteJson(jxjson::ToHex(f.bytes.data(), f.bytes.size()))
                << "}";
        }
        oss << "]}";
        return oss.str();
    }

    if (method == "raw_op") {
        uint32_t uOper = static_cast<uint32_t>(params.GetInt("u_oper", 0));
        uint32_t uParam = static_cast<uint32_t>(params.GetInt("u_param", 0));
        int32_t nParam = static_cast<int32_t>(params.GetInt("n_param", 0));
        int ret = 0;
        __try {
            auto fnCoreOp = reinterpret_cast<PfnCoreShellOpRequest>(jx::FN_CORESHELL_OP_REQUEST);
            ret = fnCoreOp(uOper, uParam, nParam);
        } __except (EXCEPTION_EXECUTE_HANDLER) {
            ret = -1;
        }
        std::ostringstream oss;
        oss << "{\"return_value\":" << ret << ",\"u_oper\":" << uOper << ",\"sent_packet\":false}";
        return oss.str();
    }

    return RpcError("Unknown RPC method: " + method);
}

} // namespace jxagent
