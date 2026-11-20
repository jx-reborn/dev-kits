#include "AgentState.h"
#include "JsonMini.h"
#include "../include/jx_offsets.h"

#include <shellapi.h>
#include <atomic>
#include <cstdio>
#include <cstring>
#include <deque>
#include <mutex>

namespace jxagent {

namespace {

// Original API pointers (only set once when IAT is first patched)
using PfnCreateWindowExA = HWND(WINAPI*)(DWORD, LPCSTR, LPCSTR, DWORD, int, int, int, int, HWND, HMENU, HINSTANCE, LPVOID);
using PfnShowWindow = BOOL(WINAPI*)(HWND, int);
using PfnSetWindowPos = BOOL(WINAPI*)(HWND, HWND, int, int, int, int, UINT);
using PfnSetForegroundWindow = BOOL(WINAPI*)(HWND);
using PfnSetFocus = HWND(WINAPI*)(HWND);
using PfnSetCapture = HWND(WINAPI*)(HWND);
using PfnClipCursor = BOOL(WINAPI*)(const RECT*);
using PfnShowCursor = int(WINAPI*)(BOOL);
using PfnWaitMessage = BOOL(WINAPI*)();
using PfnMessageBoxA = int(WINAPI*)(HWND, LPCSTR, LPCSTR, UINT);
using PfnMessageBoxW = int(WINAPI*)(HWND, LPCWSTR, LPCWSTR, UINT);
using PfnShellExecuteA = HINSTANCE(WINAPI*)(HWND, LPCSTR, LPCSTR, LPCSTR, LPCSTR, INT);
using PfnShellExecuteW = HINSTANCE(WINAPI*)(HWND, LPCWSTR, LPCWSTR, LPCWSTR, LPCWSTR, INT);
using PfnRegCreateKeyExA = LSTATUS(APIENTRY*)(HKEY, LPCSTR, DWORD, LPSTR, DWORD, REGSAM, const LPSECURITY_ATTRIBUTES, PHKEY, LPDWORD);
using PfnDirectSoundCreate = HRESULT(WINAPI*)(LPCGUID, void**, void*);
using PfnCreateSemaphoreA = HANDLE(WINAPI*)(LPSECURITY_ATTRIBUTES, LONG, LONG, LPCSTR);
using PfnOpenSemaphoreA = HANDLE(WINAPI*)(DWORD, BOOL, LPCSTR);
using PfnCreateMutexA = HANDLE(WINAPI*)(LPSECURITY_ATTRIBUTES, BOOL, LPCSTR);
using PfnOpenMutexA = HANDLE(WINAPI*)(DWORD, BOOL, LPCSTR);
using PfnSetUnhandledExceptionFilter = LPTOP_LEVEL_EXCEPTION_FILTER(WINAPI*)(LPTOP_LEVEL_EXCEPTION_FILTER);

PfnCreateWindowExA g_OrigCreateWindowExA = nullptr;
PfnShowWindow g_OrigShowWindow = nullptr;
PfnSetWindowPos g_OrigSetWindowPos = nullptr;
PfnSetForegroundWindow g_OrigSetForegroundWindow = nullptr;
PfnSetFocus g_OrigSetFocus = nullptr;
PfnSetCapture g_OrigSetCapture = nullptr;
PfnClipCursor g_OrigClipCursor = nullptr;
PfnShowCursor g_OrigShowCursor = nullptr;
PfnWaitMessage g_OrigWaitMessage = nullptr;
PfnMessageBoxA g_OrigMessageBoxA = nullptr;
PfnMessageBoxW g_OrigMessageBoxW = nullptr;
PfnShellExecuteA g_OrigShellExecuteA = nullptr;
PfnShellExecuteW g_OrigShellExecuteW = nullptr;
PfnRegCreateKeyExA g_OrigRegCreateKeyExA = nullptr;
PfnDirectSoundCreate g_OrigDirectSoundCreate = nullptr;
PfnCreateSemaphoreA g_OrigCreateSemaphoreA = nullptr;
PfnOpenSemaphoreA g_OrigOpenSemaphoreA = nullptr;
PfnCreateMutexA g_OrigCreateMutexA = nullptr;
PfnOpenMutexA g_OrigOpenMutexA = nullptr;
PfnSetUnhandledExceptionFilter g_OrigSetUnhandledExceptionFilter = nullptr;

using PfnGameLoop = int(__fastcall*)(void* pThis, void* edxUnused);
using PfnDisconnectAgent = void(__fastcall*)(void* pAgent);
using PfnProcessNetMsg = void(__fastcall*)(void* pProto, void* edxUnused, const uint8_t* pMsg, size_t len);
using PfnS2CChatGetSomeone = void(__fastcall*)(void* pProto, void* edxUnused, const uint8_t* pMsg, int32_t nParam);
using PfnS2CScriptAction = void(__stdcall*)(const uint8_t* pMsg, int32_t nParam);
using PfnCoreDataChanged = int(__fastcall*)(void* pThis, void* edxUnused, uint32_t uMsgId, uint32_t uParam, int32_t nParam, int32_t nReserve);
using PfnChannelMessageArrival = int(__fastcall*)(
    void* pThis,
    void* edxUnused,
    uint32_t nChannelId,
    const char* szSendName,
    const char* pMsgBuff,
    uint32_t nMsgLength,
    uint32_t bSucc,
    uint32_t uPackage);
using PfnMSNMessageArrival = void(__fastcall*)(
    void* pThis,
    void* edxUnused,
    const char* szSourceName,
    const char* szSendName,
    const char* pMsgBuff,
    uint32_t nMsgLength,
    int32_t nFailCode,
    uint32_t dwItemId);
using PfnOpenChannel = void(__fastcall*)(
    void* pThis,
    void* edxUnused,
    const char* szChannelName,
    int32_t nParam,
    uint32_t nChannelId,
    uint32_t bClose,
    uint32_t u18,
    uint32_t cost,
    uint32_t u20);
using PfnFriendInvite = void(__fastcall*)(void* pThis, void* edxUnused, const char* szRoleName);
using PfnAddFriend = void(__fastcall*)(void* pThis, void* edxUnused, const char* szRoleName, uint32_t answer);
using PfnFriendStatus = void(__fastcall*)(void* pThis, void* edxUnused, const char* szRoleName, uint32_t state);
using PfnFriendInfo = void(__fastcall*)(
    void* pThis,
    void* edxUnused,
    const char* szRoleName,
    const char* szUnitName,
    const char* szGroupName,
    uint32_t state);
using PfnAddPeople = void(__fastcall*)(void* pThis, void* edxUnused, const char* szUnitName, const char* szRoleName);
using PfnSendPackToServer = int(__fastcall*)(void* pThis, void* edxUnused, const void* pBuf, uint32_t len);
using PfnIsInBlackName = bool(__cdecl*)(const char* szName);

PfnGameLoop g_OrigGameLoop = reinterpret_cast<PfnGameLoop>(jx::FN_KMYAPP_GAMELOOP);
PfnProcessNetMsg g_OrigProcessNetMsg = reinterpret_cast<PfnProcessNetMsg>(jx::FN_PROCESS_NET_MSG);
PfnS2CChatGetSomeone g_OrigS2CChatGetSomeone = reinterpret_cast<PfnS2CChatGetSomeone>(jx::FN_S2C_CHAT_GET_SOMEONE);
PfnS2CScriptAction g_OrigS2CScriptAction = reinterpret_cast<PfnS2CScriptAction>(jx::FN_S2C_SCRIPT_ACTION_HANDLER);
PfnCoreDataChanged g_OrigCoreDataChanged = nullptr;
PfnChannelMessageArrival g_OrigChannelMessageArrival = nullptr;
PfnMSNMessageArrival g_OrigMSNMessageArrival = nullptr;
PfnOpenChannel g_OrigOpenChannel = nullptr;
PfnFriendInvite g_OrigFriendInvite = nullptr;
PfnAddFriend g_OrigAddFriend = nullptr;
PfnFriendStatus g_OrigFriendStatus = nullptr;
PfnFriendInfo g_OrigFriendInfo = nullptr;
PfnAddPeople g_OrigAddPeople = nullptr;
PfnSendPackToServer g_OrigSendPackToServer = nullptr;

// Main thread queued RPC items
struct QueuedCommand {
    std::function<std::string()> fn;
    std::string result;
    std::string error;
    HANDLE doneEvent = nullptr;
};

std::mutex g_CmdMutex;
std::deque<QueuedCommand*> g_CmdQueue;

const char* RewriteSingleInstanceName(const char* name, char* buf, size_t cap) {
    if (!name) return nullptr;
    if (std::strstr(name, "24C3984E") != nullptr || std::strstr(name, "ApolloDetourSignature") != nullptr) {
        std::snprintf(buf, cap, "%s_%lu", name, GetCurrentProcessId());
        return buf;
    }
    return name;
}

bool WriteProtectedPtr(void* address, void* newVal, void** outOrig) {
    DWORD oldProt = 0;
    if (!VirtualProtect(address, sizeof(void*), PAGE_EXECUTE_READWRITE, &oldProt)) {
        return false;
    }
    void* current = *reinterpret_cast<void**>(address);
    if (outOrig && *outOrig == nullptr) {
        *outOrig = current;
    }
    *reinterpret_cast<void**>(address) = newVal;
    VirtualProtect(address, sizeof(void*), oldProt, &oldProt);
    return true;
}

template <typename T>
void TryHookIatEntry(IMAGE_THUNK_DATA32* origThunk, IMAGE_THUNK_DATA32* iatThunk, const char* dllName, const char* impName,
                     bool isOrdinal, uint16_t ordinal,
                     const char* targetDll, const char* targetName, uint16_t targetOrdinal,
                     void* hookFn, T* origSlot) {
    if (_stricmp(dllName, targetDll) != 0) return;

    bool match = false;
    if (targetName && !isOrdinal && impName && _stricmp(impName, targetName) == 0) {
        match = true;
    } else if (targetOrdinal != 0 && isOrdinal && ordinal == targetOrdinal) {
        match = true;
    }
    if (!match) return;

    if (iatThunk->u1.Function == reinterpret_cast<DWORD>(hookFn)) return;
    WriteProtectedPtr(&iatThunk->u1.Function, hookFn, reinterpret_cast<void**>(origSlot));
    (void)origThunk;
}

// Forward declarations for hooks
HWND WINAPI HookedCreateWindowExA(DWORD dwExStyle, LPCSTR lpClassName, LPCSTR lpWindowName, DWORD dwStyle,
                                  int X, int Y, int nWidth, int nHeight, HWND hWndParent, HMENU hMenu, HINSTANCE hInstance, LPVOID lpParam);
BOOL WINAPI HookedShowWindow(HWND hWnd, int nCmdShow);
BOOL WINAPI HookedSetWindowPos(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, UINT uFlags);
BOOL WINAPI HookedSetForegroundWindow(HWND hWnd);
HWND WINAPI HookedSetFocus(HWND hWnd);
HWND WINAPI HookedSetCapture(HWND hWnd);
BOOL WINAPI HookedClipCursor(const RECT* lpRect);
int WINAPI HookedShowCursor(BOOL bShow);
BOOL WINAPI HookedWaitMessage();
int WINAPI HookedMessageBoxA(HWND hWnd, LPCSTR lpText, LPCSTR lpCaption, UINT uType);
int WINAPI HookedMessageBoxW(HWND hWnd, LPCWSTR lpText, LPCWSTR lpCaption, UINT uType);
HINSTANCE WINAPI HookedShellExecuteA(HWND hwnd, LPCSTR lpOp, LPCSTR lpFile, LPCSTR lpParams, LPCSTR lpDir, INT nShow);
HINSTANCE WINAPI HookedShellExecuteW(HWND hwnd, LPCWSTR lpOp, LPCWSTR lpFile, LPCWSTR lpParams, LPCWSTR lpDir, INT nShow);
LSTATUS APIENTRY HookedRegCreateKeyExA(HKEY hKey, LPCSTR lpSubKey, DWORD Reserved, LPSTR lpClass, DWORD dwOptions,
                                       REGSAM samDesired, const LPSECURITY_ATTRIBUTES lpSecurityAttributes, PHKEY phkResult, LPDWORD lpdwDisposition);
HRESULT WINAPI HookedDirectSoundCreate(LPCGUID pcGuidDevice, void** ppDS, void* pUnkOuter);
HANDLE WINAPI HookedCreateSemaphoreA(LPSECURITY_ATTRIBUTES lpAttr, LONG lInitialCount, LONG lMaximumCount, LPCSTR lpName);
HANDLE WINAPI HookedOpenSemaphoreA(DWORD dwDesiredAccess, BOOL bInheritHandle, LPCSTR lpName);
HANDLE WINAPI HookedCreateMutexA(LPSECURITY_ATTRIBUTES lpAttr, BOOL bInitialOwner, LPCSTR lpName);
HANDLE WINAPI HookedOpenMutexA(DWORD dwDesiredAccess, BOOL bInheritHandle, LPCSTR lpName);
LPTOP_LEVEL_EXCEPTION_FILTER WINAPI HookedSetUnhandledExceptionFilter(LPTOP_LEVEL_EXCEPTION_FILTER lpTopLevelExceptionFilter);

void PatchModuleIat(HMODULE hMod) {
    if (!hMod) return;
    auto* base = reinterpret_cast<uint8_t*>(hMod);
    auto* dos = reinterpret_cast<IMAGE_DOS_HEADER*>(base);
    if (dos->e_magic != IMAGE_DOS_SIGNATURE) return;
    auto* nt = reinterpret_cast<IMAGE_NT_HEADERS32*>(base + dos->e_lfanew);
    if (nt->Signature != IMAGE_NT_SIGNATURE) return;

    const auto& dir = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (dir.VirtualAddress == 0 || dir.Size == 0) return;

    auto* impDesc = reinterpret_cast<IMAGE_IMPORT_DESCRIPTOR*>(base + dir.VirtualAddress);
    for (; impDesc->Name != 0; ++impDesc) {
        const char* dllName = reinterpret_cast<const char*>(base + impDesc->Name);
        DWORD oft = impDesc->OriginalFirstThunk ? impDesc->OriginalFirstThunk : impDesc->FirstThunk;
        auto* origThunk = reinterpret_cast<IMAGE_THUNK_DATA32*>(base + oft);
        auto* iatThunk = reinterpret_cast<IMAGE_THUNK_DATA32*>(base + impDesc->FirstThunk);

        for (; iatThunk->u1.Function != 0; ++origThunk, ++iatThunk) {
            bool isOrd = IMAGE_SNAP_BY_ORDINAL32(origThunk->u1.Ordinal);
            uint16_t ord = isOrd ? static_cast<uint16_t>(IMAGE_ORDINAL32(origThunk->u1.Ordinal)) : 0;
            const char* impName = nullptr;
            if (!isOrd && origThunk->u1.AddressOfData != 0) {
                auto* ibn = reinterpret_cast<IMAGE_IMPORT_BY_NAME*>(base + origThunk->u1.AddressOfData);
                impName = ibn->Name;
            }

            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "USER32.dll", "CreateWindowExA", 0, HookedCreateWindowExA, &g_OrigCreateWindowExA);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "USER32.dll", "ShowWindow", 0, HookedShowWindow, &g_OrigShowWindow);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "USER32.dll", "SetWindowPos", 0, HookedSetWindowPos, &g_OrigSetWindowPos);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "USER32.dll", "SetForegroundWindow", 0, HookedSetForegroundWindow, &g_OrigSetForegroundWindow);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "USER32.dll", "SetFocus", 0, HookedSetFocus, &g_OrigSetFocus);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "USER32.dll", "SetCapture", 0, HookedSetCapture, &g_OrigSetCapture);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "USER32.dll", "ClipCursor", 0, HookedClipCursor, &g_OrigClipCursor);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "USER32.dll", "ShowCursor", 0, HookedShowCursor, &g_OrigShowCursor);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "USER32.dll", "WaitMessage", 0, HookedWaitMessage, &g_OrigWaitMessage);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "USER32.dll", "MessageBoxA", 0, HookedMessageBoxA, &g_OrigMessageBoxA);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "USER32.dll", "MessageBoxW", 0, HookedMessageBoxW, &g_OrigMessageBoxW);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "SHELL32.dll", "ShellExecuteA", 0, HookedShellExecuteA, &g_OrigShellExecuteA);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "SHELL32.dll", "ShellExecuteW", 0, HookedShellExecuteW, &g_OrigShellExecuteW);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "ADVAPI32.dll", "RegCreateKeyExA", 0, HookedRegCreateKeyExA, &g_OrigRegCreateKeyExA);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "DSOUND.dll", "DirectSoundCreate", 1, HookedDirectSoundCreate, &g_OrigDirectSoundCreate);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "KERNEL32.dll", "CreateSemaphoreA", 0, HookedCreateSemaphoreA, &g_OrigCreateSemaphoreA);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "KERNEL32.dll", "OpenSemaphoreA", 0, HookedOpenSemaphoreA, &g_OrigOpenSemaphoreA);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "KERNEL32.dll", "CreateMutexA", 0, HookedCreateMutexA, &g_OrigCreateMutexA);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "KERNEL32.dll", "OpenMutexA", 0, HookedOpenMutexA, &g_OrigOpenMutexA);
            TryHookIatEntry(origThunk, iatThunk, dllName, impName, isOrd, ord, "KERNEL32.dll", "SetUnhandledExceptionFilter", 0, HookedSetUnhandledExceptionFilter, &g_OrigSetUnhandledExceptionFilter);
        }
    }
}

void PatchAllModules() {
    PatchModuleIat(GetModuleHandleA(nullptr));
    PatchModuleIat(GetModuleHandleA("engine.dll"));
}

HWND WINAPI HookedCreateWindowExA(DWORD dwExStyle, LPCSTR lpClassName, LPCSTR lpWindowName, DWORD dwStyle,
                                  int X, int Y, int nWidth, int nHeight, HWND hWndParent, HMENU hMenu, HINSTANCE hInstance, LPVOID lpParam) {
    InstallVehCrashHandler();
    StartPipeServerOnce();
    PatchAllModules();

    if (HMODULE hImm = GetModuleHandleA("imm32.dll")) {
        using PfnImmDisableIME = BOOL(WINAPI*)(DWORD);
        if (auto fnImmDisable = reinterpret_cast<PfnImmDisableIME>(GetProcAddress(hImm, "ImmDisableIME"))) {
            fnImmDisable(0xFFFFFFFFu);
        }
    }

    PfnCreateWindowExA orig = g_OrigCreateWindowExA ? g_OrigCreateWindowExA : ::CreateWindowExA;
    HWND hwnd = nullptr;
    if (GetGlobals().interactiveWindowEnabled.load(std::memory_order_acquire)) {
        hwnd = orig(dwExStyle, lpClassName, lpWindowName, dwStyle, X, Y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
    } else {
        DWORD hiddenStyle = (dwStyle & ~WS_VISIBLE) | WS_POPUP;
        DWORD hiddenExStyle = (dwExStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;
        hwnd = orig(hiddenExStyle, lpClassName, lpWindowName, hiddenStyle, -32000, -32000, 1, 1, hWndParent, hMenu, hInstance, lpParam);
    }
    if (hwnd && !GetGlobals().mainHwnd) {
        GetGlobals().mainHwnd = hwnd;
    }
    return hwnd;
}

BOOL WINAPI HookedShowWindow(HWND hWnd, int nCmdShow) {
    if (GetGlobals().interactiveWindowEnabled.load(std::memory_order_acquire)) {
        PfnShowWindow orig = g_OrigShowWindow ? g_OrigShowWindow : ::ShowWindow;
        return orig(hWnd, nCmdShow);
    }
    return TRUE;
}

BOOL WINAPI HookedSetWindowPos(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, UINT uFlags) {
    if (GetGlobals().interactiveWindowEnabled.load(std::memory_order_acquire)) {
        PfnSetWindowPos orig = g_OrigSetWindowPos ? g_OrigSetWindowPos : ::SetWindowPos;
        return orig(hWnd, hWndInsertAfter, X, Y, cx, cy, uFlags);
    }
    return TRUE;
}

BOOL WINAPI HookedSetForegroundWindow(HWND hWnd) {
    if (GetGlobals().interactiveWindowEnabled.load(std::memory_order_acquire)) {
        PfnSetForegroundWindow orig = g_OrigSetForegroundWindow ? g_OrigSetForegroundWindow : ::SetForegroundWindow;
        return orig(hWnd);
    }
    return TRUE;
}

HWND WINAPI HookedSetFocus(HWND hWnd) {
    if (GetGlobals().interactiveWindowEnabled.load(std::memory_order_acquire)) {
        PfnSetFocus orig = g_OrigSetFocus ? g_OrigSetFocus : ::SetFocus;
        return orig(hWnd);
    }
    return hWnd;
}

HWND WINAPI HookedSetCapture(HWND hWnd) {
    if (GetGlobals().interactiveWindowEnabled.load(std::memory_order_acquire)) {
        PfnSetCapture orig = g_OrigSetCapture ? g_OrigSetCapture : ::SetCapture;
        return orig(hWnd);
    }
    return nullptr;
}

BOOL WINAPI HookedClipCursor(const RECT* lpRect) { (void)lpRect; return TRUE; }

int WINAPI HookedShowCursor(BOOL bShow) {
    if (GetGlobals().interactiveWindowEnabled.load(std::memory_order_acquire)) {
        PfnShowCursor orig = g_OrigShowCursor ? g_OrigShowCursor : ::ShowCursor;
        return orig(bShow);
    }
    return 0;
}

BOOL WINAPI HookedWaitMessage() {
    Sleep(1);
    return TRUE;
}

int WINAPI HookedMessageBoxA(HWND hWnd, LPCSTR lpText, LPCSTR lpCaption, UINT uType) {
    (void)hWnd; (void)uType;
    auto& g = GetGlobals();
    std::lock_guard<std::mutex> lk(g.uiMutex);
    std::string entry = std::string(lpCaption ? lpCaption : "") + ": " + (lpText ? lpText : "");
    if (g.messageBoxes.size() >= 16) {
        g.messageBoxes.erase(g.messageBoxes.begin());
    }
    g.messageBoxes.push_back(jxjson::ToHex(reinterpret_cast<const uint8_t*>(entry.data()), entry.size()));
    return IDOK;
}

int WINAPI HookedMessageBoxW(HWND hWnd, LPCWSTR lpText, LPCWSTR lpCaption, UINT uType) {
    (void)hWnd; (void)uType;
    char cap[128] = {};
    char txt[512] = {};
    if (lpCaption) WideCharToMultiByte(CP_UTF8, 0, lpCaption, -1, cap, sizeof(cap), nullptr, nullptr);
    if (lpText) WideCharToMultiByte(CP_UTF8, 0, lpText, -1, txt, sizeof(txt), nullptr, nullptr);
    return HookedMessageBoxA(nullptr, txt, cap, uType);
}

HINSTANCE WINAPI HookedShellExecuteA(HWND, LPCSTR, LPCSTR, LPCSTR, LPCSTR, INT) {
    return reinterpret_cast<HINSTANCE>(33);
}

HINSTANCE WINAPI HookedShellExecuteW(HWND, LPCWSTR, LPCWSTR, LPCWSTR, LPCWSTR, INT) {
    return reinterpret_cast<HINSTANCE>(33);
}

LSTATUS APIENTRY HookedRegCreateKeyExA(HKEY hKey, LPCSTR lpSubKey, DWORD Reserved, LPSTR lpClass, DWORD dwOptions,
                                       REGSAM samDesired, const LPSECURITY_ATTRIBUTES lpSecurityAttributes, PHKEY phkResult, LPDWORD lpdwDisposition) {
    if (lpSubKey && _strnicmp(lpSubKey, "jxr_file", 8) == 0) {
        if (phkResult) *phkResult = nullptr;
        return ERROR_ACCESS_DENIED;
    }
    PfnRegCreateKeyExA orig = g_OrigRegCreateKeyExA ? g_OrigRegCreateKeyExA : ::RegCreateKeyExA;
    return orig(hKey, lpSubKey, Reserved, lpClass, dwOptions, samDesired, lpSecurityAttributes, phkResult, lpdwDisposition);
}

HRESULT WINAPI HookedDirectSoundCreate(LPCGUID, void** ppDS, void*) {
    if (ppDS) *ppDS = nullptr;
    return static_cast<HRESULT>(0x88780078); // DSERR_NODRIVER
}

HANDLE WINAPI HookedCreateSemaphoreA(LPSECURITY_ATTRIBUTES lpAttr, LONG lInit, LONG lMax, LPCSTR lpName) {
    char buf[256];
    const char* mapped = RewriteSingleInstanceName(lpName, buf, sizeof(buf));
    PfnCreateSemaphoreA orig = g_OrigCreateSemaphoreA ? g_OrigCreateSemaphoreA : ::CreateSemaphoreA;
    return orig(lpAttr, lInit, lMax, mapped);
}

HANDLE WINAPI HookedOpenSemaphoreA(DWORD dwAccess, BOOL bInherit, LPCSTR lpName) {
    char buf[256];
    const char* mapped = RewriteSingleInstanceName(lpName, buf, sizeof(buf));
    PfnOpenSemaphoreA orig = g_OrigOpenSemaphoreA ? g_OrigOpenSemaphoreA : ::OpenSemaphoreA;
    return orig(dwAccess, bInherit, mapped);
}

HANDLE WINAPI HookedCreateMutexA(LPSECURITY_ATTRIBUTES lpAttr, BOOL bInitOwner, LPCSTR lpName) {
    char buf[256];
    const char* mapped = RewriteSingleInstanceName(lpName, buf, sizeof(buf));
    PfnCreateMutexA orig = g_OrigCreateMutexA ? g_OrigCreateMutexA : ::CreateMutexA;
    return orig(lpAttr, bInitOwner, mapped);
}

HANDLE WINAPI HookedOpenMutexA(DWORD dwAccess, BOOL bInherit, LPCSTR lpName) {
    char buf[256];
    const char* mapped = RewriteSingleInstanceName(lpName, buf, sizeof(buf));
    PfnOpenMutexA orig = g_OrigOpenMutexA ? g_OrigOpenMutexA : ::OpenMutexA;
    return orig(dwAccess, bInherit, mapped);
}

LPTOP_LEVEL_EXCEPTION_FILTER WINAPI HookedSetUnhandledExceptionFilter(LPTOP_LEVEL_EXCEPTION_FILTER lpTopLevelExceptionFilter) {
    (void)lpTopLevelExceptionFilter;
    InstallVehCrashHandler();
    return nullptr;
}

static uint32_t g_CurrentNetChatChannel = 0;

__declspec(noinline) bool SafeIsInBlackName(const char* szName) {
    if (!szName || szName[0] == '\0') return false;
    __try {
        auto fn = reinterpret_cast<PfnIsInBlackName>(jx::FN_BLACKLIST_CONTAINS);
        return fn(szName);
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        return false;
    }
}

void UpsertSocialRelationUnlocked(
    AgentGlobals& g,
    const std::string& nameHex,
    const std::string& unitHex,
    const std::string& groupHex,
    uint8_t state,
    bool isAssociate) {
    if (nameHex.empty()) return;
    for (auto& rec : g.socialRelations) {
        if (rec.nameHex == nameHex && rec.isAssociate == isAssociate) {
            if (!unitHex.empty()) rec.unitHex = unitHex;
            if (!groupHex.empty()) rec.groupHex = groupHex;
            rec.state = state;
            return;
        }
    }
    if (g.socialRelations.size() >= 128) {
        g.socialRelations.erase(g.socialRelations.begin());
    }
    g.socialRelations.push_back(SocialRelationRecord{nameHex, unitHex, groupHex, state, isAssociate});
}

void RemoveSocialRelationUnlocked(AgentGlobals& g, const std::string& nameHex) {
    for (auto it = g.socialRelations.begin(); it != g.socialRelations.end();) {
        if (it->nameHex == nameHex) {
            it = g.socialRelations.erase(it);
        } else {
            ++it;
        }
    }
}

std::vector<uint8_t> ExtractCleanTcvn3(const uint8_t* buf, size_t maxBytes, bool stopAtNullWhenNoExplicitLen) {
    std::vector<uint8_t> out;
    if (!buf || maxBytes == 0) return out;
    out.reserve(maxBytes);
    size_t i = 0;
    while (i < maxBytes) {
        uint8_t b = buf[i];
        if (b == 0x00) {
            if (stopAtNullWhenNoExplicitLen) break;
            ++i;
            continue;
        }
        if (b == 0x02 || b == 0x04) {
            // TEncodeText inline RGB / border-RGB tag: [0x02/0x04, R, G, B]
            i += (i + 4 <= maxBytes) ? 4 : maxBytes - i;
            continue;
        }
        if (b == 0x03) {
            // TEncodeText end-color tag
            ++i;
            continue;
        }
        out.push_back(b);
        ++i;
    }
    return out;
}

void CaptureInlineDialogFromScriptAction(const uint8_t* pMsg) {
    if (!pMsg || pMsg[0] != 0x63 || pMsg[3] != 0 || pMsg[4] != 0) return;

    int32_t dataLen = *reinterpret_cast<const int32_t*>(pMsg + 0x0c);
    if (dataLen <= 0 || dataLen > 16384) return;

    uint8_t optCount = pMsg[5];
    uint8_t isStrId = pMsg[6];
    if (isStrId != 0) return; // Handled after decoding in CoreDataChanged(0x16)

    const char* base = reinterpret_cast<const char*>(pMsg + 0x10);
    const char* end = base + dataLen;
    size_t promptLen = strnlen(base, static_cast<size_t>(end - base));
    std::string promptHex = jxjson::ToHex(reinterpret_cast<const uint8_t*>(base), promptLen);

    std::vector<std::string> optsHex;
    const char* cur = base + promptLen;
    if (cur < end && *cur == '\0') ++cur;

    if (cur >= end || *cur == '\0') {
        optCount = 0;
    }
    for (uint8_t i = 0; i < optCount && cur < end && *cur != '\0'; ++i) {
        size_t oLen = strnlen(cur, static_cast<size_t>(end - cur));
        optsHex.push_back(jxjson::ToHex(reinterpret_cast<const uint8_t*>(cur), oLen));
        cur += oLen;
        if (cur < end && *cur == '\0') ++cur;
    }

    auto& g = GetGlobals();
    uint32_t ticks = *reinterpret_cast<const uint32_t*>(jx::ADDR_KMYAPP_GAMELOOP_COUNT);
    std::lock_guard<std::mutex> lk(g.uiMutex);
    g.dialog.sequence++;
    g.dialog.openedTick = ticks;
    g.dialog.isOpen = true;
    g.dialog.isTalk = false;
    g.dialog.talkNeedCallback = false;
    g.dialog.promptHex = std::move(promptHex);
    g.dialog.optionsHex = std::move(optsHex);
}

void __fastcall HookedProcessNetMsg(void* pProto, void* edxUnused, const uint8_t* pMsg, size_t len) {
    (void)edxUnused;
    uint32_t tick = *reinterpret_cast<const uint32_t*>(jx::ADDR_KMYAPP_GAMELOOP_COUNT);
    RecordS2cPacket(pMsg, len, tick);
    if (pMsg && len > 0) {
        if (len >= 41 && (pMsg[0] == 0x70 || pMsg[0] == 0x6B)) {
            // Legacy s2c_chatonefrienddata (0x70) / s2c_chataddfriend (0x6B): [1..4]=FriendId, [5..8]=PlayerIdx, [9..40]=Name[32]
            const char* fName = reinterpret_cast<const char*>(pMsg + 9);
            size_t fLen = strnlen(fName, 32);
            if (fLen > 0) {
                auto& g = GetGlobals();
                std::lock_guard<std::mutex> lk(g.uiMutex);
                UpsertSocialRelationUnlocked(
                    g,
                    jxjson::ToHex(reinterpret_cast<const uint8_t*>(fName), fLen),
                    std::string(),
                    std::string(),
                    1u,
                    false);
            }
        } else if (len >= 9 && pMsg[0] == 0x8B) {
            // s2c_tradeapplystart (0x8B, 9B @ jx_linux_y 0x080c43fa / game.exe 0x00570200): [1..4]=inviterPlayerIdx, [5..8]=inviterNpcDwId
            int32_t invIdx = *reinterpret_cast<const int32_t*>(pMsg + 1);
            uint32_t invDwId = *reinterpret_cast<const uint32_t*>(pMsg + 5);
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            if (g.pendingTradeInvites.size() >= 16) g.pendingTradeInvites.erase(g.pendingTradeInvites.begin());
            g.pendingTradeInvites.push_back(PendingTradeInviteRecord{
                g.tradeInviteSeq.fetch_add(1, std::memory_order_acq_rel) + 1,
                tick,
                invIdx,
                invDwId
            });
        } else if (len >= 5 && pMsg[0] == 0x77) {
            // s2c_trademoneysync (0x77, 5B @ jx_linux_y 0x080bfc6b / game.exe 0x00555720): [1..4]=otherMoney
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            g.tradeWire.otherMoney = *reinterpret_cast<const int32_t*>(pMsg + 1);
        } else if (len >= 5 && pMsg[0] == 0x81) {
            // s2c_tradestatesync (0x81, 5B @ jx_linux_y 0x080bfca8): [1]=selfLock, [2]=otherLock, [3]=selfOk, [4]=otherOk
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            g.tradeWire.selfLocked = pMsg[1];
            g.tradeWire.otherLocked = pMsg[2];
            g.tradeWire.selfConfirmed = pMsg[3];
            g.tradeWire.otherConfirmed = pMsg[4];
        } else if (len >= 2 && pMsg[0] == 0x78) {
            // s2c_tradedecision (0x78, 2B @ jx_linux_y 0x080c27fd / game.exe 0x00555740): [1]=0(cancel)/1(success)
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            g.tradeWire.lastDecisionResult = pMsg[1];
            g.tradeWire.decisionSeq++;
            g.tradeWire.selfLocked = 0;
            g.tradeWire.otherLocked = 0;
            g.tradeWire.selfConfirmed = 0;
            g.tradeWire.otherConfirmed = 0;
            g.tradeWire.otherMoney = 0;
        } else if (len >= 12 && pMsg[0] == 0x9D && pMsg[3] == 1) {
            // s2c_stallextend sub=1 (AdvEcho): [4..7]=sellerDwId, [8..11]=advLen, [12..]=advBytes
            int32_t advLen = *reinterpret_cast<const int32_t*>(pMsg + 8);
            if (advLen >= 0 && static_cast<size_t>(12 + advLen) <= len) {
                auto& g = GetGlobals();
                std::lock_guard<std::mutex> lk(g.uiMutex);
                g.lastViewedStallAdvHex = jxjson::ToHex(pMsg + 12, static_cast<size_t>(advLen));
            }
        } else if (len == 7 && pMsg[0] == 0xA0) {
            // s2c_foundry_result / Enchaser result (0xA0, 7B @ jx_linux_y 0x080f3587 / 0x080f5c7f): [1]=action, [2..5]=itemDwId, [6]=result
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            g.lastCrafting = CraftingResultRecord{
                g.craftingSeq.fetch_add(1, std::memory_order_acq_rel) + 1,
                pMsg[1],
                *reinterpret_cast<const uint32_t*>(pMsg + 2),
                pMsg[6]
            };
        } else if (len >= 9 && pMsg[0] == 0xFD && pMsg[3] == 1) {
            // s2c_extendtong sub=1 (TransferAddApply @ jx_linux_y 0x080e8659 / game.exe 0x0055c894):
            // [4..7]=int32 applicantPlayerIdx, [8..]=TCVN3 applicantName
            int32_t appIdx = *reinterpret_cast<const int32_t*>(pMsg + 4);
            size_t maxChars = (len - 8) < 32 ? (len - 8) : 32;
            size_t nameLen = strnlen(reinterpret_cast<const char*>(pMsg + 8), maxChars);
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            g.lastTongApplicantPlayerIdx = appIdx;
            g.lastTongApplicantNameHex = nameLen > 0 ? jxjson::ToHex(pMsg + 8, nameLen) : std::string();
        } else if (len >= 43 && pMsg[0] == 0xFA && pMsg[3] == 0x04 && pMsg[4] == 0x22 &&
                   pMsg[5] == '\\' && (pMsg[6] == 'O' || pMsg[6] == 'o')) {
            // s3relay_y CChannelMgr::QueryChannelID @ 0x08052bdb..0x08052c21 (43B CHANNEL_RETURNCHANNELID):
            // [5..36]=channel[32], [37..40]=uint32 channelid, [41]=uint8 cost, [42]=uint8 someflag (validated by IsValidChatMsg @ 0x0804cc64)
            uint32_t retChId = *reinterpret_cast<const uint32_t*>(pMsg + 37);
            auto& g = GetGlobals();
            std::lock_guard<std::mutex> lk(g.uiMutex);
            g.tongChannelId = retChId;
            g.tongChannelCost = pMsg[41];
            g.tongChannelSomeFlag = pMsg[42];
        }
    }
    uint32_t prevChatChannel = g_CurrentNetChatChannel;
    if (pMsg && len >= 12 && pMsg[0] == 0x7E) {
        g_CurrentNetChatChannel = pMsg[3];
    }
    g_OrigProcessNetMsg(pProto, nullptr, pMsg, len);
    g_CurrentNetChatChannel = prevChatChannel;
}

int __fastcall HookedSendPackToServer(void* pThis, void* edxUnused, const void* pBuf, uint32_t len) {
    (void)edxUnused;
    if (pThis != nullptr && pBuf != nullptr && len > 0 &&
        pThis == *reinterpret_cast<void**>(jx::ADDR_GAMESVR_ICLIENT)) {
        uint32_t tick = *reinterpret_cast<const uint32_t*>(jx::ADDR_KMYAPP_GAMELOOP_COUNT);
        RecordC2sPacket(static_cast<const uint8_t*>(pBuf), len, tick);
    }
    return g_OrigSendPackToServer ? g_OrigSendPackToServer(pThis, nullptr, pBuf, len) : 0;
}

void __fastcall HookedS2CChatGetSomeone(void* pProto, void* edxUnused, const uint8_t* pMsg, int32_t nParam) {
    (void)edxUnused;
    uint32_t prevChatChannel = g_CurrentNetChatChannel;
    if (pMsg && pMsg[0] == 0x7E) {
        g_CurrentNetChatChannel = pMsg[3];
    }
    g_OrigS2CChatGetSomeone(pProto, nullptr, pMsg, nParam);
    g_CurrentNetChatChannel = prevChatChannel;
}

void __stdcall HookedS2CScriptAction(const uint8_t* pMsg, int32_t nParam) {
    CaptureInlineDialogFromScriptAction(pMsg);
    g_OrigS2CScriptAction(pMsg, nParam);
}

int __fastcall HookedCoreDataChanged(void* pThis, void* edxUnused, uint32_t uMsgId, uint32_t uParam, int32_t nParam, int32_t nReserve) {
    (void)edxUnused;
    auto& g = GetGlobals();
    uint32_t tick = *reinterpret_cast<const uint32_t*>(jx::ADDR_KMYAPP_GAMELOOP_COUNT);

    if (uMsgId == 0x16 && uParam != 0) {
        // KUiQuestionAndAnswerInfo (Say) layout created in 0x0056d450:
        // +0x000: char prompt[0x200], +0x200: int promptLen, +0x204: int optCount, +0x208 + i*0x104: option string
        const uint8_t* info = reinterpret_cast<const uint8_t*>(uParam);
        std::lock_guard<std::mutex> lk(g.uiMutex);
        if (g.dialog.openedTick != tick || g.dialog.promptHex.empty() || g.dialog.isTalk) {
            int32_t rawPromptLen = *reinterpret_cast<const int32_t*>(info + 0x200);
            bool hasExplicitLen = (rawPromptLen > 0 && rawPromptLen <= 0x200);
            size_t pMax = hasExplicitLen ? static_cast<size_t>(rawPromptLen) : 0x200;
            std::vector<uint8_t> cleanPrompt = ExtractCleanTcvn3(info, pMax, !hasExplicitLen);
            int32_t optCount = *reinterpret_cast<const int32_t*>(info + 0x204);
            if (optCount < 0) optCount = 0;
            if (optCount > 32) optCount = 32;
            g.dialog.sequence++;
            g.dialog.openedTick = tick;
            g.dialog.isOpen = true;
            g.dialog.isTalk = false;
            g.dialog.talkNeedCallback = false;
            g.dialog.promptHex = jxjson::ToHex(cleanPrompt.data(), cleanPrompt.size());
            g.dialog.optionsHex.clear();
            for (int32_t i = 0; i < optCount; ++i) {
                const uint8_t* optPtr = info + 0x208 + static_cast<size_t>(i) * 0x104;
                std::vector<uint8_t> cleanOpt = ExtractCleanTcvn3(optPtr, 0x100, true);
                g.dialog.optionsHex.push_back(jxjson::ToHex(cleanOpt.data(), cleanOpt.size()));
            }
        }
    } else if (uMsgId == 0x40 && uParam != 0) {
        // Describe / CreateNewSayEx layout created in 0x0056d780:
        // +0x000: char prompt[0x400], +0x400: int promptLen, +0x404: int optCount, +0x40c + i*0x104: option string
        const uint8_t* info = reinterpret_cast<const uint8_t*>(uParam);
        int32_t rawPromptLen = *reinterpret_cast<const int32_t*>(info + 0x400);
        bool hasExplicitLen = (rawPromptLen > 0 && rawPromptLen <= 0x400);
        size_t pMax = hasExplicitLen ? static_cast<size_t>(rawPromptLen) : 0x400;
        std::vector<uint8_t> cleanPrompt = ExtractCleanTcvn3(info, pMax, !hasExplicitLen);
        int32_t optCount = *reinterpret_cast<const int32_t*>(info + 0x404);
        if (optCount < 0) optCount = 0;
        if (optCount > 32) optCount = 32;

        std::lock_guard<std::mutex> lk(g.uiMutex);
        g.dialog.sequence++;
        g.dialog.openedTick = tick;
        g.dialog.isOpen = true;
        g.dialog.isTalk = false;
        g.dialog.talkNeedCallback = false;
        g.dialog.promptHex = jxjson::ToHex(cleanPrompt.data(), cleanPrompt.size());
        g.dialog.optionsHex.clear();
        for (int32_t i = 0; i < optCount; ++i) {
            const uint8_t* optPtr = info + 0x40c + static_cast<size_t>(i) * 0x104;
            std::vector<uint8_t> cleanOpt = ExtractCleanTcvn3(optPtr, 0x100, true);
            g.dialog.optionsHex.push_back(jxjson::ToHex(cleanOpt.data(), cleanOpt.size()));
        }
    } else if (uMsgId == 0x17 && uParam != 0 && nParam > 0) {
        // Talk pages array created in 0x0056dc60:
        // nParam pages of stride 0x144: +0x000 text[0x100], +0x100 speaker[0x40], +0x140 uint16_t textLen, +0x142 uint8_t bNeedCallback
        const uint8_t* pages = reinterpret_cast<const uint8_t*>(uParam);
        int32_t pageCount = nParam > 32 ? 32 : nParam;
        std::vector<uint8_t> combined;
        bool pageNeedCb = false;
        for (int32_t i = 0; i < pageCount; ++i) {
            const uint8_t* page = pages + static_cast<size_t>(i) * 0x144;
            uint16_t wLen = *reinterpret_cast<const uint16_t*>(page + 0x140);
            bool hasLen = (wLen > 0 && wLen <= 0x100);
            std::vector<uint8_t> cleanPage = ExtractCleanTcvn3(page, hasLen ? wLen : 0x100, !hasLen);
            if (i > 0 && !combined.empty() && !cleanPage.empty()) {
                combined.push_back(static_cast<uint8_t>('\n'));
            }
            for (size_t j = 0; j < cleanPage.size(); ++j) {
                combined.push_back(cleanPage[j]);
            }
            if (page[0x142] != 0) pageNeedCb = true;
        }
        bool globalNeedCb = (*reinterpret_cast<const int32_t*>(jx::ADDR_UI_TALK_CALLBACK_FLAG) != 0);

        std::lock_guard<std::mutex> lk(g.uiMutex);
        g.dialog.sequence++;
        g.dialog.openedTick = tick;
        g.dialog.isOpen = true;
        g.dialog.isTalk = true;
        g.dialog.talkNeedCallback = globalNeedCb || pageNeedCb;
        g.dialog.promptHex = jxjson::ToHex(combined.data(), combined.size());
        g.dialog.optionsHex.clear();
    } else if (uMsgId == 0x19 && uParam != 0 && nParam != 0) {
        // GDCNI_MSG_ARRIVAL (0x7E chat channels 1,2,4,5,6,7,8; 0x005b592b..0x005b5c44):
        // uParam = const char* szMsgBuff, nParam = const KUiMsgParam* (+0x02 uint16_t nMsgLength, +0x04 char szSender[32])
        const uint8_t* msgBytes = reinterpret_cast<const uint8_t*>(uParam);
        const uint8_t* param = reinterpret_cast<const uint8_t*>(nParam);
        uint16_t msgLen = *reinterpret_cast<const uint16_t*>(param + 0x02);
        size_t maxLen = (msgLen > 0 && msgLen <= 512) ? static_cast<size_t>(msgLen) : 256;
        bool hasExplicitLen = (msgLen > 0 && msgLen <= 512);
        std::vector<uint8_t> cleanMsg = ExtractCleanTcvn3(msgBytes, maxLen, !hasExplicitLen);
        if (!cleanMsg.empty()) {
            const char* senderPtr = reinterpret_cast<const char*>(param + 0x04);
            size_t senderLen = strnlen(senderPtr, 32);
            char senderBuf[33] = {};
            if (senderLen > 0) std::memcpy(senderBuf, senderPtr, senderLen);
            if (senderLen == 0 || !SafeIsInBlackName(senderBuf)) {
                uint32_t chId = g_CurrentNetChatChannel != 0 ? g_CurrentNetChatChannel : 2u;
                std::lock_guard<std::mutex> lk(g.uiMutex);
                if (g.chatMessages.size() >= 64) {
                    g.chatMessages.erase(g.chatMessages.begin());
                }
                g.chatMessages.push_back(ChatMessageRecord{
                    g.chatMsgSeq.fetch_add(1, std::memory_order_acq_rel) + 1,
                    tick,
                    chId,
                    senderLen > 0 ? jxjson::ToHex(reinterpret_cast<const uint8_t*>(senderBuf), senderLen) : std::string(),
                    jxjson::ToHex(cleanMsg.data(), cleanMsg.size())
                });
            }
        }
    } else if (uMsgId == 0x1e && uParam != 0 && nParam != 0) {
        // GDCNI_CHAT_MESSAGE (0x7E Whisper channel 3):
        // uParam = +0x04 int16_t sentenceLen, +0x06 char szSentence[256]; nParam = const char* szSenderName
        const uint8_t* param = reinterpret_cast<const uint8_t*>(uParam);
        int16_t rawLen = *reinterpret_cast<const int16_t*>(param + 0x04);
        bool hasExplicitLen = (rawLen > 0 && rawLen <= 256);
        size_t maxLen = hasExplicitLen ? static_cast<size_t>(rawLen) : 256;
        std::vector<uint8_t> cleanMsg = ExtractCleanTcvn3(param + 0x06, maxLen, !hasExplicitLen);
        if (!cleanMsg.empty()) {
            const char* senderPtr = reinterpret_cast<const char*>(nParam);
            size_t senderLen = strnlen(senderPtr, 32);
            char senderBuf[33] = {};
            if (senderLen > 0) std::memcpy(senderBuf, senderPtr, senderLen);
            if (senderLen == 0 || !SafeIsInBlackName(senderBuf)) {
                std::lock_guard<std::mutex> lk(g.uiMutex);
                if (g.chatMessages.size() >= 64) {
                    g.chatMessages.erase(g.chatMessages.begin());
                }
                g.chatMessages.push_back(ChatMessageRecord{
                    g.chatMsgSeq.fetch_add(1, std::memory_order_acq_rel) + 1,
                    tick,
                    3u,
                    senderLen > 0 ? jxjson::ToHex(reinterpret_cast<const uint8_t*>(senderBuf), senderLen) : std::string(),
                    jxjson::ToHex(cleanMsg.data(), cleanMsg.size())
                });
            }
        }
    } else if (uMsgId == 0x1f && uParam != 0) {
        const char* txt = reinterpret_cast<const char*>(uParam);
        size_t len = strnlen(txt, 512);
        const uint8_t* sysMsgBytes = reinterpret_cast<const uint8_t*>(uParam);
        // KSystemMessage layout (game.exe 0x00406994 / 0x0056fcf7 / 0x00567d3b):
        // +0x000..+0x3FF szMessage[0x400], +0x400 int32_t nMsgLen, +0x404 uint8_t eType, +0x405 uint8_t byConfirmType
        uint8_t confirmType = sysMsgBytes[0x405];
        std::lock_guard<std::mutex> lk(g.uiMutex);
        if (g.systemMessages.size() >= 32) {
            g.systemMessages.erase(g.systemMessages.begin());
        }
        g.systemMessages.push_back(SystemMessageRecord{
            g.sysMsgSeq.fetch_add(1, std::memory_order_acq_rel) + 1,
            tick,
            jxjson::ToHex(reinterpret_cast<const uint8_t*>(txt), len)
        });
        if (nParam != 0) {
            const uint8_t* itemBytes = reinterpret_cast<const uint8_t*>(nParam);
            size_t itemNameLen = strnlen(reinterpret_cast<const char*>(itemBytes), 32);
            std::string itemNameHex = itemNameLen > 0 ? jxjson::ToHex(itemBytes, itemNameLen) : std::string();
            if (confirmType == 7) {
                // SMCT_UI_TEAM_INVITE (0x0056fcf7): item+0x44 = InviterPlayerIdx
                int32_t inviterIdx = *reinterpret_cast<const int32_t*>(itemBytes + 0x44);
                if (g.pendingTeamInvites.size() >= 16) {
                    g.pendingTeamInvites.erase(g.pendingTeamInvites.begin());
                }
                g.pendingTeamInvites.push_back(PendingTeamInviteRecord{
                    g.teamInviteSeq.fetch_add(1, std::memory_order_acq_rel) + 1,
                    tick,
                    inviterIdx,
                    std::move(itemNameHex)
                });
            } else if (confirmType == 8) {
                // SMCT_UI_TEAM_APPLY (0x00567d3b): item+0x40 = ApplicantNpcDwId
                uint32_t appDwId = *reinterpret_cast<const uint32_t*>(itemBytes + 0x40);
                if (g.pendingTeamApplies.size() >= 16) {
                    g.pendingTeamApplies.erase(g.pendingTeamApplies.begin());
                }
                g.pendingTeamApplies.push_back(PendingTeamApplyRecord{
                    g.teamInviteSeq.fetch_add(1, std::memory_order_acq_rel) + 1,
                    tick,
                    appDwId,
                    std::move(itemNameHex)
                });
            } else if (confirmType == 11 && !itemNameHex.empty()) {
                // SMCT_UI_FRIEND_INVITE (0x00406994)
                bool exists = false;
                for (const auto& h : g.pendingFriendInvitesHex) {
                    if (h == itemNameHex) { exists = true; break; }
                }
                if (!exists) {
                    if (g.pendingFriendInvitesHex.size() >= 32) {
                        g.pendingFriendInvitesHex.erase(g.pendingFriendInvitesHex.begin());
                    }
                    g.pendingFriendInvitesHex.push_back(std::move(itemNameHex));
                }
            }
        }
    }

    return g_OrigCoreDataChanged ? g_OrigCoreDataChanged(pThis, nullptr, uMsgId, uParam, nParam, nReserve) : 0;
}

int __fastcall HookedChannelMessageArrival(
    void* pThis,
    void* edxUnused,
    uint32_t nChannelId,
    const char* szSendName,
    const char* pMsgBuff,
    uint32_t nMsgLength,
    uint32_t bSucc,
    uint32_t uPackage) {
    (void)edxUnused;
    if (pMsgBuff != nullptr && (bSucc & 0xFFu) != 0) {
        size_t senderLen = szSendName ? strnlen(szSendName, 64) : 0;
        char senderBuf[65] = {};
        if (senderLen > 0) std::memcpy(senderBuf, szSendName, senderLen);
        if (senderLen == 0 || !SafeIsInBlackName(senderBuf)) {
            size_t rawLen = static_cast<size_t>(nMsgLength & 0xFFFFu);
            if (rawLen > 512) rawLen = 512;
            const uint8_t* rawPtr = reinterpret_cast<const uint8_t*>(pMsgBuff);
            if (rawLen >= 2 && rawPtr[0] == 0x1b) {
                rawPtr += 2;
                rawLen -= 2;
            }
            std::vector<uint8_t> clean = ExtractCleanTcvn3(rawPtr, rawLen, true);
            if (!clean.empty()) {
                auto& g = GetGlobals();
                uint32_t tick = *reinterpret_cast<const uint32_t*>(jx::ADDR_KMYAPP_GAMELOOP_COUNT);
                std::string textHex = jxjson::ToHex(clean.data(), clean.size());
                std::string senderHex = senderLen > 0
                    ? jxjson::ToHex(reinterpret_cast<const uint8_t*>(senderBuf), senderLen)
                    : std::string();
                std::lock_guard<std::mutex> lk(g.uiMutex);
                if (g.chatMessages.size() >= 64) {
                    g.chatMessages.erase(g.chatMessages.begin());
                }
                g.chatMessages.push_back(ChatMessageRecord{
                    g.chatMsgSeq.fetch_add(1, std::memory_order_acq_rel) + 1,
                    tick,
                    nChannelId,
                    std::move(senderHex),
                    textHex
                });
                if (nChannelId == 0xFFFFFFFFu) {
                    if (g.systemMessages.size() >= 32) {
                        g.systemMessages.erase(g.systemMessages.begin());
                    }
                    g.systemMessages.push_back(SystemMessageRecord{
                        g.sysMsgSeq.fetch_add(1, std::memory_order_acq_rel) + 1,
                        tick,
                        std::move(textHex)
                    });
                }
            }
        }
    }
    return g_OrigChannelMessageArrival
        ? g_OrigChannelMessageArrival(pThis, nullptr, nChannelId, szSendName, pMsgBuff, nMsgLength, bSucc, uPackage)
        : 0;
}

void __fastcall HookedMSNMessageArrival(
    void* pThis,
    void* edxUnused,
    const char* szSourceName,
    const char* szSendName,
    const char* pMsgBuff,
    uint32_t nMsgLength,
    int32_t nFailCode,
    uint32_t dwItemId) {
    (void)edxUnused;
    if (nFailCode == 0 && pMsgBuff != nullptr) {
        size_t senderLen = szSourceName ? strnlen(szSourceName, 64) : 0;
        char senderBuf[65] = {};
        if (senderLen > 0) std::memcpy(senderBuf, szSourceName, senderLen);
        if (senderLen == 0 || !SafeIsInBlackName(senderBuf)) {
            size_t rawLen = static_cast<size_t>(nMsgLength & 0xFFFFu);
            if (rawLen > 512) rawLen = 512;
            const uint8_t* rawPtr = reinterpret_cast<const uint8_t*>(pMsgBuff);
            if (rawLen >= 2 && rawPtr[0] == 0x1b) {
                size_t skip = ((rawPtr[1] & 0x04u) != 0 && rawLen >= 6) ? 6 : 2;
                rawPtr += skip;
                rawLen -= skip;
            }
            std::vector<uint8_t> clean = ExtractCleanTcvn3(rawPtr, rawLen, true);
            if (!clean.empty()) {
                auto& g = GetGlobals();
                uint32_t tick = *reinterpret_cast<const uint32_t*>(jx::ADDR_KMYAPP_GAMELOOP_COUNT);
                std::lock_guard<std::mutex> lk(g.uiMutex);
                if (g.chatMessages.size() >= 64) {
                    g.chatMessages.erase(g.chatMessages.begin());
                }
                g.chatMessages.push_back(ChatMessageRecord{
                    g.chatMsgSeq.fetch_add(1, std::memory_order_acq_rel) + 1,
                    tick,
                    3u,
                    senderLen > 0 ? jxjson::ToHex(reinterpret_cast<const uint8_t*>(senderBuf), senderLen) : std::string(),
                    jxjson::ToHex(clean.data(), clean.size())
                });
            }
        }
    }
    if (g_OrigMSNMessageArrival) {
        g_OrigMSNMessageArrival(pThis, nullptr, szSourceName, szSendName, pMsgBuff, nMsgLength, nFailCode, dwItemId);
    }
}

void __fastcall HookedOpenChannel(
    void* pThis,
    void* edxUnused,
    const char* szChannelName,
    int32_t nParam,
    uint32_t nChannelId,
    uint32_t bClose,
    uint32_t u18,
    uint32_t cost,
    uint32_t u20) {
    (void)edxUnused;
    uint32_t beforeC2s = GetGlobals().c2sSeq.load(std::memory_order_acquire);
    if (g_OrigOpenChannel) {
        g_OrigOpenChannel(pThis, nullptr, szChannelName, nParam, nChannelId, bClose, u18, cost, u20);
    }
    if (szChannelName != nullptr && szChannelName[0] == '\\' && (szChannelName[1] == 'O' || szChannelName[1] == 'o')) {
        auto& g = GetGlobals();
        bool shouldSub = ((bClose & 0xFFu) == 0 && nChannelId != 0xFFFFFFFFu);
        {
            std::lock_guard<std::mutex> lk(g.uiMutex);
            g.tongChannelId = shouldSub ? nChannelId : 0xFFFFFFFFu;
        }
        // Headless fallback for KUiMsgCentrePad::OpenChannel (0x0048f510 -> SendSubscribe @ 0x00490ff0..0x0049108f):
        // 13-byte [0xFA, uint16 wLen=12, 0x04 (pf_playercommunity), 0x24 (PLAYERCOMM_SUBSCRIBE), uint32 channelId, uint32 bSubscribe=1]
        if (shouldSub && g.c2sSeq.load(std::memory_order_acquire) == beforeC2s) {
            void* pCoreShell = *reinterpret_cast<void**>(jx::ADDR_G_CORE_SHELL);
            if (pCoreShell != nullptr) {
                uint8_t subPkt[13] = {};
                subPkt[0] = 0xFA;
                uint16_t wLen = 12;
                std::memcpy(subPkt + 1, &wLen, sizeof(wLen));
                subPkt[3] = 0x04;
                subPkt[4] = 0x24;
                std::memcpy(subPkt + 5, &nChannelId, sizeof(nChannelId));
                uint32_t bSubscribe = 1u;
                std::memcpy(subPkt + 9, &bSubscribe, sizeof(bSubscribe));
                reinterpret_cast<int(__thiscall*)(void*, const void*, uint32_t)>(jx::FN_SEND_NEW_DATA_TO_CORE)(
                    pCoreShell, subPkt, sizeof(subPkt));
            }
        }
    }
}

void __fastcall HookedFriendInvite(void* pThis, void* edxUnused, const char* szRoleName) {
    (void)edxUnused;
    if (szRoleName != nullptr && szRoleName[0] != '\0') {
        size_t len = strnlen(szRoleName, 32);
        std::string hex = jxjson::ToHex(reinterpret_cast<const uint8_t*>(szRoleName), len);
        auto& g = GetGlobals();
        std::lock_guard<std::mutex> lk(g.uiMutex);
        bool exists = false;
        for (const auto& h : g.pendingFriendInvitesHex) {
            if (h == hex) { exists = true; break; }
        }
        if (!exists) {
            if (g.pendingFriendInvitesHex.size() >= 32) {
                g.pendingFriendInvitesHex.erase(g.pendingFriendInvitesHex.begin());
            }
            g.pendingFriendInvitesHex.push_back(std::move(hex));
        }
    }
    if (g_OrigFriendInvite) {
        g_OrigFriendInvite(pThis, nullptr, szRoleName);
    }
}

void __fastcall HookedAddFriend(void* pThis, void* edxUnused, const char* szRoleName, uint32_t answer) {
    (void)edxUnused;
    if (szRoleName != nullptr && szRoleName[0] != '\0') {
        size_t len = strnlen(szRoleName, 32);
        std::string hex = jxjson::ToHex(reinterpret_cast<const uint8_t*>(szRoleName), len);
        auto& g = GetGlobals();
        std::lock_guard<std::mutex> lk(g.uiMutex);
        for (auto it = g.pendingFriendInvitesHex.begin(); it != g.pendingFriendInvitesHex.end();) {
            if (*it == hex) it = g.pendingFriendInvitesHex.erase(it);
            else ++it;
        }
        // game.exe 0x0055eb95: movzx edx, byte ptr [edi + 0x26] -> 1 == rep_yes (accepted), 0 == rep_no (refused)
        if ((answer & 0xFFu) == 1) {
            for (auto it = g.blacklistNamesHex.begin(); it != g.blacklistNamesHex.end();) {
                if (*it == hex) it = g.blacklistNamesHex.erase(it);
                else ++it;
            }
            UpsertSocialRelationUnlocked(g, hex, std::string(), std::string(), 1u, false);
        }
    }
    if (g_OrigAddFriend) {
        g_OrigAddFriend(pThis, nullptr, szRoleName, answer);
    }
}

void __fastcall HookedFriendStatus(void* pThis, void* edxUnused, const char* szRoleName, uint32_t state) {
    (void)edxUnused;
    if (szRoleName != nullptr && szRoleName[0] != '\0') {
        size_t len = strnlen(szRoleName, 32);
        std::string hex = jxjson::ToHex(reinterpret_cast<const uint8_t*>(szRoleName), len);
        uint8_t st = static_cast<uint8_t>(state & 0xFFu);
        auto& g = GetGlobals();
        std::lock_guard<std::mutex> lk(g.uiMutex);
        if (st == 4 || st == 5) {
            RemoveSocialRelationUnlocked(g, hex);
        } else {
            bool updated = false;
            for (auto& rec : g.socialRelations) {
                if (rec.nameHex == hex) {
                    rec.state = st;
                    updated = true;
                }
            }
            if (!updated && st == 1) {
                UpsertSocialRelationUnlocked(g, hex, std::string(), std::string(), st, false);
            }
        }
    }
    if (g_OrigFriendStatus) {
        g_OrigFriendStatus(pThis, nullptr, szRoleName, state);
    }
}

void __fastcall HookedFriendInfo(
    void* pThis,
    void* edxUnused,
    const char* szRoleName,
    const char* szUnitName,
    const char* szGroupName,
    uint32_t state) {
    (void)edxUnused;
    if (szRoleName != nullptr && szRoleName[0] != '\0') {
        size_t rLen = strnlen(szRoleName, 32);
        size_t uLen = szUnitName ? strnlen(szUnitName, 32) : 0;
        size_t gLen = szGroupName ? strnlen(szGroupName, 32) : 0;
        std::string rHex = jxjson::ToHex(reinterpret_cast<const uint8_t*>(szRoleName), rLen);
        std::string uHex = uLen > 0 ? jxjson::ToHex(reinterpret_cast<const uint8_t*>(szUnitName), uLen) : std::string();
        std::string gHex = gLen > 0 ? jxjson::ToHex(reinterpret_cast<const uint8_t*>(szGroupName), gLen) : std::string();
        auto& g = GetGlobals();
        std::lock_guard<std::mutex> lk(g.uiMutex);
        UpsertSocialRelationUnlocked(g, rHex, uHex, gHex, static_cast<uint8_t>(state & 0xFFu), uLen > 0);
    }
    if (g_OrigFriendInfo) {
        g_OrigFriendInfo(pThis, nullptr, szRoleName, szUnitName, szGroupName, state);
    }
}

void __fastcall HookedAddPeople(void* pThis, void* edxUnused, const char* szUnitName, const char* szRoleName) {
    (void)edxUnused;
    if (szRoleName != nullptr && szRoleName[0] != '\0') {
        size_t rLen = strnlen(szRoleName, 32);
        size_t uLen = szUnitName ? strnlen(szUnitName, 32) : 0;
        std::string rHex = jxjson::ToHex(reinterpret_cast<const uint8_t*>(szRoleName), rLen);
        std::string uHex = uLen > 0 ? jxjson::ToHex(reinterpret_cast<const uint8_t*>(szUnitName), uLen) : std::string();
        auto& g = GetGlobals();
        std::lock_guard<std::mutex> lk(g.uiMutex);
        UpsertSocialRelationUnlocked(g, rHex, uHex, std::string(), 1u, true);
    }
    if (g_OrigAddPeople) {
        g_OrigAddPeople(pThis, nullptr, szUnitName, szRoleName);
    }
}

int __fastcall HookedGameLoop(void* pThis, void* edxUnused) {
    (void)edxUnused;
    auto& g = GetGlobals();

    StartPipeServerOnce();
    EnsureDynamicHooksInstalled();

    // Keep KWin32App active & MultiGame=1 so KWin32App::Run never blocks in WaitMessage
    *reinterpret_cast<int32_t*>(reinterpret_cast<uint8_t*>(pThis) + jx::OFF_KWIN32APP_ACTIVE) = 1;
    *reinterpret_cast<int32_t*>(reinterpret_cast<uint8_t*>(pThis) + jx::OFF_KWIN32APP_MULTI_GAME) = 1;

    DrainMainThreadQueue();

    if (g.quitRequested.load(std::memory_order_acquire)) {
        auto fnDiscGw = reinterpret_cast<PfnDisconnectAgent>(jx::FN_NET_DISCONNECT_GATEWAY);
        auto fnDiscGs = reinterpret_cast<PfnDisconnectAgent>(jx::FN_NET_DISCONNECT_GAMESVR);
        fnDiscGs(reinterpret_cast<void*>(jx::ADDR_NET_CONNECT_AGENT));
        fnDiscGw(reinterpret_cast<void*>(jx::ADDR_NET_CONNECT_AGENT));
        for (int i = 0; i < 100 && !g.pipeClosed.load(std::memory_order_acquire); ++i) {
            Sleep(5);
        }
        TerminateProcess(GetCurrentProcess(), 0);
        return 0;
    }

    int loopResult = g_OrigGameLoop(pThis, nullptr);
    SyncSceneFocusToLocalPlayer();

    // Auto-subscribe to Tong# (\O<dwTongNameID>) once Core has dequeued 0x98 / 0xFD sub=2 in GameLoop
    // (KPlayerTong::GetTongNameID @ 0x005b8960: m_nFlag at +0x2c, m_dwTongNameID at +0x38;
    //  SendQueryChannelID 37B @ 0x00490f62..0x00490fcf -> s3relay_y CTongControl::GetChannelId @ 0x080c8640)
    {
        static uint32_t s_LastSubscribedTongNameId = 0;
        static uint32_t s_LastTongSubscribeTick = 0;
        uintptr_t playerBase = *reinterpret_cast<uintptr_t*>(jx::ADDR_PLAYER_ARRAY_PTR);
        void* pCoreShell = *reinterpret_cast<void**>(jx::ADDR_G_CORE_SHELL);
        if (playerBase != 0 && pCoreShell != nullptr) {
            uintptr_t pTong = playerBase + jx::STRIDE_PLAYER + jx::OFF_PLAYER_TONG;
            int32_t tongFlag = *reinterpret_cast<int32_t*>(pTong + 0x2c);
            uint32_t tongNameId = (tongFlag != 0) ? *reinterpret_cast<uint32_t*>(pTong + 0x38) : 0u;
            if (tongNameId == 0) {
                s_LastSubscribedTongNameId = 0;
                std::lock_guard<std::mutex> lk(g.uiMutex);
                g.tongChannelId = 0xFFFFFFFFu;
            } else {
                uint32_t curTongChId = 0xFFFFFFFFu;
                {
                    std::lock_guard<std::mutex> lk(g.uiMutex);
                    curTongChId = g.tongChannelId;
                }
                uint32_t curTick = *reinterpret_cast<const uint32_t*>(jx::ADDR_KMYAPP_GAMELOOP_COUNT);
                if (tongNameId != s_LastSubscribedTongNameId ||
                    (curTongChId == 0xFFFFFFFFu && (curTick - s_LastTongSubscribeTick) >= 36u)) {
                    s_LastSubscribedTongNameId = tongNameId;
                    s_LastTongSubscribeTick = curTick;
                    uint32_t beforeC2s = g.c2sSeq.load(std::memory_order_acquire);
                    if (*reinterpret_cast<void**>(jx::ADDR_UI_MSG_CENTRE_PAD_PTR) != nullptr) {
                        reinterpret_cast<void(__cdecl*)(int32_t)>(jx::FN_UI_CHECK_CHANNEL)(2);
                        reinterpret_cast<void(__cdecl*)()>(jx::FN_UI_QUERY_ALL_CHANNEL)();
                    }
                    if (g.c2sSeq.load(std::memory_order_acquire) == beforeC2s) {
                        uint8_t pkt[37] = {};
                        pkt[0] = 0xFA;
                        uint16_t wLen = 36;
                        std::memcpy(pkt + 1, &wLen, sizeof(wLen));
                        pkt[3] = 0x04;
                        pkt[4] = 0x21;
                        std::snprintf(reinterpret_cast<char*>(pkt + 5), 32, "\\O%u", tongNameId);
                        reinterpret_cast<int(__thiscall*)(void*, const void*, uint32_t)>(jx::FN_SEND_NEW_DATA_TO_CORE)(
                            pCoreShell, pkt, sizeof(pkt));
                    }
                }
            }
        }
    }

    return loopResult;
}

DWORD WINAPI PipeThreadProc(LPVOID) {
    RunPipeServerThread();
    return 0;
}

} // namespace

void StartPipeServerOnce() {
    auto& g = GetGlobals();
    if (!g.headlessEnabled.load(std::memory_order_acquire)) return;
    bool expected = false;
    if (g.pipeStarted.compare_exchange_strong(expected, true, std::memory_order_acq_rel)) {
        HANDLE hThread = CreateThread(nullptr, 0, PipeThreadProc, nullptr, 0, nullptr);
        if (hThread) CloseHandle(hThread);
    }
}

void EnsureDynamicHooksInstalled() {
    auto& g = GetGlobals();
    PatchAllModules();

    // Hook 0x005389c0 JMP 0x00552970 (rel32 at 0x005389c6)
    auto* wrapperBytes = reinterpret_cast<uint8_t*>(jx::ADDR_PROCESS_NET_MSG_WRAPPER);
    if (wrapperBytes[5] == 0xE9) {
        uintptr_t target = reinterpret_cast<uintptr_t>(&HookedProcessNetMsg);
        uintptr_t nextIp = jx::ADDR_PROCESS_NET_MSG_JMP_REL + 4;
        int32_t rel = static_cast<int32_t>(target - nextIp);
        DWORD oldProt = 0;
        if (*reinterpret_cast<int32_t*>(jx::ADDR_PROCESS_NET_MSG_JMP_REL) != rel &&
            VirtualProtect(reinterpret_cast<void*>(jx::ADDR_PROCESS_NET_MSG_JMP_REL), 4, PAGE_EXECUTE_READWRITE, &oldProt)) {
            *reinterpret_cast<int32_t*>(jx::ADDR_PROCESS_NET_MSG_JMP_REL) = rel;
            VirtualProtect(reinterpret_cast<void*>(jx::ADDR_PROCESS_NET_MSG_JMP_REL), 4, oldProt, &oldProt);
        }
    }

    // Hook g_ProtocolProcess[0x63] at 0x006e25a4 & g_ProtocolProcess[0x7E] at 0x006e2610
    auto* slot63 = reinterpret_cast<void**>(jx::ADDR_PROTOCOL_HANDLER_0X63);
    if (*slot63 == reinterpret_cast<void*>(jx::FN_S2C_SCRIPT_ACTION_HANDLER)) {
        WriteProtectedPtr(slot63, reinterpret_cast<void*>(&HookedS2CScriptAction), reinterpret_cast<void**>(&g_OrigS2CScriptAction));
    }
    auto* slot7E = reinterpret_cast<void**>(jx::ADDR_PROTOCOL_HANDLER_0X7E);
    if (*slot7E == reinterpret_cast<void*>(jx::FN_S2C_CHAT_GET_SOMEONE)) {
        WriteProtectedPtr(slot7E, reinterpret_cast<void*>(&HookedS2CChatGetSomeone), reinterpret_cast<void**>(&g_OrigS2CChatGetSomeone));
    }

    // Hook IClientCallback slots vtable[0..2, 4..8] once set by GameInit
    void* cbObj = *reinterpret_cast<void**>(jx::ADDR_G_CORE_DATA_CHANGED_CB);
    if (cbObj != nullptr) {
        void** vtable = *reinterpret_cast<void***>(cbObj);
        if (vtable) {
            if (vtable[0] != reinterpret_cast<void*>(&HookedCoreDataChanged)) {
                WriteProtectedPtr(&vtable[0], reinterpret_cast<void*>(&HookedCoreDataChanged), reinterpret_cast<void**>(&g_OrigCoreDataChanged));
            }
            if (vtable[1] != reinterpret_cast<void*>(&HookedChannelMessageArrival)) {
                WriteProtectedPtr(&vtable[1], reinterpret_cast<void*>(&HookedChannelMessageArrival), reinterpret_cast<void**>(&g_OrigChannelMessageArrival));
            }
            if (vtable[2] != reinterpret_cast<void*>(&HookedMSNMessageArrival)) {
                WriteProtectedPtr(&vtable[2], reinterpret_cast<void*>(&HookedMSNMessageArrival), reinterpret_cast<void**>(&g_OrigMSNMessageArrival));
            }
            if (vtable[3] != reinterpret_cast<void*>(&HookedOpenChannel)) {
                WriteProtectedPtr(&vtable[3], reinterpret_cast<void*>(&HookedOpenChannel), reinterpret_cast<void**>(&g_OrigOpenChannel));
            }
            if (vtable[4] != reinterpret_cast<void*>(&HookedFriendInvite)) {
                WriteProtectedPtr(&vtable[4], reinterpret_cast<void*>(&HookedFriendInvite), reinterpret_cast<void**>(&g_OrigFriendInvite));
            }
            if (vtable[5] != reinterpret_cast<void*>(&HookedAddFriend)) {
                WriteProtectedPtr(&vtable[5], reinterpret_cast<void*>(&HookedAddFriend), reinterpret_cast<void**>(&g_OrigAddFriend));
            }
            if (vtable[6] != reinterpret_cast<void*>(&HookedFriendStatus)) {
                WriteProtectedPtr(&vtable[6], reinterpret_cast<void*>(&HookedFriendStatus), reinterpret_cast<void**>(&g_OrigFriendStatus));
            }
            if (vtable[7] != reinterpret_cast<void*>(&HookedFriendInfo)) {
                WriteProtectedPtr(&vtable[7], reinterpret_cast<void*>(&HookedFriendInfo), reinterpret_cast<void**>(&g_OrigFriendInfo));
            }
            if (vtable[8] != reinterpret_cast<void*>(&HookedAddPeople)) {
                WriteProtectedPtr(&vtable[8], reinterpret_cast<void*>(&HookedAddPeople), reinterpret_cast<void**>(&g_OrigAddPeople));
            }
        }
    }

    // Hook GameServer IClient::SendPackToServer vtable[4] (+0x10) once connected
    void* gsClient = *reinterpret_cast<void**>(jx::ADDR_GAMESVR_ICLIENT);
    if (gsClient != nullptr) {
        void** gsVtable = *reinterpret_cast<void***>(gsClient);
        if (gsVtable && gsVtable[4] != nullptr && gsVtable[4] != reinterpret_cast<void*>(&HookedSendPackToServer)) {
            WriteProtectedPtr(&gsVtable[4], reinterpret_cast<void*>(&HookedSendPackToServer), reinterpret_cast<void**>(&g_OrigSendPackToServer));
        }
    }

    g.coreHooksInstalled.store(true, std::memory_order_release);
}

void InstallInitialHooks() {
    PatchAllModules();

    // Neutralize exit advertisement function at 0x00526750 (RET 4 -> C2 04 00)
    DWORD oldProt = 0;
    if (VirtualProtect(reinterpret_cast<void*>(jx::ADDR_EXIT_AD_FN), 4, PAGE_EXECUTE_READWRITE, &oldProt)) {
        auto* pExitAd = reinterpret_cast<uint8_t*>(jx::ADDR_EXIT_AD_FN);
        pExitAd[0] = 0xC2;
        pExitAd[1] = 0x04;
        pExitAd[2] = 0x00;
        VirtualProtect(reinterpret_cast<void*>(jx::ADDR_EXIT_AD_FN), 4, oldProt, &oldProt);
    }

    // Replace KMyApp::GameLoop vtable slot [3] at 0x00646bc8
    WriteProtectedPtr(
        reinterpret_cast<void*>(jx::ADDR_KMYAPP_VTABLE_GAMELOOP),
        reinterpret_cast<void*>(&HookedGameLoop),
        reinterpret_cast<void**>(&g_OrigGameLoop));
}

bool ExecuteOnMainThread(std::function<std::string()> fn, uint32_t timeoutMs, std::string& outJsonResult, std::string& outError) {
    HANDLE evt = CreateEventA(nullptr, TRUE, FALSE, nullptr);
    if (!evt) {
        outError = "CreateEventA failed";
        return false;
    }

    QueuedCommand cmd;
    cmd.fn = std::move(fn);
    cmd.doneEvent = evt;

    {
        std::lock_guard<std::mutex> lk(g_CmdMutex);
        g_CmdQueue.push_back(&cmd);
    }

    DWORD wait = WaitForSingleObject(evt, timeoutMs);
    if (wait != WAIT_OBJECT_0) {
        // Remove pending item before cmd goes out of scope!
        std::lock_guard<std::mutex> lk(g_CmdMutex);
        for (auto it = g_CmdQueue.begin(); it != g_CmdQueue.end(); ++it) {
            if (*it == &cmd) {
                g_CmdQueue.erase(it);
                CloseHandle(evt);
                outError = "Timed out waiting for main GameLoop tick";
                return false;
            }
        }
        // Already picked up or finished while locking — wait briefly for completion
        WaitForSingleObject(evt, 1000);
    }

    CloseHandle(evt);
    if (!cmd.error.empty()) {
        outError = cmd.error;
        return false;
    }
    outJsonResult = std::move(cmd.result);
    return true;
}

__declspec(noinline) void InvokeQueuedCommandBody(QueuedCommand* cmd) {
    cmd->result = cmd->fn();
}

bool TryInvokeQueuedCommand(QueuedCommand* cmd, DWORD* outExceptionCode) {
    __try {
        InvokeQueuedCommandBody(cmd);
        return true;
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        *outExceptionCode = GetExceptionCode();
        return false;
    }
}

void DrainMainThreadQueue() {
    while (true) {
        QueuedCommand* cmd = nullptr;
        {
            std::lock_guard<std::mutex> lk(g_CmdMutex);
            if (g_CmdQueue.empty()) break;
            cmd = g_CmdQueue.front();
            g_CmdQueue.pop_front();
        }
        if (cmd) {
            DWORD exCode = 0;
            if (!TryInvokeQueuedCommand(cmd, &exCode)) {
                char buf[64];
                std::snprintf(buf, sizeof(buf), "SEH exception 0x%08lX on main thread command", exCode);
                cmd->error = buf;
            }
            SetEvent(cmd->doneEvent);
        }
    }
}

} // namespace jxagent
