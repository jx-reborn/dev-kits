# Quickstart: Spec 018 Repository Box, Secondary Password & Revival Binding

## Build & Test Commands

1. **Build Solution (Release)**:
   ```powershell
   dotnet build JXReborn.slnx -c Release
   ```

2. **Run Protocol Tests**:
   ```powershell
   dotnet test tests/JXReborn.Protocol.Tests -c Release --no-build --filter "FullyQualifiedName~RepositoryBoxGoldenVectorPacketTests"
   ```

3. **Run ZoneEngine Storage Tests**:
   ```powershell
   dotnet test tests/JXReborn.ZoneEngine.Tests -c Release --no-build --filter "FullyQualifiedName~Repository"
   ```

4. **Run Live Differential Parity Journey J10 (via standalone Oracle container)**:
   ```powershell
   .\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParityRepositoryBoxTests"
   ```
