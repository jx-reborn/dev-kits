using System.Buffers.Binary;
using JXReborn.Adapter.Legacy.Packets;
using JXReborn.TestBase.WireCaptures;

namespace JXReborn.ClientDriver.Tests.WireCaptures;

/// <summary>
/// Converts raw decrypted <c>C2S</c> and <c>S2C</c> frames captured inside <c>game.exe</c> against <c>legacy server</c>
/// into symbolic <see cref="WireCaptureC2sFrame"/> patches and semantic <see cref="WireCaptureExpectedS2cEvent"/> contracts.
/// </summary>
public static class WireCaptureAutoSymbolizer
{
    public static IReadOnlyList<WireCaptureC2sFrame> BuildSymbolicC2sFrames(
        EndWireCaptureResult capture,
        IReadOnlyDictionary<string, WireCaptureSymbolDefinition> symbols,
        WorldDomSnapshot preStepState)
    {
        var list = new List<WireCaptureC2sFrame>();

        foreach (CapturedWireFrameSnapshot f in capture.Frames)
        {
            if (!string.Equals(f.Direction, "c2s", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            byte opcode = (byte)f.Opcode;
            if (opcode == ProtocolOpcodes.c2s_ping)
            {
                continue;
            }

            byte[] raw = Convert.FromHexString(f.RawHex);
            var patches = new List<WireCaptureFramePatch>();

            if (opcode == ProtocolOpcodes.c2s_playereatitem && raw.Length == PlayerEatItemC2SPacket.PayloadSize)
            {
                byte eatX = raw[1];
                uint itemDwId = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(2, 4));
                byte eatY = raw[6];
                byte eatPlace = raw[7];

                string? sym = TryMatchSymbolByRecordedDwId(symbols, WireCaptureSymbolKind.Item, itemDwId);
                bool matchedByDwId = sym is not null;
                if (sym is null)
                {
                    InventoryItemSnapshot? cellItem = preStepState.Items.FirstOrDefault(
                        i => i.Place == eatPlace &&
                             eatX >= i.GridX && eatX < i.GridX + Math.Max(1, i.Width) &&
                             eatY >= i.GridY && eatY < i.GridY + Math.Max(1, i.Height));
                    if (cellItem is not null)
                    {
                        sym = TryMatchSymbolByRecordedDwId(symbols, WireCaptureSymbolKind.Item, cellItem.ServerItemId);
                    }
                }

                if (sym is not null)
                {
                    patches.Add(new WireCaptureFramePatch(1, WireCapturePatchKind.UInt8ItemGridX, sym, "EatItem GridX"));
                    if (matchedByDwId)
                    {
                        patches.Add(new WireCaptureFramePatch(2, WireCapturePatchKind.UInt32LeDwId, sym, "EatItem RuntimeItemId"));
                    }

                    patches.Add(new WireCaptureFramePatch(6, WireCapturePatchKind.UInt8ItemGridY, sym, "EatItem GridY"));
                    patches.Add(new WireCaptureFramePatch(7, WireCapturePatchKind.UInt8ItemPlace, sym, "EatItem ContainerPlace"));
                }
            }
            else if (opcode == ProtocolOpcodes.c2s_playermoveitem && raw.Length == PlayerMoveItemC2SPacket.PayloadSize)
            {
                byte downY = raw[1];
                byte downPos = raw[3];
                byte downX = raw[4];
                if (downPos == 3)
                {
                    InventoryItemSnapshot? bagItem = preStepState.Items.FirstOrDefault(
                        i => i.Place == 3 &&
                             downX >= i.GridX && downX < i.GridX + Math.Max(1, i.Width) &&
                             downY >= i.GridY && downY < i.GridY + Math.Max(1, i.Height));
                    if (bagItem is not null)
                    {
                        string? sym = TryMatchSymbolByRecordedDwId(symbols, WireCaptureSymbolKind.Item, (uint)bagItem.ServerItemId);
                        if (sym is not null)
                        {
                            patches.Add(new WireCaptureFramePatch(1, WireCapturePatchKind.UInt8ItemGridY, sym, "MoveItem DownY"));
                            patches.Add(new WireCaptureFramePatch(4, WireCapturePatchKind.UInt8ItemGridX, sym, "MoveItem DownX"));
                        }
                    }
                }
            }
            else if (opcode == ProtocolOpcodes.c2s_npcskill && raw.Length == NpcSkillC2SPacket.PayloadSize)
            {
                int targetMpsX = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(9, 4));
                if (targetMpsX == -1)
                {
                    uint targetDwId = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(1, 4));
                    string? npcSym = TryMatchSymbolByRecordedDwId(symbols, WireCaptureSymbolKind.Npc, targetDwId);
                    if (npcSym is not null)
                    {
                        patches.Add(new WireCaptureFramePatch(1, WireCapturePatchKind.UInt32LeDwId, npcSym, "NpcSkill TargetNpcDwId"));
                    }
                }
            }
            else if (opcode == ProtocolOpcodes.c2s_tradeapplystart && raw.Length >= 5)
            {
                uint targetDwId = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(1, 4));
                string? playerSym = TryMatchSymbolByRecordedDwId(symbols, WireCaptureSymbolKind.PlayerSelf, targetDwId);
                if (playerSym is not null)
                {
                    patches.Add(new WireCaptureFramePatch(1, WireCapturePatchKind.UInt32LeDwId, playerSym, "TradeApplyStart TargetNpcDwId"));
                }
            }
            else if (opcode == ProtocolOpcodes.c2s_teamprotocol && raw.Length >= 8)
            {
                foreach (int off in (ReadOnlySpan<int>)[4, 5])
                {
                    if (raw.Length >= off + 4)
                    {
                        uint candidateId = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(off, 4));
                        string? playerSym = TryMatchSymbolByRecordedDwId(symbols, WireCaptureSymbolKind.PlayerSelf, candidateId);
                        if (playerSym is not null)
                        {
                            patches.Add(new WireCaptureFramePatch(off, WireCapturePatchKind.UInt32LeDwId, playerSym, $"TeamProtocol PlayerDwId@{off}"));
                            break;
                        }
                    }
                }
            }

            int tickOffset = (int)Math.Max(0, f.RelativeTickFrames);
            list.Add(new WireCaptureC2sFrame(tickOffset, opcode, f.RawHex, patches));
        }

        return list;
    }

    public static IReadOnlyList<WireCaptureExpectedS2cEvent> BuildExpectedS2cContract(
        EndWireCaptureResult capture,
        IReadOnlyDictionary<string, WireCaptureSymbolDefinition> symbols,
        bool includePeriodicVitalSyncs,
        int defaultTickToleranceFrames = 3,
        int? initialMana = null,
        string? observerActorId = null,
        IReadOnlySet<byte>? extraTrackedOpcodes = null)
    {
        var events = new List<WireCaptureExpectedS2cEvent>();
        int? lastRecordedMana = initialMana;

        foreach (CapturedWireFrameSnapshot f in capture.Frames)
        {
            if (!string.Equals(f.Direction, "s2c", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            byte opcode = (byte)f.Opcode;
            byte[] raw = Convert.FromHexString(f.RawHex);
            int relTick = (int)Math.Max(1, f.RelativeTickFrames);

            switch (opcode)
            {
                case ProtocolOpcodes.s2c_playermoveitem when raw.Length == PlayerMoveItemS2CPacket.PayloadSize:
                {
                    events.Add(new WireCaptureExpectedS2cEvent(
                        Opcode: opcode,
                        Name: "s2c_playermoveitem",
                        ExactByteLength: PlayerMoveItemS2CPacket.PayloadSize,
                        ExpectedTickOffsetFrames: relTick,
                        TickToleranceFrames: defaultTickToleranceFrames,
                        FieldAssertions:
                        [
                            new WireCaptureFieldAssertion(2, 1, false, WireCaptureFieldMatcherKind.ExactInt64, ExpectedValue: raw[2], FieldName: "DownPos"),
                            new WireCaptureFieldAssertion(6, 1, false, WireCaptureFieldMatcherKind.ExactInt64, ExpectedValue: raw[6], FieldName: "UpPos")
                        ],
                        ObserverActorId: observerActorId));
                    break;
                }

                case ProtocolOpcodes.s2c_itemautomove when raw.Length == ItemAutoMoveS2CPacket.PayloadSize:
                {
                    events.Add(new WireCaptureExpectedS2cEvent(
                        Opcode: opcode,
                        Name: "s2c_itemautomove",
                        ExactByteLength: ItemAutoMoveS2CPacket.PayloadSize,
                        ExpectedTickOffsetFrames: relTick,
                        TickToleranceFrames: defaultTickToleranceFrames,
                        FieldAssertions: [],
                        ObserverActorId: observerActorId));
                    break;
                }

                case ProtocolOpcodes.s2c_removeitem when raw.Length == RemoveItemS2CPacket.PayloadSize:
                {
                    uint removedId = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(1, 4));
                    string? itemSym = TryMatchSymbolByRecordedDwId(symbols, WireCaptureSymbolKind.Item, removedId);
                    var assertions = new List<WireCaptureFieldAssertion>();
                    if (itemSym is not null)
                    {
                        assertions.Add(new WireCaptureFieldAssertion(1, 4, false, WireCaptureFieldMatcherKind.SymbolDwId, Symbol: itemSym, FieldName: "RuntimeItemId"));
                    }

                    events.Add(new WireCaptureExpectedS2cEvent(
                        Opcode: opcode,
                        Name: "s2c_removeitem",
                        ExactByteLength: RemoveItemS2CPacket.PayloadSize,
                        ExpectedTickOffsetFrames: relTick,
                        TickToleranceFrames: defaultTickToleranceFrames,
                        FieldAssertions: assertions,
                        ObserverActorId: observerActorId));
                    break;
                }

                case ProtocolOpcodes.s2c_synccurplayernormal when includePeriodicVitalSyncs && raw.Length == SyncCurPlayerNormalS2CPacket.Size:
                {
                    int mana = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(3, 4));
                    if (lastRecordedMana != mana)
                    {
                        lastRecordedMana = mana;
                        events.Add(new WireCaptureExpectedS2cEvent(
                            Opcode: opcode,
                            Name: "s2c_synccurplayernormal",
                            ExactByteLength: SyncCurPlayerNormalS2CPacket.Size,
                            ExpectedTickOffsetFrames: relTick,
                            TickToleranceFrames: Math.Max(defaultTickToleranceFrames, 5),
                            FieldAssertions:
                            [
                                new WireCaptureFieldAssertion(3, 4, true, WireCaptureFieldMatcherKind.ExactInt64, ExpectedValue: mana, FieldName: "Mana")
                            ],
                            ObserverActorId: observerActorId));
                    }

                    break;
                }

                case ProtocolOpcodes.s2c_skillcast when raw.Length == SkillCastS2CPacket.PayloadSize:
                {
                    uint skillId = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(5, 4));
                    uint casterId = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(13, 4));
                    uint skillLv = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(17, 4));
                    string? casterSym = TryMatchSymbolByRecordedDwId(symbols, WireCaptureSymbolKind.PlayerSelf, casterId);

                    var assertions = new List<WireCaptureFieldAssertion>
                    {
                        new(5, 4, false, WireCaptureFieldMatcherKind.ExactInt64, ExpectedValue: skillId, FieldName: "SkillId"),
                        new(17, 4, false, WireCaptureFieldMatcherKind.ExactInt64, ExpectedValue: skillLv, FieldName: "SkillLevel")
                    };
                    if (casterSym is not null)
                    {
                        assertions.Add(new WireCaptureFieldAssertion(13, 4, false, WireCaptureFieldMatcherKind.SymbolDwId, Symbol: casterSym, FieldName: "CasterNpcId"));
                    }

                    events.Add(new WireCaptureExpectedS2cEvent(
                        Opcode: opcode,
                        Name: "s2c_skillcast",
                        ExactByteLength: SkillCastS2CPacket.PayloadSize,
                        ExpectedTickOffsetFrames: relTick,
                        TickToleranceFrames: defaultTickToleranceFrames,
                        FieldAssertions: assertions,
                        ObserverActorId: observerActorId));
                    break;
                }

                case ProtocolOpcodes.s2c_npcdeath when raw.Length == NpcDeathS2CPacket.PayloadSize:
                {
                    uint deadNpcId = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(1, 4));
                    string? npcSym = TryMatchSymbolByRecordedDwId(symbols, WireCaptureSymbolKind.Npc, deadNpcId);
                    var assertions = new List<WireCaptureFieldAssertion>();
                    if (npcSym is not null)
                    {
                        assertions.Add(new WireCaptureFieldAssertion(1, 4, false, WireCaptureFieldMatcherKind.SymbolDwId, Symbol: npcSym, FieldName: "NpcId"));
                    }

                    events.Add(new WireCaptureExpectedS2cEvent(
                        Opcode: opcode,
                        Name: "s2c_npcdeath",
                        ExactByteLength: NpcDeathS2CPacket.PayloadSize,
                        ExpectedTickOffsetFrames: relTick,
                        TickToleranceFrames: Math.Max(defaultTickToleranceFrames, 10),
                        FieldAssertions: assertions,
                        ObserverActorId: observerActorId));
                    break;
                }

                case ProtocolOpcodes.s2c_syncmoney when raw.Length >= 9 && (extraTrackedOpcodes is null || extraTrackedOpcodes.Contains(opcode)):
                {
                    int money1 = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(1, 4));
                    int money2 = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(5, 4));
                    events.Add(new WireCaptureExpectedS2cEvent(
                        Opcode: opcode,
                        Name: "s2c_syncmoney",
                        ExactByteLength: raw.Length,
                        ExpectedTickOffsetFrames: relTick,
                        TickToleranceFrames: Math.Max(defaultTickToleranceFrames, 5),
                        FieldAssertions:
                        [
                            new WireCaptureFieldAssertion(1, 4, true, WireCaptureFieldMatcherKind.ExactInt64, ExpectedValue: money1, FieldName: "Money1"),
                            new WireCaptureFieldAssertion(5, 4, true, WireCaptureFieldMatcherKind.ExactInt64, ExpectedValue: money2, FieldName: "Money2")
                        ],
                        ObserverActorId: observerActorId));
                    break;
                }

                default:
                {
                    if (extraTrackedOpcodes is not null && extraTrackedOpcodes.Contains(opcode))
                    {
                        events.Add(new WireCaptureExpectedS2cEvent(
                            Opcode: opcode,
                            Name: $"s2c_0x{opcode:X2}",
                            ExactByteLength: null,
                            ExpectedTickOffsetFrames: relTick,
                            TickToleranceFrames: Math.Max(defaultTickToleranceFrames, 6),
                            FieldAssertions: [],
                            ObserverActorId: observerActorId));
                    }

                    break;
                }
            }
        }

        return events;
    }

    private static string? TryMatchSymbolByRecordedDwId(
        IReadOnlyDictionary<string, WireCaptureSymbolDefinition> symbols,
        WireCaptureSymbolKind kind,
        uint recordedDwId)
    {
        foreach ((string key, WireCaptureSymbolDefinition def) in symbols)
        {
            if (def.Kind == kind && def.RecordedDwId == recordedDwId)
            {
                return key;
            }
        }

        return null;
    }
}
