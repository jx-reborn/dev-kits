#include "AgentState.h"

#include <cstdio>
#include <cstdlib>

namespace jxagent {

AgentGlobals& GetGlobals() {
    static AgentGlobals g;
    return g;
}

} // namespace jxagent

namespace {

INIT_ONCE g_WinmmInitOnce = INIT_ONCE_STATIC_INIT;
HMODULE g_RealWinmm = nullptr;

BOOL CALLBACK LoadRealWinmmOnce(PINIT_ONCE, PVOID, PVOID*) {
    char dir[MAX_PATH] = {};
    UINT len = GetSystemWow64DirectoryA(dir, MAX_PATH);
    if (len == 0 || len >= MAX_PATH - 16) {
        len = GetSystemDirectoryA(dir, MAX_PATH);
    }
    if (len > 0 && len < MAX_PATH - 16) {
        if (dir[len - 1] != '\\') {
            dir[len++] = '\\';
        }
        lstrcpynA(dir + len, "winmm.dll", static_cast<int>(MAX_PATH - len));
        g_RealWinmm = LoadLibraryA(dir);
    }
    if (!g_RealWinmm) {
        g_RealWinmm = LoadLibraryA("C:\\Windows\\SysWOW64\\winmm.dll");
    }
    return TRUE;
}

extern "C" FARPROC __stdcall EnsureWinmmProc(int idx, const char* name, WORD ordinal) {
    InitOnceExecuteOnce(&g_WinmmInitOnce, LoadRealWinmmOnce, nullptr, nullptr);
    FARPROC proc = nullptr;
    if (g_RealWinmm) {
        proc = name ? GetProcAddress(g_RealWinmm, name)
                    : GetProcAddress(g_RealWinmm, MAKEINTRESOURCEA(ordinal));
    }
    return proc;
}

} // namespace

// Generate g_WinmmProcs[193] + naked proxy thunks that store resolved FARPROC into g_WinmmProcs[idx].
// Wrapper helper used by thunks to cache resolved address into g_WinmmProcs before jumping.
namespace {
// Redefined after including WinmmThunks.inc? No, WinmmThunks.inc references g_WinmmProcs and EnsureWinmmProc directly!
}

// Re-declare EnsureWinmmProc cleanly so thunks cache into g_WinmmProcs without re-resolving on hot calls (e.g. timeGetTime).
static FARPROC g_WinmmProcs_Storage[193];
#define g_WinmmProcs g_WinmmProcs_Storage

static FARPROC __stdcall ResolveAndCacheWinmmProc(int idx, const char* name, WORD ordinal) {
    FARPROC p = g_WinmmProcs_Storage[idx];
    if (p) return p;
    p = ::EnsureWinmmProc(idx, name, ordinal);
    if (p && idx >= 0 && idx < 193) {
        InterlockedExchangePointer(reinterpret_cast<PVOID volatile*>(&g_WinmmProcs_Storage[idx]), reinterpret_cast<PVOID>(p));
    }
    return p;
}

// Intercept EnsureWinmmProc call name inside WinmmThunks.inc by redirecting via macro AFTER real EnsureWinmmProc definition.
#define EnsureWinmmProc ResolveAndCacheWinmmProc
// Suppress duplicate g_WinmmProcs definition in WinmmThunks.inc by aliasing it cleanly:
#pragma push_macro("g_WinmmProcs")
#undef g_WinmmProcs
#include "WinmmThunks.inc"
#pragma pop_macro("g_WinmmProcs")
#undef EnsureWinmmProc

namespace {

std::string NormalizePipeName(const char* raw) {
    if (!raw || raw[0] == '\0') {
        char buf[64];
        std::snprintf(buf, sizeof(buf), "\\\\.\\pipe\\jx-headless-%lu", GetCurrentProcessId());
        return std::string(buf);
    }
    std::string s(raw);
    if (s.rfind("\\\\.\\pipe\\", 0) == 0) {
        return s;
    }
    return std::string("\\\\.\\pipe\\") + s;
}

void InitializeHeadlessAgent() {
    auto& g = jxagent::GetGlobals();

    char envHeadless[16] = {};
    DWORD n = GetEnvironmentVariableA("LEGACY_HEADLESS", envHeadless, sizeof(envHeadless));
    if (n == 0 || envHeadless[0] != '1') {
        return;
    }

    char envPipe[256] = {};
    GetEnvironmentVariableA("LEGACY_PIPE_NAME", envPipe, sizeof(envPipe));
    g.pipeName = NormalizePipeName(envPipe);

    char envCrash[MAX_PATH] = {};
    if (GetEnvironmentVariableA("LEGACY_CRASH_DUMP_PATH", envCrash, sizeof(envCrash)) > 0) {
        g.crashDumpPath = envCrash;
    }

    char envInteractive[16] = {};
    if (GetEnvironmentVariableA("LEGACY_INTERACTIVE_WINDOW", envInteractive, sizeof(envInteractive)) > 0 &&
        envInteractive[0] == '1') {
        g.interactiveWindowEnabled.store(true, std::memory_order_release);
    }

    g.headlessEnabled.store(true, std::memory_order_release);

    // Safe inside DllMain: only VEH registration + VirtualProtect memory/IAT patches (no LoadLibrary/CreateThread).
    jxagent::InstallVehCrashHandler();
    jxagent::InstallInitialHooks();
}

} // namespace

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID reserved) {
    (void)reserved;
    if (reason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hModule);
        InitializeHeadlessAgent();
    }
    return TRUE;
}
