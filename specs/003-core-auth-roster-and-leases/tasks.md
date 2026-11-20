# Tasks: Core Authentication, Character Roster & Session Leases

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)  

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (`[US1]`, `[US2]`, `[US3]`)
- Every task specifies the exact target file path(s) relative to the repository root

---

## Phase 1: Primitives & Domain Entities (Shared Scaffolding)

- [X] T001 [P] [US1] Define account constants, status enum, and login result codes in `src/JXReborn.Primitives/Accounts/` (`AccountConsts.cs`, `AccountStatus.cs`, `LoginResultCode.cs`)
- [X] T002 [P] [US2] Define character constants, gender enum, and element series in `src/JXReborn.Primitives/Characters/` (`CharacterConsts.cs`, `CharacterGender.cs`, `ElementSeries.cs`)
- [X] T003 [P] [US3] Define session lease constants and lease state in `src/JXReborn.Primitives/SessionLeases/` (`SessionLeaseConsts.cs`, `SessionLeaseState.cs`)
- [X] T004 [P] [US1] Implement `Account` aggregate root, `AccountManager`, and `IAccountRepository` in `src/JXReborn.Domain/Accounts/` (`Account.cs`, `AccountManager.cs`, `IAccountRepository.cs`)
- [X] T005 [P] [US2] Implement `Character` aggregate root, `CharacterManager`, `CharacterSkillValue`, and `ICharacterRepository` in `src/JXReborn.Domain/Characters/` (`Character.cs`, `CharacterManager.cs`, `CharacterSkillValue.cs`, `ICharacterRepository.cs`)
- [X] T006 [P] [US3] Implement `SessionLease` aggregate root, `SessionLeaseManager`, `ISessionLeaseRepository`, and `ISessionEvictionNotifier` in `src/JXReborn.Domain/SessionLeases/` (`SessionLease.cs`, `SessionLeaseManager.cs`, `ISessionLeaseRepository.cs`, `ISessionEvictionNotifier.cs`)

---

## Phase 2: Contracts & Application Services

- [X] T007 [P] [US1] Create account DTOs and `IAccountAppService` in `src/JXReborn.Contracts/Accounts/` (`AuthenticateAccountInput.cs`, `AuthenticateAccountResultDto.cs`, `IAccountAppService.cs`)
- [X] T008 [P] [US2] Create character DTOs and `ICharacterAppService` in `src/JXReborn.Contracts/Characters/` (`GetCharacterRosterInput.cs`, `CharacterSummaryDto.cs`, `CreateCharacterDto.cs`, `CreateCharacterResultDto.cs`, `DeleteCharacterDto.cs`, `DeleteCharacterResultDto.cs`, `ICharacterAppService.cs`)
- [X] T009 [P] [US3] Create session lease DTOs and `ISessionLeaseAppService` in `src/JXReborn.Contracts/SessionLeases/` (`SelectCharacterInput.cs`, `WorldHandoffTicketDto.cs`, `RedeemHandoffTicketInput.cs`, `RedeemHandoffTicketResultDto.cs`, `ReleaseSessionLeaseInput.cs`, `ISessionLeaseAppService.cs`)
- [X] T010 [US1] Implement `AccountAppService` with auto-provisioning support in `src/JXReborn.Application/Accounts/AccountAppService.cs`
- [X] T011 [US2] Implement `CharacterAppService` with starter village fallback and deletion password verification in `src/JXReborn.Application/Characters/CharacterAppService.cs`
- [X] T012 [US3] Implement `SessionLeaseAppService` with monotonic lease-epoch fencing and per-account locking in `src/JXReborn.Application/SessionLeases/SessionLeaseAppService.cs`

---

## Phase 3: Infrastructure Persistence & EF Core 10

- [X] T013 [P] [US1] Configure EF Core 10 entity mappings for `Accounts` and `Characters` with unique constraints in `src/JXReborn.Infrastructure/EntityFrameworkCore/JXRebornDbContext.cs`
- [X] T014 [P] [US1] Implement `EfCoreAccountRepository` in `src/JXReborn.Infrastructure/Accounts/EfCoreAccountRepository.cs`
- [X] T015 [P] [US2] Implement `EfCoreCharacterRepository` in `src/JXReborn.Infrastructure/Characters/EfCoreCharacterRepository.cs`
- [X] T016 [P] [US3] Implement thread-safe `InMemorySessionLeaseRepository` in `src/JXReborn.Infrastructure/SessionLeases/InMemorySessionLeaseRepository.cs`

---

## Phase 4: Bishop Gateway Wire Packets & Protocol Handlers

- [X] T017 [P] [US1] Implement Bishop wire packet structs (`AccountLoginC2SPacket`, `AccountLoginResultS2CPacket`, `RoleListResultS2CPacket`) in `src/JXReborn.Adapter.Legacy/Packets/`
- [X] T018 [P] [US2] Implement character creation and deletion packet structs (`CreateCharacterC2SPacket`, `DeleteCharacterC2SPacket`, `RoleNewDelResponseS2CPacket`) in `src/JXReborn.Adapter.Legacy/Packets/`
- [X] T019 [P] [US3] Implement character selection and world handoff packet structs (`SelectCharacterC2SPacket`, `NotifyPlayerLoginS2CPacket`) in `src/JXReborn.Adapter.Legacy/Packets/`
- [X] T020 [P] [US1] Implement `BishopClientSession` and `AccountLoginPacketHandler` in `src/JXReborn.Adapter.Legacy/Bishop/`
- [X] T021 [P] [US2] Implement `CreateCharacterPacketHandler` and `DeleteCharacterPacketHandler` in `src/JXReborn.Adapter.Legacy/Bishop/`
- [X] T022 [P] [US3] Implement `SelectCharacterPacketHandler` and `LegacySessionEvictionNotifier` in `src/JXReborn.Adapter.Legacy/Bishop/`
- [X] T023 [US1] Implement `BishopTcpListenerHostedService` (port 5622) in `src/JXReborn.Adapter.Legacy/Bishop/BishopTcpListenerHostedService.cs`

---

## Phase 5: Verification & Integration Tests

- [X] T024 [P] [US1] Write unit tests for gateway packet serialization in `tests/JXReborn.Protocol.Tests/Bishop/GatewayAndWorldEntryPacketTests.cs`
- [X] T025 [P] [US2] Write unit tests for character creation and deletion packets in `tests/JXReborn.Protocol.Tests/Bishop/CharacterCreationAndDeletionPacketTests.cs`
- [X] T026 [P] [US2] Write integration tests for 5-element character creation, prohibited name rejection, and deletion in `tests/JXReborn.Integration.Tests/AuthAndCharacters/CharacterLifecycleIntegrationTests.cs`
- [X] T027 [P] [US3] Write adversarial integration tests for simultaneous dual-login races and lease-epoch fencing in `tests/JXReborn.Integration.Tests/SessionFencing/SessionLeaseEpochFencingIntegrationTests.cs`
- [X] T028 [US3] Write integration tests for 100-bot swarm concurrency and zero-stuck-account verification in `tests/JXReborn.Integration.Tests/SessionFencing/SessionLeaseFencingIntegrationTests.cs`
