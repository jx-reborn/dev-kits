# Quickstart: Spec 022 Personal City Stalls & Market Buyout

## Build & Test Commands

1. **Build Solution (Release)**:
   ```powershell
   dotnet build JXReborn.slnx -c Release
   ```

2. **Run Protocol Tests**:
   ```powershell
   dotnet test tests/JXReborn.Protocol.Tests -c Release --no-build --filter "FullyQualifiedName~PlayerStallGoldenVectorPacketTests"
   ```

3. **Run ZoneEngine Stall Tests**:
   ```powershell
   dotnet test tests/JXReborn.ZoneEngine.Tests -c Release --no-build --filter "FullyQualifiedName~PlayerStall"
   ```

4. **Run Live Differential Parity Journey J14 (via standalone Oracle container)**:
   ```powershell
   .\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParityPlayerStallTests"
   ```
