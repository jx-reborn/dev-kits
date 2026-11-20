# Quickstart: Spec 017 Multi-Currency NPC Shops & Equipment Repair

## Build & Test Commands

1. **Build Solution (Release)**:
   ```powershell
   dotnet build JXReborn.slnx -c Release
   ```

2. **Run Protocol Tests**:
   ```powershell
   dotnet test tests/JXReborn.Protocol.Tests -c Release --no-build --filter "FullyQualifiedName~NpcShopGoldenVectorPacketTests"
   ```

3. **Run ZoneEngine Unit Tests**:
   ```powershell
   dotnet test tests/JXReborn.ZoneEngine.Tests -c Release --no-build --filter "FullyQualifiedName~NpcShop"
   ```

4. **Run Live Differential Parity Journey J09 (via standalone Oracle container)**:
   ```powershell
   .\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParityNpcShopTests"
   ```
