# Implementation Plan: Core Legacy Wire Protocol & Ciphers

**Branch/Directory**: `specs/002-core-legacy-wire-protocol`  
**Date**: 2026-11-11  
**Spec**: [spec.md](spec.md)  

---

## 1. Summary & Technical Scope

This specification establishes the wire framing, stream encryption, secondary obfuscation, and boundary character transcoding infrastructure required for byte-level compatibility with legacy 6.0 client binaries and autonomous test swarms (`jx-swarm`):
1. **High-Performance TCP Framing (`JxFramePipeProcessor`)**: Built with `System.IO.Pipelines` to handle 2-byte length-prefixed framing (`wFrameLength = PayloadLength + 2`) with zero-copy buffer slicing, handling packet fragmentation, batching, and bounds validation (`ProtocolSizeTable`).
2. **Handshake Key Obfuscation (`HandshakeKeyObfuscator`)**: Exact mathematical implementation of the 32-bit key obfuscator used in the unencrypted 44-byte `AccountBeginS2CPacket` (`0x20`).
3. **Rolling Stream Cipher (`LegacyStreamCipher`)**: Bit-exact rolling XOR stream cipher operating over the externally loaded 5,679-entry `LegacyKeyTable` with per-packet LCG rotation.
4. **Second-Layer XOR Cipher (`SecondLayerXorCipher`)**: Secondary 16-bit linear congruential masking for role list (`0x37`, seed `0x3042`) and role selection (`0x64`, seed `0x38BC`).
5. **Boundary-Only Transcoding (`Tcvn3Encoding`, `GbkEncoding`, `LegacyPasswordHasher`)**: Fast, allocation-conscious transcoding between legacy single-byte encodings and standard .NET UTF-8 strings.

---

## 2. Technical Context

- **Target Framework**: .NET 10 (`net10.0`), C# 14
- **Build Configuration**: Centralized XML solution (`JXReborn.slnx`), `Directory.Build.props` (`TreatWarningsAsErrors=true`, `AllowUnsafeBlocks=true`, `AnalysisLevel=latest`)
- **Key Packages & Core APIs**:
  - `System.IO.Pipelines` (`10.0.0`) — Low-allocation TCP stream processing
  - `System.Buffers` & `System.Buffers.Binary` (`BinaryPrimitives`) — Endian-safe Little-Endian manipulation
  - `System.Text.Encoding.CodePages` (`10.0.0`) — Underlying CP936 (GBK) code page provider
- **Assemblies**:
  - `src/JXReborn.Primitives/`: `Encodings/` (`Tcvn3Encoding.cs`, `GbkEncoding.cs`, `LegacyPasswordHasher.cs`)
  - `src/JXReborn.Adapter.Legacy/`: `Framing/` (`JxFramePipeProcessor.cs`, `ProtocolSizeTable.cs`), `Ciphers/` (`LegacyKeyTable.cs`, `HandshakeKeyObfuscator.cs`, `LegacyStreamCipher.cs`, `SecondLayerXorCipher.cs`), `Packets/` (`AccountBeginS2CPacket.cs`, `ProtocolOpcodes.cs`)
- **Testing**: `tests/JXReborn.Protocol.Tests/` (`FramingAndCipherTests.cs`)

---

## 3. Constitution Check (v1.4.0)

| Principle / Gate | Status | Compliance Details |
| :--- | :---: | :--- |
| **Principle I: Hybrid Control Plane & 18-Hz Zone** | **PASS** | Transport framing operates asynchronously on I/O threads; decoupled from zone ticks. |
| **Principle II: Multi-Protocol Transport Abstraction** | **PASS** | All legacy packet headers, ciphers, and framing logic remain strictly isolated inside `JXReborn.Adapter.Legacy` and `JXReborn.Primitives.Encodings`. |
| **Principle III: Zero-Hallucination Evidence** | **PASS** | Cipher routines, key constants, table validation, and bit-level obfuscation math are verified against legacy 6.0 wire compatibility vectors. |
| **Principle IV: Zero-Dupe Economy** | **PASS** | Framing layer provides reliable, non-duplicating packet delivery to the application tier. |
| **Principle V: UTF-8 Domain Core & Boundary Transcoding** | **PASS** | Transcoding occurs strictly at socket ingress/egress boundaries. Internal strings are pure UTF-8. |
| **Principle VI: Clean Modular Boundaries & 1 Type Per File** | **PASS** | All classes are `sealed` concrete types in single files within pluralized feature directories. |
| **Gate 1: Ground-Truth Specification** | **PASS** | Formally documented framing contracts and cipher vectors in `contracts/`. |
| **Gate 2: Unit Gate** | **PASS** | Verified via `FramingAndCipherTests.cs` (59/59 passing). |
| **Gate 3: Integration Gate** | **PASS** | Loopback bot integration tests in `JXReborn.Integration.Tests` verify full handshake. |
| **Gate 4: Stress Gate** | **PASS** | Zero-copy pipeline parsing prevents memory degradation under high packet rates. |
| **Gate 5: Build Gate** | **PASS** | Compiles with `TreatWarningsAsErrors=true` (0 warnings). |

---

## 4. Real Project Structure

```text
jx-server/
├── artifacts/keys/legacy-public-keys.bin                       # External operator-supplied 5,679 uint32 key table (22,716B, gitignored)
├── src/
│   ├── JXReborn.Primitives/
│   │   └── Encodings/
│   │       ├── GbkEncoding.cs                                  # GBK (CP936) transcoder with CJK support
│   │       ├── LegacyPasswordHasher.cs                         # 32-char uppercase MD5 password digest
│   │       └── Tcvn3Encoding.cs                                # TCVN3 <-> UTF-8 bidirectional table transcoder
│   └── JXReborn.Adapter.Legacy/
│       ├── Ciphers/
│       │   ├── HandshakeKeyObfuscator.cs                       # 32-bit key obfuscation and deobfuscation
│       │   ├── LegacyKeyTable.cs                               # Runtime loader for external 22,716-byte key table
│       │   ├── LegacyStreamCipher.cs                           # In-place rolling XOR stream cipher
│       │   └── SecondLayerXorCipher.cs                         # 0x3042 and 0x38BC 16-bit XOR obfuscators
│       ├── Framing/
│       │   ├── JxFramePipeProcessor.cs                         # Pipelines 2-byte length-prefixed processor
│       │   └── ProtocolSizeTable.cs                            # Opcode size validation rules
│       └── Packets/
│           ├── AccountBeginS2CPacket.cs                        # 0x20 unencrypted 44-byte handshake frame
│           └── ProtocolOpcodes.cs                              # Authoritative opcode constants
└── tests/
    └── JXReborn.Protocol.Tests/
        └── FramingAndCipherTests.cs                            # Unit tests for framing, ciphers, and encodings
```

---

## 5. Risk Assessment & Verification Strategy

- **Risk 1: Stream cipher key desynchronization**: Missing a packet or miscounting processed bytes causes all subsequent packets to decrypt into garbage. Mitigation: Unit tests in `FramingAndCipherTests` verify coalesced and fragmented packet sequences with exact key rotation assertions.
- **Risk 2: Buffer overruns on unaligned packet lengths**: Payloads with lengths not divisible by 4 require specific 8-bit tail masking. Mitigation: Multi-length parameter tests (`[InlineData(1)]`, `[InlineData(4)]`, `[InlineData(9)]`, `[InlineData(483)]`) in `FramingAndCipherTests`.
- **Risk 3: External key table missing or corrupted**: Any corruption or truncation in the external key table file breaks cipher parity. Mitigation: Validate exact `22,716`-byte length upon startup via `LegacyKeyTable` (`LEGACY_KEYTABLE_PATH` / `LegacyGateway:KeyTablePath`).
