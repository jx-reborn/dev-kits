#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <atomic>
#include <cstdint>
#include <functional>
#include <mutex>
#include <string>
#include <vector>

namespace jxagent {

constexpr size_t kS2cRingSize = 256;
constexpr size_t kC2sRingSize = 256;
constexpr size_t kMaxCapturedPacketBytes = 256;

struct S2cPacketRecord {
    uint32_t seq;
    uint32_t tickCount;
    uint32_t timestampMs;
    uint8_t opcode;
    uint16_t length;
    uint16_t capturedLen;
    uint8_t bytes[kMaxCapturedPacketBytes];
};

struct C2sPacketRecord {
    uint32_t seq;
    uint32_t tickCount;
    uint32_t timestampMs;
    uint8_t opcode;
    uint16_t length;
    uint16_t capturedLen;
    uint8_t bytes[kMaxCapturedPacketBytes];
};

struct SystemMessageRecord {
    uint32_t seq;
    uint32_t tickCount;
    std::string textHex;
};

struct ChatMessageRecord {
    uint32_t seq;
    uint32_t tickCount;
    uint32_t channelId;
    std::string senderHex;
    std::string textHex;
};

struct DialogState {
    uint32_t sequence = 0;
    uint32_t openedTick = 0;
    bool isOpen = false;
    bool isTalk = false;
    bool talkNeedCallback = false;
    std::string promptHex;
    std::vector<std::string> optionsHex;
};

struct PendingTeamInviteRecord {
    uint32_t seq = 0;
    uint32_t tickCount = 0;
    int32_t inviterPlayerIdx = -1;
    std::string inviterNameHex;
};

struct PendingTeamApplyRecord {
    uint32_t seq = 0;
    uint32_t tickCount = 0;
    uint32_t applicantNpcDwId = 0;
    std::string applicantNameHex;
};

struct SocialRelationRecord {
    std::string nameHex;
    std::string unitHex;
    std::string groupHex;
    uint8_t state = 0;
    bool isAssociate = false;
};

struct PendingTradeInviteRecord {
    uint32_t seq = 0;
    uint32_t tickCount = 0;
    int32_t inviterPlayerIdx = -1;
    uint32_t inviterNpcDwId = 0;
};

struct TradeWireState {
    uint8_t selfLocked = 0;
    uint8_t otherLocked = 0;
    uint8_t selfConfirmed = 0;
    uint8_t otherConfirmed = 0;
    int32_t otherMoney = 0;
    int32_t lastDecisionResult = -1;
    uint32_t decisionSeq = 0;
};

struct CraftingResultRecord {
    uint32_t seq = 0;
    uint8_t actionKind = 0;
    uint32_t itemDwId = 0;
    uint8_t resultCode = 0;
};

struct CapturedWireFrameRecord {
    bool isC2s = false;
    uint32_t seq = 0;
    uint32_t tickCount = 0;
    uint32_t timestampMs = 0;
    uint8_t opcode = 0;
    std::vector<uint8_t> bytes;
};

struct AgentGlobals {
    std::atomic<bool> headlessEnabled{false};
    std::atomic<bool> interactiveWindowEnabled{false};
    std::atomic<bool> wireCaptureActive{false};
    std::atomic<bool> quitRequested{false};
    std::atomic<bool> pipeStarted{false};
    std::atomic<bool> pipeClosed{false};
    std::atomic<bool> coreHooksInstalled{false};
    std::atomic<bool> worldSyncReady{false};
    std::atomic<uint32_t> s2cSeq{0};
    std::atomic<uint32_t> c2sSeq{0};
    std::atomic<uint32_t> sysMsgSeq{0};
    std::atomic<uint32_t> chatMsgSeq{0};
    std::atomic<uint32_t> teamInviteSeq{0};
    std::atomic<uint32_t> tradeInviteSeq{0};
    std::atomic<uint32_t> craftingSeq{0};

    std::string pipeName;
    std::string crashDumpPath;
    HWND mainHwnd = nullptr;

    // Lock-free write on main thread / net thread; read safely from crash handler or snapshot
    S2cPacketRecord s2cRing[kS2cRingSize]{};
    C2sPacketRecord c2sRing[kC2sRingSize]{};

    std::mutex uiMutex;
    DialogState dialog;
    std::vector<SystemMessageRecord> systemMessages;
    std::vector<ChatMessageRecord> chatMessages;
    std::vector<std::string> messageBoxes;
    std::vector<PendingTeamInviteRecord> pendingTeamInvites;
    std::vector<PendingTeamApplyRecord> pendingTeamApplies;
    std::vector<SocialRelationRecord> socialRelations;
    std::vector<std::string> pendingFriendInvitesHex;
    std::vector<std::string> blacklistNamesHex;
    std::vector<PendingTradeInviteRecord> pendingTradeInvites;
    TradeWireState tradeWire{};
    CraftingResultRecord lastCrafting{};
    std::string lastViewedStallAdvHex;
    int32_t lastTongApplicantPlayerIdx = -1;
    std::string lastTongApplicantNameHex;
    uint32_t tongChannelId = 0xFFFFFFFFu;
    uint8_t tongChannelCost = 3;
    uint8_t tongChannelSomeFlag = 6;

    std::mutex wireCaptureMutex;
    uint32_t wireCaptureBaseTick = 0;
    std::vector<CapturedWireFrameRecord> wireCaptureFrames;
};

AgentGlobals& GetGlobals();

// CrashHandler.cpp
void InstallVehCrashHandler();
void RecordS2cPacket(const uint8_t* data, size_t len, uint32_t tickCount);
void RecordC2sPacket(const uint8_t* data, size_t len, uint32_t tickCount);

// Hooks.cpp
void InstallInitialHooks();
void EnsureDynamicHooksInstalled();
void StartPipeServerOnce();
bool ExecuteOnMainThread(std::function<std::string()> fn, uint32_t timeoutMs, std::string& outJsonResult, std::string& outError);
void DrainMainThreadQueue();

// JxMemory.cpp
std::string HandleRpcMethod(const std::string& method, const std::string& paramsRawJson);
void SyncSceneFocusToLocalPlayer();

// PipeServer.cpp
void RunPipeServerThread();

} // namespace jxagent
