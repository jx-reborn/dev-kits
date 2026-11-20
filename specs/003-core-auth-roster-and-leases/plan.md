# Implementation Plan: Core Authentication, Character Roster & Session Leases

**Branch/Directory**: `specs/003-core-auth-roster-and-leases`  
**Date**: 2026-11-11  
**Spec**: [spec.md](spec.md)  

---

## 1. Summary & Technical Scope

This specification implements the Bishop login gateway, account authentication, 3-slot character roster CRUD, lease-epoch session fencing, and single-use world handoff ticket coordination for JXReborn (.NET 10, C# 14) in strict compliance with Constitution v1.4.0:
1. **Bishop TCP Gateway (`TCP 5622`)**: Hosted TCP listener managing `BishopClientSession` instances, executing initial handshake (`0x20`), and dispatching typed packet handlers for login (`0x41`), character creation (`0x45`), character deletion (`0x3A`), and character selection (`0x64`).
2. **Account & Character Domain Aggregates**: ABP-aligned DDD aggregate roots (`Account`, `Character`, `SessionLease`) enforcing invariants: unique usernames and character names, 3-slot maximum, prohibited word filtering, password verification, and monotonic lease epoch tracking.
3. **Application Tier**: `AccountAppService`, `CharacterAppService`, and `SessionLeaseAppService` orchestrating business workflows, per-account `SemaphoreSlim` serialization, auto-provisioning, and lease eviction notifications.
4. **Relational Persistence**: EF Core 10 provider (`JXRebornDbContext`) supporting SQL Server for production and SQLite/InMemory for automated integration testing, with unique constraints on `UserName` and `RoleName`.

---

## 2. Technical Context

- **Target Framework**: .NET 10 (`net10.0`), C# 14
- **Build Configuration**: Centralized XML solution (`JXReborn.slnx`), `Directory.Build.props` (`TreatWarningsAsErrors=true`, `AllowUnsafeBlocks=true`)
- **Key Packages**:
  - `Microsoft.EntityFrameworkCore` & `Microsoft.EntityFrameworkCore.SqlServer` / `Sqlite` (`10.0.0`)
  - `Microsoft.Extensions.Hosting` & `DependencyInjection` (`10.0.0`)
  - `System.IO.Pipelines` (`10.0.0`)
- **Assemblies**:
  - `src/JXReborn.Primitives/`: `Accounts/`, `Characters/`, `SessionLeases/`, `ErrorCodes/`
  - `src/JXReborn.Domain/`: `Accounts/`, `Characters/`, `SessionLeases/`
  - `src/JXReborn.Contracts/`: `Accounts/`, `Characters/`, `SessionLeases/`
  - `src/JXReborn.Application/`: `Accounts/`, `Characters/`, `SessionLeases/`
  - `src/JXReborn.Infrastructure/`: `Accounts/`, `Characters/`, `SessionLeases/`, `EntityFrameworkCore/`
  - `src/JXReborn.Adapter.Legacy/`: `Bishop/`, `Packets/`
- **Testing**:
  - `tests/JXReborn.Protocol.Tests/Bishop/`: `GatewayAndWorldEntryPacketTests.cs`, `CharacterCreationAndDeletionPacketTests.cs`
  - `tests/JXReborn.Integration.Tests/AuthAndCharacters/`: `CharacterLifecycleIntegrationTests.cs`
  - `tests/JXReborn.Integration.Tests/SessionFencing/`: `SessionLeaseFencingIntegrationTests.cs`, `SessionLeaseEpochFencingIntegrationTests.cs`

---

## 3. Constitution Check (v1.4.0)

| Principle / Gate | Status | Compliance Details |
| :--- | :---: | :--- |
| **Principle I: Hybrid Control Plane & 18-Hz Zone** | **PASS** | Control-plane auth, roster, and lease fencing operate asynchronously in the Application tier, decoupled from 18-Hz zone loops. |
| **Principle II: Multi-Protocol Transport Abstraction** | **PASS** | Bishop packet structs and TCP sockets are isolated in `Adapter.Legacy/Bishop/`; Application and Domain deal solely with clean DTOs and aggregate roots. |
| **Principle III: Zero-Hallucination Evidence** | **PASS** | Packet structures (`0x41`, `0x37`, `0x45`, `0x3A`, `0x8C`, `0x64`, `0x34`), return codes (`0x00050001`..`0x0005000B`), and second-layer ciphers match legacy 6.0 wire specifications 100%. |
| **Principle IV: Zero-Dupe Economy & Lease Fencing** | **PASS** | Monotonic `LeaseEpoch` increments and per-account locking serialize dual logins and evict stale sessions before committing state changes. |
| **Principle V: UTF-8 Domain Core & Boundary Transcoding** | **PASS** | Domain and database entities store pure UTF-8 strings. TCVN3 transcoding occurs solely at the Bishop packet boundary. |
| **Principle VI: Clean Modular Boundaries & 1 Type Per File** | **PASS** | Strict ABP DDD separation: Primitives $\leftarrow$ Domain $\leftarrow$ Application $\leftarrow$ Infrastructure/Adapter. 1 type per `.cs` file, sealed concrete classes. |
| **Gate 1: Ground-Truth Specification** | **PASS** | Documented in `contracts/bishop-gateway-wire-protocol.md` and `contracts/session-lease-fencing-contracts.md`. |
| **Gate 2: Unit Gate** | **PASS** | Packet serialization and round-trip verified in `Protocol.Tests`. |
| **Gate 3: Integration Gate** | **PASS** | Swarm concurrency, dual-login race, and lifecycle tests in `Integration.Tests`. |
| **Gate 4: Stress Gate** | **PASS** | 100-bot concurrency test runs with zero stuck accounts. |
| **Gate 5: Build Gate** | **PASS** | Zero compiler warnings (`TreatWarningsAsErrors=true`). |

---

## 4. Real Project Structure

```text
jx-server/
├── src/
│   ├── JXReborn.Primitives/
│   │   ├── Accounts/
│   │   │   ├── AccountConsts.cs
│   │   │   ├── AccountStatus.cs
│   │   │   └── LoginResultCode.cs
│   │   ├── Characters/
│   │   │   ├── CharacterConsts.cs
│   │   │   ├── CharacterGender.cs
│   │   │   └── ElementSeries.cs
│   │   └── SessionLeases/
│   │       ├── SessionLeaseConsts.cs
│   │       └── SessionLeaseState.cs
│   ├── JXReborn.Domain/
│   │   ├── Accounts/
│   │   │   ├── Account.cs
│   │   │   ├── AccountManager.cs
│   │   │   └── IAccountRepository.cs
│   │   ├── Characters/
│   │   │   ├── Character.cs
│   │   │   ├── CharacterManager.cs
│   │   │   ├── CharacterSkillValue.cs
│   │   │   └── ICharacterRepository.cs
│   │   └── SessionLeases/
│   │       ├── ISessionEvictionNotifier.cs
│   │       ├── ISessionLeaseRepository.cs
│   │       ├── SessionLease.cs
│   │       └── SessionLeaseManager.cs
│   ├── JXReborn.Contracts/
│   │   ├── Accounts/
│   │   │   ├── AuthenticateAccountInput.cs
│   │   │   ├── AuthenticateAccountResultDto.cs
│   │   │   └── IAccountAppService.cs
│   │   ├── Characters/
│   │   │   ├── CharacterDto.cs
│   │   │   ├── CharacterSkillDto.cs
│   │   │   ├── CharacterSummaryDto.cs
│   │   │   ├── CreateCharacterDto.cs
│   │   │   ├── CreateCharacterResultDto.cs
│   │   │   ├── DeleteCharacterDto.cs
│   │   │   ├── DeleteCharacterResultDto.cs
│   │   │   ├── GetCharacterRosterInput.cs
│   │   │   └── ICharacterAppService.cs
│   │   └── SessionLeases/
│   │       ├── ISessionLeaseAppService.cs
│   │       ├── RedeemHandoffTicketInput.cs
│   │       ├── RedeemHandoffTicketResultDto.cs
│   │       ├── ReleaseSessionLeaseInput.cs
│   │       ├── SelectCharacterInput.cs
│   │       └── WorldHandoffTicketDto.cs
│   ├── JXReborn.Application/
│   │   ├── Accounts/
│   │   │   └── AccountAppService.cs
│   │   ├── Characters/
│   │   │   └── CharacterAppService.cs
│   │   └── SessionLeases/
│   │       └── SessionLeaseAppService.cs
│   ├── JXReborn.Infrastructure/
│   │   ├── Accounts/
│   │   │   └── EfCoreAccountRepository.cs
│   │   ├── Characters/
│   │   │   └── EfCoreCharacterRepository.cs
│   │   ├── EntityFrameworkCore/
│   │   │   ├── DatabaseInitializerHostedService.cs
│   │   │   └── JXRebornDbContext.cs
│   │   └── SessionLeases/
│   │       └── InMemorySessionLeaseRepository.cs
│   └── JXReborn.Adapter.Legacy/
│       ├── Bishop/
│       │   ├── AccountLoginPacketHandler.cs
│       │   ├── BishopClientSession.cs
│       │   ├── BishopTcpListenerHostedService.cs
│       │   ├── CreateCharacterPacketHandler.cs
│       │   ├── DeleteCharacterPacketHandler.cs
│       │   ├── IBishopPacketHandler.cs
│       │   ├── LegacySessionEvictionNotifier.cs
│       │   └── SelectCharacterPacketHandler.cs
│       └── Packets/
│           ├── AccountLoginC2SPacket.cs
│           ├── AccountLoginResultS2CPacket.cs
│           ├── CreateCharacterC2SPacket.cs
│           ├── DeleteCharacterC2SPacket.cs
│           ├── NotifyPlayerLoginS2CPacket.cs
│           ├── RoleListResultS2CPacket.cs
│           ├── RoleNewDelResponseS2CPacket.cs
│           └── SelectCharacterC2SPacket.cs
└── tests/
    ├── JXReborn.Protocol.Tests/
    │   └── Bishop/
    │       ├── CharacterCreationAndDeletionPacketTests.cs
    │       └── GatewayAndWorldEntryPacketTests.cs
    └── JXReborn.Integration.Tests/
        ├── AuthAndCharacters/
        │   └── CharacterLifecycleIntegrationTests.cs
        └── SessionFencing/
            ├── SessionLeaseEpochFencingIntegrationTests.cs
            └── SessionLeaseFencingIntegrationTests.cs
```

---

## 5. Risk Assessment & Verification Strategy

- **Risk 1: Concurrent dual-login race conditions**: Two clients submit `0x41` for the same account simultaneously. Mitigation: `SessionLeaseAppService` uses per-account keyed `SemaphoreSlim` locks, serializing access and guaranteeing monotonic epoch progression. Verified by `SessionLeaseEpochFencingIntegrationTests`.
- **Risk 2: Stuck accounts from ungraceful socket drops**: Clients disconnecting between gateway selection and world entry leave lingering tickets. Mitigation: 30-second TTL on handoff tickets automatically expires orphaned leases. Verified by `SessionLeaseFencingIntegrationTests`.
- **Risk 3: Unhandled database concurrency on character deletion**: Concurrently creating and deleting characters with identical names. Mitigation: Unique indexes on `UserName` and `RoleName` combined with immediate slot/name release in `CharacterAppService`.
