# JXReborn Wire Parity Proxy (`JXReborn.WireParityProxy`)

Server trung gian kiểm thử đối chiếu ở **tầng giao thức Wire L7** (**Single-Client, Dual-Backend Differential Shadow Proxy**): chạy **duy nhất 1 `game.exe`** (kèm driver tùy chọn) kết nối vào proxy, proxy đồng thời kết nối tới **`legacy-linux`** (Primary Oracle) và **`reborn-server`** (Shadow SUT), giải mã/dịch ID động theo thời gian thực và kiểm định 3 tầng song song.

## Điểm khác biệt so với `JXReborn.DualParityServer`

- Chỉ cần chạy **1 cửa sổ `game.exe` duy nhất** (thay vì 2 `game.exe`).
- Nhân bản **100% gói tin C2S trên wire** (mọi thao tác di chuyển, skill, dùng/di chuyển item, đối thoại NPC, mua bán, trade, chat...) thay vì chỉ mirror một số hành động chọn lọc.
- Kiểm định đồng thời ở **3 tầng**:
  1. **Tầng A — Protocol Safety Gate (`GameExeProtocolVerifier`)**: kiểm tra mọi gói S2C của Shadow (`reborn-server`) theo bảng kích thước chuẩn 134 opcode & state machine của `game.exe` để bắt ngay lỗi có thể làm crash client mà không cần chạy `game.exe #2`.
  2. **Tầng B — Action-Window Wire Diff**: gom phản hồi S2C của cả 2 server theo cửa sổ hành động C2S (lọc bỏ `s2c_ping`/heartbeat nền), báo cáo thiếu/thừa opcode và lệch kích thước frame.
  3. **Tầng C — Converged Shadow State Diff & Drift Detector**: dựng `ProxyShadowClientState` cho cả 2 backend, đối chiếu trạng thái ngữ nghĩa qua `SemanticDiffEngine` và cảnh báo khi phát sinh State Drift (`MapId` lệch hoặc khoảng cách vị trí vượt ngưỡng).
- **One-Click Export `*.capture.json` (L3A+)**: xuất trực tiếp bất kỳ action window nào thành file capture đã ký SHA-256 để replay offline trong CI (0 Docker, 0 `game.exe`).

## Kiến trúc luồng kết nối

```text
1x game.exe ──► BishopShadowProxy (:16622) ──┬──► legacy-linux Bishop (:15622) [Primary]
                                             └──► reborn-server Bishop (:15623) [Shadow]
                     │ (map Guid_L <-> Guid_R, rewrite 0x34 IP:Port -> :16668)
                     ▼
1x game.exe ──► GameServerShadowProxy (:16668) ──┬──► legacy-linux GS (:16666) [Primary -> trả S2C về game.exe]
                                                 └──► reborn-server GS (:16667) [Shadow  -> chặn lại kiểm định & diff]
```

## Cấu trúc thư mục

- `Configuration/` — `WireParityProxyOptions` (CLI flags & ngưỡng diff/drift).
- `Wire/` — `JxWireConnection` (bắt tay `0x20` + 3-way `LegacyStreamCipher`), `JxWireFrame`, `WireKeyTableLoader`.
- `Proxy/` — `BishopShadowProxy`, `GameServerShadowProxy`, `HandoffTicketMap`, `WireParitySession`.
- `ShadowStates/` — `ProxyShadowClientState`, `ShadowDialogState`, `ShadowStateSnapshotAdapter`.
- `Translation/` — `EntityIdTranslator` (`SelfNpcId`/`NpcId`/`ItemId` & ô túi đồ), `C2sPacketRewriter` (dịch ID + tính lại `dwCheckValue`).
- `Diffing/` — `ProtocolSafetyGate`, `ActionWindowWireDiffer`, `StateDriftDetector`, `WireParityDiffCoordinator`.
- `Driver/` — `PrimaryClientDriverBridge` (kết nối tùy chọn Named Pipe tới 1 `game.exe`).
- `Mcp/` — `WireParityMcpToolDispatcher`, `WireParityMcpStdioHost` (MCP Server qua stdio JSON-RPC 2.0 cho AI Agent).
- `Reporting/` — `WireParityReportWriter` (`findings.jsonl`, `report.md`, `report.html`), `L3aCaptureExporter` (`*.capture.json`).
- `Web/` — Dashboard trực quan tại `http://127.0.0.1:5100`.

## Cách chạy nhanh

### 0. Chế độ Standalone (All-in-One — 1 lệnh bật tất cả & tự dọn sạch khi tắt)

Tự khởi động song song container `legacy-linux` (`T=0` qua `run-parity.ps1 -StartOnly`) + `JXReborn.Host` cô lập (port `15623`/`16667`, SQLite DB tạm `T=0`, không đụng `JXReborn.Host` dev) + Bishop/GS L7 Shadow Proxy (`:16622` / `:16668`) + **1 cửa sổ `game.exe` duy nhất** trỏ thẳng vào `:16622` + tự mở Dashboard `:5100`. Khi bấm `Ctrl+C`, tự xuất report + server logs (`legacy-server.log`, `reborn-server.log`) và dọn sạch toàn bộ:

```powershell
dotnet run --project d:\Projects\jx-reborn\workbench\qa\JXReborn.WireParityProxy -- --standalone
```

Tùy chọn hữu ích cho Standalone:
- `--reuse-legacy`: tái sử dụng container `legacy-oracle-test` đang chạy thay vì tạo mới.
- `--keep-containers`: giữ nguyên container `legacy-linux` sau khi tắt để lần sau khởi động nhanh hơn.
- `--no-spawn`: chỉ bật 2 server + L7 Proxy + Dashboard, không tự mở `game.exe` (để bạn tự chạy `game.exe` ngoài).
- `--headless`: chạy `game.exe` ẩn không mở cửa sổ Win32 / trình duyệt.
- `--no-browser`: không tự mở trình duyệt Dashboard.

### 1. Chạy Proxy gắn vào `legacy-linux` và `reborn-server` đang chạy

```powershell
dotnet run --project d:\Projects\jx-reborn\workbench\qa\JXReborn.WireParityProxy -- `
  --legacy-host 127.0.0.1 --legacy-port 15622 `
  --reborn-host 127.0.0.1 --reborn-port 15623 `
  --bishop-port 16622 `
  --gs-port 16668 `
  --dashboard-port 5100
```

Sau đó:
1. Mở Dashboard tại `http://127.0.0.1:5100`.
2. Trỏ **1 `game.exe` duy nhất** vào `127.0.0.1:16622` (Bishop Proxy) và đăng nhập/chơi bình thường.
3. Bấm **Export Report** để xuất `findings.jsonl`, `report.md`, `report.html`, hoặc bấm nút **L3A+** ở từng bước để xuất `*.capture.json`.

### 2. Các tham số CLI chính

| Tham số | Mặc định | Ý nghĩa |
| :--- | :--- | :--- |
| `--bind-host` | `127.0.0.1` | Địa chỉ bind của Proxy và Dashboard |
| `--bishop-port` | `16622` | Cổng Bishop L7 Proxy nhận kết nối từ `game.exe` |
| `--gs-port` | `16668` | Cổng GameServer L7 Proxy (tự rewrite vào gói `0x34`) |
| `--dashboard-port` | `5100` | Cổng Web Dashboard & REST API |
| `--legacy-host` / `--legacy-port` | `127.0.0.1:15622` | Endpoint Bishop của `legacy-linux` |
| `--reborn-host` / `--reborn-port` | `127.0.0.1:15623` | Endpoint Bishop của `reborn-server` |
| `--keytable` | *(auto-discover)* | Đường dẫn file `legacy-public-keys.bin` (hoặc env `LEGACY_KEYTABLE_PATH`) |
| `--allow-synthetic-keys` | `false` | Cho phép dùng synthetic key table khi test cô lập không có `game.exe` thật |
| `--swap-primary` | `false` | Đảo `reborn-server` làm Primary và `legacy-linux` làm Shadow |
| `--action-window-ms` | `350` | Cửa sổ thời gian (ms) gom phản hồi S2C cho mỗi action C2S |
| `--report-dir` | `wire-parity-reports` | Thư mục lưu báo cáo và capture xuất ra |
| `--mcp` | `false` | Chạy như MCP Server (`stdin`/`stdout` JSON-RPC 2.0) để AI Agent tự điều khiển game & đọc findings |

## Chế độ AI Agent (MCP Server)

Chạy kèm `--mcp` (có thể kết hợp `--standalone --headless`) để expose 8 MCP tools (`get_proxy_status`, `get_world_state`, `perform_action`, `get_proxy_findings`, `get_action_steps`, `export_l3a_capture`, `export_report`, `reset_drift`):

```powershell
dotnet run --project d:\Projects\jx-reborn\workbench\qa\JXReborn.WireParityProxy -- --standalone --headless --mcp
```

## REST API trên Dashboard (`:5100`)

- `GET /` — Giao diện Web Dashboard live.
- `GET /api/status` — Trạng thái kết nối, bộ đếm frame C2S/S2C, số lỗi & trạng thái Drift.
- `GET /api/findings` — Danh sách `ParityFinding` từ cả 3 tầng.
- `GET /api/steps` — Timeline các Action Window đã đóng.
- `POST /api/drift/reset` — Reset cờ cảnh báo Drift sau khi resync nhân vật.
- `POST /api/report/export` — Xuất `findings.jsonl`, `report.md`, `report.html`.
- `POST /api/capture/export/{stepIndex}` — Xuất step tương ứng thành `*.capture.json` đã ký SHA-256.
