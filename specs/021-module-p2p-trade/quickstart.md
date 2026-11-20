# Quickstart: Spec 021 Two-Phase Zero-Dupe P2P Player Trade

## Build & Test Commands

1. **Build Solution (Release)**:
   ```powershell
   dotnet build JXReborn.slnx -c Release
   ```

2. **Run Protocol Tests**:
   ```powershell
   dotnet test tests/JXReborn.Protocol.Tests -c Release --no-build --filter "FullyQualifiedName~PlayerTradeGoldenVectorPacketTests"
   ```

3. **Run ZoneEngine Trade Tests**:
   ```powershell
   dotnet test tests/JXReborn.ZoneEngine.Tests -c Release --no-build --filter "FullyQualifiedName~Trade"
   ```

4. **Run Live Differential Parity Journey J13 (via standalone Oracle container)**:
   ```powershell
   .\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParityPlayerTradeTests"
   ```
