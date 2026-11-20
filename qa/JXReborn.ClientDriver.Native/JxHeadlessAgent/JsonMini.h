#pragma once

#include <cctype>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <map>
#include <string>
#include <vector>

namespace jxjson {

inline std::string ToHex(const uint8_t* data, size_t len) {
    static const char kHex[] = "0123456789ABCDEF";
    std::string out;
    out.reserve(len * 2);
    for (size_t i = 0; i < len; ++i) {
        uint8_t b = data[i];
        out.push_back(kHex[(b >> 4) & 0xF]);
        out.push_back(kHex[b & 0xF]);
    }
    return out;
}

inline std::string ToHexCStr(const char* str, size_t maxBytes) {
    if (!str) return {};
    size_t len = strnlen(str, maxBytes);
    return ToHex(reinterpret_cast<const uint8_t*>(str), len);
}

inline int HexVal(char c) {
    if (c >= '0' && c <= '9') return c - '0';
    if (c >= 'a' && c <= 'f') return c - 'a' + 10;
    if (c >= 'A' && c <= 'F') return c - 'A' + 10;
    return -1;
}

inline std::vector<uint8_t> FromHex(const std::string& hex) {
    std::vector<uint8_t> out;
    out.reserve(hex.size() / 2);
    for (size_t i = 0; i + 1 < hex.size(); i += 2) {
        int hi = HexVal(hex[i]);
        int lo = HexVal(hex[i + 1]);
        if (hi < 0 || lo < 0) break;
        out.push_back(static_cast<uint8_t>((hi << 4) | lo));
    }
    return out;
}

inline std::string EscapeJson(const std::string& s) {
    std::string out;
    out.reserve(s.size() + 8);
    for (unsigned char c : s) {
        switch (c) {
        case '"': out += "\\\""; break;
        case '\\': out += "\\\\"; break;
        case '\b': out += "\\b"; break;
        case '\f': out += "\\f"; break;
        case '\n': out += "\\n"; break;
        case '\r': out += "\\r"; break;
        case '\t': out += "\\t"; break;
        default:
            if (c < 0x20 || c >= 0x7F) {
                char buf[8];
                std::snprintf(buf, sizeof(buf), "\\u%04x", static_cast<unsigned>(c));
                out += buf;
            } else {
                out.push_back(static_cast<char>(c));
            }
            break;
        }
    }
    return out;
}

inline std::string QuoteJson(const std::string& s) {
    return "\"" + EscapeJson(s) + "\"";
}

// Minimal JSON value parser for RPC request objects (supports nested objects/arrays/strings/ints/bools/null).
struct JsonValue {
    enum Type { Null, Bool, Int, String, Object, Array } type = Null;
    bool boolVal = false;
    int64_t intVal = 0;
    std::string strVal;
    std::map<std::string, JsonValue> objVal;
    std::vector<JsonValue> arrVal;
    std::string rawSlice;

    const JsonValue* Get(const std::string& key) const {
        if (type != Object) return nullptr;
        auto it = objVal.find(key);
        return it == objVal.end() ? nullptr : &it->second;
    }

    std::string GetString(const std::string& key, const std::string& def = {}) const {
        const JsonValue* v = Get(key);
        return (v && v->type == String) ? v->strVal : def;
    }

    int64_t GetInt(const std::string& key, int64_t def = 0) const {
        const JsonValue* v = Get(key);
        return (v && v->type == Int) ? v->intVal : def;
    }

    bool GetBool(const std::string& key, bool def = false) const {
        const JsonValue* v = Get(key);
        return (v && v->type == Bool) ? v->boolVal : def;
    }
};

class Parser {
public:
    explicit Parser(const std::string& src) : src_(src), pos_(0) {}

    bool Parse(JsonValue& out) {
        SkipWs();
        if (!ParseValue(out)) return false;
        SkipWs();
        return true;
    }

private:
    const std::string& src_;
    size_t pos_;

    void SkipWs() {
        while (pos_ < src_.size() && std::isspace(static_cast<unsigned char>(src_[pos_]))) {
            ++pos_;
        }
    }

    bool ParseValue(JsonValue& out) {
        SkipWs();
        if (pos_ >= src_.size()) return false;
        size_t start = pos_;
        char c = src_[pos_];
        if (c == '{') {
            if (!ParseObject(out)) return false;
        } else if (c == '[') {
            if (!ParseArray(out)) return false;
        } else if (c == '"') {
            out.type = JsonValue::String;
            if (!ParseString(out.strVal)) return false;
        } else if (c == 't' && src_.compare(pos_, 4, "true") == 0) {
            out.type = JsonValue::Bool;
            out.boolVal = true;
            pos_ += 4;
        } else if (c == 'f' && src_.compare(pos_, 5, "false") == 0) {
            out.type = JsonValue::Bool;
            out.boolVal = false;
            pos_ += 5;
        } else if (c == 'n' && src_.compare(pos_, 4, "null") == 0) {
            out.type = JsonValue::Null;
            pos_ += 4;
        } else if (c == '-' || std::isdigit(static_cast<unsigned char>(c))) {
            out.type = JsonValue::Int;
            char* endPtr = nullptr;
            out.intVal = std::strtoll(src_.c_str() + pos_, &endPtr, 10);
            if (endPtr == src_.c_str() + pos_) return false;
            pos_ = static_cast<size_t>(endPtr - src_.c_str());
        } else {
            return false;
        }
        out.rawSlice = src_.substr(start, pos_ - start);
        return true;
    }

    bool ParseString(std::string& out) {
        if (pos_ >= src_.size() || src_[pos_] != '"') return false;
        ++pos_;
        out.clear();
        while (pos_ < src_.size()) {
            char c = src_[pos_++];
            if (c == '"') return true;
            if (c == '\\') {
                if (pos_ >= src_.size()) return false;
                char esc = src_[pos_++];
                switch (esc) {
                case '"': out.push_back('"'); break;
                case '\\': out.push_back('\\'); break;
                case '/': out.push_back('/'); break;
                case 'b': out.push_back('\b'); break;
                case 'f': out.push_back('\f'); break;
                case 'n': out.push_back('\n'); break;
                case 'r': out.push_back('\r'); break;
                case 't': out.push_back('\t'); break;
                case 'u': {
                    if (pos_ + 4 > src_.size()) return false;
                    uint32_t cp = 0;
                    for (int i = 0; i < 4; ++i) {
                        int hv = HexVal(src_[pos_++]);
                        if (hv < 0) return false;
                        cp = (cp << 4) | static_cast<uint32_t>(hv);
                    }
                    if (cp < 0x80) {
                        out.push_back(static_cast<char>(cp));
                    } else if (cp < 0x800) {
                        out.push_back(static_cast<char>(0xC0 | (cp >> 6)));
                        out.push_back(static_cast<char>(0x80 | (cp & 0x3F)));
                    } else {
                        out.push_back(static_cast<char>(0xE0 | (cp >> 12)));
                        out.push_back(static_cast<char>(0x80 | ((cp >> 6) & 0x3F)));
                        out.push_back(static_cast<char>(0x80 | (cp & 0x3F)));
                    }
                    break;
                }
                default: return false;
                }
            } else {
                out.push_back(c);
            }
        }
        return false;
    }

    bool ParseObject(JsonValue& out) {
        out.type = JsonValue::Object;
        ++pos_;
        SkipWs();
        if (pos_ < src_.size() && src_[pos_] == '}') {
            ++pos_;
            return true;
        }
        while (pos_ < src_.size()) {
            SkipWs();
            std::string key;
            if (!ParseString(key)) return false;
            SkipWs();
            if (pos_ >= src_.size() || src_[pos_] != ':') return false;
            ++pos_;
            JsonValue val;
            if (!ParseValue(val)) return false;
            out.objVal[key] = std::move(val);
            SkipWs();
            if (pos_ < src_.size() && src_[pos_] == ',') {
                ++pos_;
                continue;
            }
            if (pos_ < src_.size() && src_[pos_] == '}') {
                ++pos_;
                return true;
            }
            return false;
        }
        return false;
    }

    bool ParseArray(JsonValue& out) {
        out.type = JsonValue::Array;
        ++pos_;
        SkipWs();
        if (pos_ < src_.size() && src_[pos_] == ']') {
            ++pos_;
            return true;
        }
        while (pos_ < src_.size()) {
            JsonValue elem;
            if (!ParseValue(elem)) return false;
            out.arrVal.push_back(std::move(elem));
            SkipWs();
            if (pos_ < src_.size() && src_[pos_] == ',') {
                ++pos_;
                continue;
            }
            if (pos_ < src_.size() && src_[pos_] == ']') {
                ++pos_;
                return true;
            }
            return false;
        }
        return false;
    }
};

} // namespace jxjson
