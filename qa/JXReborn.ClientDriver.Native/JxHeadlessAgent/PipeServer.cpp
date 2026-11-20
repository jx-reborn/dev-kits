#include "AgentState.h"
#include "JsonMini.h"
#include "../include/jx_offsets.h"

#include <sstream>
#include <string>

namespace jxagent {

namespace {

bool WriteAllToPipe(HANDLE hPipe, const std::string& data) {
    const char* ptr = data.data();
    size_t remaining = data.size();
    while (remaining > 0) {
        DWORD written = 0;
        if (!WriteFile(hPipe, ptr, static_cast<DWORD>(remaining), &written, nullptr) || written == 0) {
            return false;
        }
        ptr += written;
        remaining -= written;
    }
    return true;
}

std::string FormatResponse(const std::string& id, bool ok, const std::string& payloadOrError) {
    std::ostringstream oss;
    oss << "{\"id\":" << jxjson::QuoteJson(id) << ",\"ok\":" << (ok ? "true" : "false");
    if (ok) {
        oss << ",\"result\":" << (payloadOrError.empty() ? "null" : payloadOrError);
    } else {
        oss << ",\"error\":" << jxjson::QuoteJson(payloadOrError);
    }
    oss << "}\n";
    return oss.str();
}

std::string ProcessRpcLine(const std::string& line, bool& outQuitNow) {
    outQuitNow = false;
    jxjson::JsonValue req;
    jxjson::Parser parser(line);
    if (!parser.Parse(req) || req.type != jxjson::JsonValue::Object) {
        return FormatResponse("", false, "Malformed JSON-RPC request");
    }

    std::string id = req.GetString("id", "");
    std::string method = req.GetString("method", "");
    if (method.empty()) {
        return FormatResponse(id, false, "Missing method field");
    }

    if (method == "ping") {
        uint32_t tickCount = *reinterpret_cast<const uint32_t*>(jx::ADDR_KMYAPP_GAMELOOP_COUNT);
        std::ostringstream res;
        res << "{\"pong\":true,\"pid\":" << GetCurrentProcessId() << ",\"tick_count\":" << tickCount << "}";
        return FormatResponse(id, true, res.str());
    }

    if (method == "quit") {
        GetGlobals().quitRequested.store(true, std::memory_order_release);
        outQuitNow = true;
        return FormatResponse(id, true, "{\"quitting\":true}");
    }

    const jxjson::JsonValue* paramsVal = req.Get("params");
    std::string paramsRaw = (paramsVal && !paramsVal->rawSlice.empty()) ? paramsVal->rawSlice : "{}";

    std::string outJsonResult;
    std::string outErr;
    bool dispatched = ExecuteOnMainThread(
        [method, paramsRaw]() {
            return HandleRpcMethod(method, paramsRaw);
        },
        5000,
        outJsonResult,
        outErr);

    if (!dispatched) {
        return FormatResponse(id, false, outErr);
    }
    if (outJsonResult.rfind("!ERR:", 0) == 0) {
        return FormatResponse(id, false, outJsonResult.substr(5));
    }
    return FormatResponse(id, true, outJsonResult);
}

} // namespace

void RunPipeServerThread() {
    auto& g = GetGlobals();
    std::string fullPipeName = g.pipeName;
    if (fullPipeName.empty()) {
        char fallback[64];
        std::snprintf(fallback, sizeof(fallback), "jx-headless-%lu", static_cast<unsigned long>(GetCurrentProcessId()));
        fullPipeName = fallback;
    }
    if (fullPipeName.rfind("\\\\.\\pipe\\", 0) != 0) {
        fullPipeName = "\\\\.\\pipe\\" + fullPipeName;
    }

    HANDLE hPipe = CreateNamedPipeA(
        fullPipeName.c_str(),
        PIPE_ACCESS_DUPLEX,
        PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT,
        1,
        1024 * 1024,
        64 * 1024,
        0,
        nullptr);

    if (hPipe == INVALID_HANDLE_VALUE) {
        g.pipeClosed.store(true, std::memory_order_release);
        return;
    }

    while (!g.quitRequested.load(std::memory_order_acquire)) {
        BOOL connected = ConnectNamedPipe(hPipe, nullptr) ? TRUE : (GetLastError() == ERROR_PIPE_CONNECTED);
        if (!connected) {
            Sleep(10);
            continue;
        }

        std::string buffer;
        char chunk[4096];
        bool quitNow = false;

        while (!g.quitRequested.load(std::memory_order_acquire) && !quitNow) {
            DWORD bytesRead = 0;
            BOOL readOk = ReadFile(hPipe, chunk, sizeof(chunk), &bytesRead, nullptr);
            if (!readOk || bytesRead == 0) {
                break;
            }
            buffer.append(chunk, bytesRead);

            size_t nlPos = std::string::npos;
            while ((nlPos = buffer.find('\n')) != std::string::npos) {
                std::string line = buffer.substr(0, nlPos);
                buffer.erase(0, nlPos + 1);
                while (!line.empty() && (line.back() == '\r' || line.back() == ' ' || line.back() == '\t')) {
                    line.pop_back();
                }
                if (line.empty()) continue;

                std::string response = ProcessRpcLine(line, quitNow);
                if (!WriteAllToPipe(hPipe, response)) {
                    quitNow = true;
                    break;
                }
                FlushFileBuffers(hPipe);
                if (quitNow) {
                    break;
                }
            }
        }

        FlushFileBuffers(hPipe);
        DisconnectNamedPipe(hPipe);
        if (quitNow) {
            break;
        }
    }

    CloseHandle(hPipe);
    g.pipeClosed.store(true, std::memory_order_release);
}

} // namespace jxagent
