# JXReborn Dual-Run Parity Server (`JXReborn.DualParityServer`)

Server trung gian & bộ điều phối kiểm thử đối chiếu (Differential Testing) chạy song song 2 `game.exe` kết nối đồng thời vào **`legacy-linux`** và **`reborn-server`**, hỗ trợ:

1. **Dual TCP Proxy Relay:** Lắng nghe 2 cổng trung gian (`--legacy-proxy-port 16622` → `legacy-linux`, `--reborn-proxy-port 26622` → `reborn-server`) và thống kê lưu lượng C2S/S2C theo thời gian thực.
2. **Interactive Mirror (Chơi 1 tay, test 2 server):** Theo dõi thao tác trên `game.exe #1` (`legacy-linux`), dịch thành hành động ngữ nghĩa (di chuyển, click NPC theo tên, chọn dòng thoại, xác nhận thoại, dùng item, đổi PK) và nhân bản sang `game.exe #2` (`reborn-server`) kèm so sánh live.
3. **Scripted Scenario Runner & Auto NPC Crawler:** Chạy kịch bản JSON lockstep hoặc tự động càn quét toàn bộ NPC đối thoại gần nhân vật trên cả 2 server, tự đóng/resync thoại và chấm điểm parity từng NPC.
4. **Web Dashboard + Báo cáo cho người & AI Agent:**
   - Dashboard tại `http://127.0.0.1:5099`
   - Tự xuất 3 định dạng vào `artifacts/dual-parity-reports/<sessionId>/`:
     - `findings.jsonl` (chuẩn cấu trúc cho AI Agent phân tích sau này)
     - `report.md` (tóm tắt Markdown + bảng điểm NPC + gom cụm lỗi)
     - `report.html` (báo cáo trực quan dark-mode)

## Cách chạy nhanh

### 1. Chế độ Dashboard + Attach 2 cửa sổ `game.exe` đang mở
```powershell
dotnet run --project d:\Projects\jx-reborn\workbench\qa\JXReborn.DualParityServer -- `
  --legacy-host 127.0.0.1 --legacy-port 15622 `
  --reborn-host 127.0.0.1 --reborn-port 5622 `
  --dashboard-port 5099
```
Mở trình duyệt tại `http://127.0.0.1:5099`, nhập PID của 2 `game.exe` (hoặc truyền `--legacy-pid <pid1> --reborn-pid <pid2>`), rồi bấm **Start Interactive Mirror** hoặc **Crawl Nearby NPCs**.

### 2. Tự động Spawn 2 `game.exe` + Login qua Proxy trung gian
```powershell
dotnet run --project d:\Projects\jx-reborn\workbench\qa\JXReborn.DualParityServer -- `
  --spawn --mode interactive `
  --legacy-host 127.0.0.1 --legacy-port 15622 `
  --reborn-host 127.0.0.1 --reborn-port 5622 `
  --account gm001 --password 123456 --role ParityHero01
```

### 3. Chạy kịch bản tự động (CLI / Headless)
```powershell
dotnet run --project d:\Projects\jx-reborn\workbench\qa\JXReborn.DualParityServer -- `
  --spawn --headless --mode scenario `
  --scenario d:\Projects\jx-reborn\workbench\qa\JXReborn.DualParityServer\Scenarios\sample-balang-npcs.json
```
