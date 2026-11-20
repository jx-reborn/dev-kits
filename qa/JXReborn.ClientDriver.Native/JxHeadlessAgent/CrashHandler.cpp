#include "AgentState.h"
#include "../include/jx_offsets.h"

#include <cstring>

namespace jxagent {

namespace {

bool IsFatalCrashException(DWORD code) {
    switch (code) {
    case EXCEPTION_ACCESS_VIOLATION:
    case EXCEPTION_IN_PAGE_ERROR:
    case EXCEPTION_INT_DIVIDE_BY_ZERO:
    case EXCEPTION_INT_OVERFLOW:
    case EXCEPTION_ILLEGAL_INSTRUCTION:
    case EXCEPTION_PRIV_INSTRUCTION:
    case EXCEPTION_STACK_OVERFLOW:
    case EXCEPTION_ARRAY_BOUNDS_EXCEEDED:
    case EXCEPTION_FLT_DIVIDE_BY_ZERO:
    case EXCEPTION_FLT_INVALID_OPERATION:
    case 0xC0000409: // STATUS_STACK_BUFFER_OVERRUN
        return true;
    default:
        return false;
    }
}

struct StackJsonWriter {
    char buf[65536];
    size_t len = 0;

    void PutChar(char c) {
        if (len + 1 < sizeof(buf)) {
            buf[len++] = c;
        }
    }

    void PutStr(const char* s) {
        if (!s) return;
        while (*s && len + 1 < sizeof(buf)) {
            PutChar(*s++);
        }
    }

    void PutJsonStr(const char* s) {
        PutChar('"');
        if (s) {
            for (const char* p = s; *p; ++p) {
                char c = *p;
                if (c == '"' || c == '\\') {
                    PutChar('\\');
                    PutChar(c);
                } else if (static_cast<unsigned char>(c) >= 0x20 && static_cast<unsigned char>(c) < 0x7F) {
                    PutChar(c);
                } else {
                    PutChar('.');
                }
            }
        }
        PutChar('"');
    }

    void PutHex32(uint32_t v) {
        static const char kHex[] = "0123456789ABCDEF";
        PutStr("\"0x");
        for (int shift = 28; shift >= 0; shift -= 4) {
            PutChar(kHex[(v >> shift) & 0xF]);
        }
        PutChar('"');
    }

    void PutUInt(uint32_t v) {
        char tmp[16];
        int i = 0;
        if (v == 0) {
            PutChar('0');
            return;
        }
        while (v > 0 && i < 15) {
            tmp[i++] = static_cast<char>('0' + (v % 10));
            v /= 10;
        }
        while (i > 0) {
            PutChar(tmp[--i]);
        }
    }

    void PutHexBytes(const uint8_t* data, size_t count) {
        static const char kHex[] = "0123456789ABCDEF";
        PutChar('"');
        for (size_t i = 0; i < count; ++i) {
            uint8_t b = data[i];
            PutChar(kHex[(b >> 4) & 0xF]);
            PutChar(kHex[b & 0xF]);
        }
        PutChar('"');
    }
};

volatile LONG g_CrashHandled = 0;

LONG WINAPI UnhandledCrashFilter(PEXCEPTION_POINTERS info) {
    if (!info || !info->ExceptionRecord || !info->ContextRecord) {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    DWORD code = info->ExceptionRecord->ExceptionCode;
    if (!IsFatalCrashException(code)) {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    // Avoid infinite recursion if crash occurs inside diagnostics
    if (InterlockedCompareExchange(&g_CrashHandled, 1, 0) != 0) {
        TerminateProcess(GetCurrentProcess(), code);
        return EXCEPTION_EXECUTE_HANDLER;
    }

    auto& g = GetGlobals();
    if (!g.crashDumpPath.empty()) {
        static StackJsonWriter w;
        w.len = 0;

        uintptr_t eip = info->ContextRecord->Eip;
        HMODULE hMod = nullptr;
        char modPath[MAX_PATH] = "unknown";
        uintptr_t modRva = 0;
        if (GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                               reinterpret_cast<LPCSTR>(eip), &hMod) && hMod) {
            GetModuleFileNameA(hMod, modPath, MAX_PATH);
            modRva = eip - reinterpret_cast<uintptr_t>(hMod);
        }

        const char* modBaseName = modPath;
        for (const char* p = modPath; *p; ++p) {
            if (*p == '\\' || *p == '/') modBaseName = p + 1;
        }

        uint32_t tickCount = 0;
        int32_t loginStatus = -1;
        __try {
            tickCount = *reinterpret_cast<const uint32_t*>(jx::ADDR_KMYAPP_GAMELOOP_COUNT);
            loginStatus = *reinterpret_cast<const int32_t*>(jx::ADDR_KLOGIN + jx::OFF_KLOGIN_STATUS);
        } __except (EXCEPTION_EXECUTE_HANDLER) {
        }

        uintptr_t faultAddr = info->ExceptionRecord->NumberParameters >= 2
            ? static_cast<uintptr_t>(info->ExceptionRecord->ExceptionInformation[1])
            : reinterpret_cast<uintptr_t>(info->ExceptionRecord->ExceptionAddress);

        w.PutStr("{\"pid\":"); w.PutUInt(GetCurrentProcessId());
        w.PutStr(",\"exception_code\":"); w.PutHex32(code);
        w.PutStr(",\"exception_address\":"); w.PutHex32(reinterpret_cast<uintptr_t>(info->ExceptionRecord->ExceptionAddress));
        w.PutStr(",\"fault_address\":"); w.PutHex32(static_cast<uint32_t>(faultAddr));
        w.PutStr(",\"module\":"); w.PutJsonStr(modBaseName);
        w.PutStr(",\"module_rva\":"); w.PutHex32(static_cast<uint32_t>(modRva));
        w.PutStr(",\"tick_count\":"); w.PutUInt(tickCount);
        w.PutStr(",\"login_status\":"); w.PutUInt(static_cast<uint32_t>(loginStatus));
        w.PutStr(",\"registers\":{\"eip\":"); w.PutHex32(info->ContextRecord->Eip);
        w.PutStr(",\"esp\":"); w.PutHex32(info->ContextRecord->Esp);
        w.PutStr(",\"ebp\":"); w.PutHex32(info->ContextRecord->Ebp);
        w.PutStr(",\"eax\":"); w.PutHex32(info->ContextRecord->Eax);
        w.PutStr(",\"ebx\":"); w.PutHex32(info->ContextRecord->Ebx);
        w.PutStr(",\"ecx\":"); w.PutHex32(info->ContextRecord->Ecx);
        w.PutStr(",\"edx\":"); w.PutHex32(info->ContextRecord->Edx);
        w.PutStr(",\"esi\":"); w.PutHex32(info->ContextRecord->Esi);
        w.PutStr(",\"edi\":"); w.PutHex32(info->ContextRecord->Edi);
        w.PutStr("},\"recent_s2c\":[");

        constexpr uint32_t kMaxCrashS2c = 32;
        uint32_t total = g.s2cSeq.load(std::memory_order_acquire);
        uint32_t start = total > kMaxCrashS2c ? total - kMaxCrashS2c : 0;
        bool first = true;
        for (uint32_t seq = start; seq < total; ++seq) {
            const S2cPacketRecord& rec = g.s2cRing[seq % kS2cRingSize];
            if (rec.seq != seq + 1) continue;
            if (!first) w.PutChar(',');
            first = false;
            w.PutStr("{\"seq\":"); w.PutUInt(rec.seq);
            w.PutStr(",\"tick\":"); w.PutUInt(rec.tickCount);
            w.PutStr(",\"opcode\":"); w.PutHex32(rec.opcode);
            w.PutStr(",\"length\":"); w.PutUInt(rec.length);
            w.PutStr(",\"hex\":"); w.PutHexBytes(rec.bytes, rec.capturedLen);
            w.PutChar('}');
        }
        w.PutStr("],\"recent_c2s\":[");
        uint32_t totalC2s = g.c2sSeq.load(std::memory_order_acquire);
        uint32_t startC2s = totalC2s > kMaxCrashS2c ? totalC2s - kMaxCrashS2c : 0;
        bool firstC2s = true;
        for (uint32_t seq = startC2s; seq < totalC2s; ++seq) {
            const C2sPacketRecord& rec = g.c2sRing[seq % kC2sRingSize];
            if (rec.seq != seq + 1) continue;
            if (!firstC2s) w.PutChar(',');
            firstC2s = false;
            w.PutStr("{\"seq\":"); w.PutUInt(rec.seq);
            w.PutStr(",\"tick\":"); w.PutUInt(rec.tickCount);
            w.PutStr(",\"opcode\":"); w.PutHex32(rec.opcode);
            w.PutStr(",\"length\":"); w.PutUInt(rec.length);
            w.PutStr(",\"hex\":"); w.PutHexBytes(rec.bytes, rec.capturedLen);
            w.PutChar('}');
        }
        w.PutStr("]}\n");

        HANDLE hFile = CreateFileA(
            g.crashDumpPath.c_str(),
            GENERIC_WRITE,
            FILE_SHARE_READ,
            nullptr,
            CREATE_ALWAYS,
            FILE_ATTRIBUTE_NORMAL,
            nullptr);
        if (hFile != INVALID_HANDLE_VALUE) {
            DWORD written = 0;
            WriteFile(hFile, w.buf, static_cast<DWORD>(w.len), &written, nullptr);
            FlushFileBuffers(hFile);
            CloseHandle(hFile);
        }
    }

    TerminateProcess(GetCurrentProcess(), code);
    return EXCEPTION_EXECUTE_HANDLER;
}

} // namespace

void InstallVehCrashHandler() {
    SetUnhandledExceptionFilter(&UnhandledCrashFilter);
}

void RecordS2cPacket(const uint8_t* data, size_t len, uint32_t tickCount) {
    if (!data || len == 0) return;
    auto& g = GetGlobals();
    if (data[0] == 0x49) {
        g.worldSyncReady.store(false, std::memory_order_release);
    } else if (data[0] == 0x4E || data[0] == 0xC5) {
        g.worldSyncReady.store(true, std::memory_order_release);
    }
    uint32_t seq0 = g.s2cSeq.fetch_add(1, std::memory_order_acq_rel);
    uint32_t nowMs = GetTickCount();
    S2cPacketRecord& slot = g.s2cRing[seq0 % kS2cRingSize];
    slot.seq = seq0 + 1;
    slot.tickCount = tickCount;
    slot.timestampMs = nowMs;
    slot.opcode = data[0];
    slot.length = len > 0xFFFF ? 0xFFFF : static_cast<uint16_t>(len);
    size_t cap = len > kMaxCapturedPacketBytes ? kMaxCapturedPacketBytes : len;
    slot.capturedLen = static_cast<uint16_t>(cap);
    std::memcpy(slot.bytes, data, cap);

    if (g.wireCaptureActive.load(std::memory_order_acquire)) {
        size_t fullCap = len > 65535 ? 65535 : len;
        CapturedWireFrameRecord rec;
        rec.isC2s = false;
        rec.seq = seq0 + 1;
        rec.tickCount = tickCount;
        rec.timestampMs = nowMs;
        rec.opcode = data[0];
        rec.bytes.reserve(fullCap);
        for (size_t i = 0; i < fullCap; ++i) {
            rec.bytes.push_back(data[i]);
        }
        std::lock_guard<std::mutex> lk(g.wireCaptureMutex);
        if (g.wireCaptureActive.load(std::memory_order_relaxed) && g.wireCaptureFrames.size() < 16384) {
            g.wireCaptureFrames.push_back(std::move(rec));
        }
    }
}

void RecordC2sPacket(const uint8_t* data, size_t len, uint32_t tickCount) {
    if (!data || len == 0) return;
    auto& g = GetGlobals();
    uint32_t seq0 = g.c2sSeq.fetch_add(1, std::memory_order_acq_rel);
    uint32_t nowMs = GetTickCount();
    C2sPacketRecord& slot = g.c2sRing[seq0 % kS2cRingSize];
    slot.seq = seq0 + 1;
    slot.tickCount = tickCount;
    slot.timestampMs = nowMs;
    slot.opcode = data[0];
    slot.length = len > 0xFFFF ? 0xFFFF : static_cast<uint16_t>(len);
    size_t cap = len > kMaxCapturedPacketBytes ? kMaxCapturedPacketBytes : len;
    slot.capturedLen = static_cast<uint16_t>(cap);
    std::memcpy(slot.bytes, data, cap);

    if (g.wireCaptureActive.load(std::memory_order_acquire)) {
        size_t fullCap = len > 65535 ? 65535 : len;
        CapturedWireFrameRecord rec;
        rec.isC2s = true;
        rec.seq = seq0 + 1;
        rec.tickCount = tickCount;
        rec.timestampMs = nowMs;
        rec.opcode = data[0];
        rec.bytes.reserve(fullCap);
        for (size_t i = 0; i < fullCap; ++i) {
            rec.bytes.push_back(data[i]);
        }
        std::lock_guard<std::mutex> lk(g.wireCaptureMutex);
        if (g.wireCaptureActive.load(std::memory_order_relaxed) && g.wireCaptureFrames.size() < 16384) {
            g.wireCaptureFrames.push_back(std::move(rec));
        }
    }
}

} // namespace jxagent
