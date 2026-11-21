# JXReborn Wire Parity Proxy Tests (`JXReborn.WireParityProxy.Tests`)

Bộ kiểm thử tự động (xUnit v3, `net10.0`) xác minh toàn bộ lõi của [`JXReborn.WireParityProxy`](../JXReborn.WireParityProxy/README.md) **không cần Docker và không cần `game.exe`** (chạy ~0.6s trên máy local hoặc CI).

## Phạm vi kiểm thử

| Thư mục / File | Nội dung kiểm chứng |
| :--- | :--- |
| `Wire/JxWireConnectionTests.cs` | Bắt tay `0x20` hai chiều, trích xuất khóa obfuscated và mã hóa/giải mã round-trip bằng `LegacyStreamCipher` với synthetic key table. |
| `Proxy/BishopAndGameServerShadowProxyTests.cs` | Luồng TCP thật đầu-cuối: 1 client nối vào `BishopShadowProxy` → 2 Bishop servers trả 2 `Guid` & 2 GS port khác nhau → xác minh rewrite `0x34` về `GameServerShadowProxy.BoundPort`, map `Guid_L <-> Guid_R`, gửi `0x42` đúng GUID riêng cho từng GS, giữ chung `(ClientToken1, ClientToken2)` trên `0x43`, và xác minh client chỉ nhận S2C từ Primary (cô lập hoàn toàn S2C của Shadow). |
| `Translation/EntityIdTranslatorAndRewriterTests.cs` | Dịch động `NpcId` trên `c2s_dialognpc` (`0x6E`) kèm tính lại `dwCheckValue` hợp lệ qua `MovementAntiCheatConsts.IsValidCheckValue`, và phát hiện `TranslationMiss` khi Shadow thiếu NPC. |
| `Diffing/ProtocolSafetyGateAndWireDifferTests.cs` | Tầng A (`ProtocolSafetyGate` bắt frame S2C sai kích thước thành `Critical` finding), Tầng B (`ActionWindowWireDiffer` phát hiện thiếu S2C opcode và lọc bỏ `s2c_ping`), và `StateDriftDetector` (phát hiện lệch `MapId`). |
| `Reporting/WireParityReportAndExportTests.cs` | Xuất đủ `findings.jsonl`, `report.md`, `report.html` và xác minh file `*.capture.json` xuất từ `L3aCaptureExporter` vượt qua kiểm tra toàn vẹn SHA-256 của `WireCaptureStore.LoadAndVerify(...)`. |

## Cách chạy test

```powershell
# Chạy toàn bộ test của JXReborn.WireParityProxy.Tests
dotnet test d:\Projects\jx-reborn\workbench\qa\JXReborn.WireParityProxy.Tests\JXReborn.WireParityProxy.Tests.csproj

# Chạy lọc theo 1 nhóm test cụ thể
dotnet test d:\Projects\jx-reborn\workbench\qa\JXReborn.WireParityProxy.Tests\JXReborn.WireParityProxy.Tests.csproj --filter "FullyQualifiedName~BishopAndGameServerShadowProxyTests"
```
