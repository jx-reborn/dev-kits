using System.Buffers.Binary;
using JXReborn.Adapter.Legacy.Packets;
using JXReborn.Primitives.Characters;
using JXReborn.Primitives.WorldZones;
using JXReborn.WireParityProxy.ShadowStates;
using JXReborn.WireParityProxy.Translation;
using Xunit;

namespace JXReborn.WireParityProxy.Tests.Translation;

public sealed class EntityIdTranslatorAndRewriterTests
{
    [Fact]
    public void RewriteDialogNpc_ShouldTranslateNpcIdAndRestampValidAntiCheatCheckValue()
    {
        var primary = new ProxyShadowClientState("legacy-linux");
        var shadow = new ProxyShadowClientState("reborn-server");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        primary.ApplyS2c(BuildSyncNpcPayload(npcId: 1042, templateId: 413, mpsX: 51200, mpsY: 102400, name: "CocGo"), now);
        shadow.ApplyS2c(BuildSyncNpcPayload(npcId: 2088, templateId: 413, mpsX: 51210, mpsY: 102405, name: "CocGo"), now);

        var translator = new EntityIdTranslator(primary, shadow);
        var rewriter = new C2sPacketRewriter(translator);

        const uint token1 = 0x13572468u;
        const uint token2 = 0x24681357u;
        byte[] dialogC2s = new byte[9];
        dialogC2s[0] = ProtocolOpcodes.c2s_dialognpc;
        BinaryPrimitives.WriteUInt32LittleEndian(dialogC2s.AsSpan(1, 4), 1042u);
        BinaryPrimitives.WriteUInt32LittleEndian(dialogC2s.AsSpan(5, 4), 0xDEADBEEFu);

        C2sRewriteResult result = rewriter.RewriteForShadow(dialogC2s, token1, token2);

        Assert.True(result.WasModified);
        Assert.Null(result.TranslationMissReason);
        Assert.Equal(2088u, BinaryPrimitives.ReadUInt32LittleEndian(result.ShadowPayload.AsSpan(1, 4)));

        uint stampedCheck = BinaryPrimitives.ReadUInt32LittleEndian(result.ShadowPayload.AsSpan(5, 4));
        Assert.True(MovementAntiCheatConsts.IsValidCheckValue(
            stampedCheck,
            token1,
            token2,
            MovementAntiCheatConsts.DialogNpcCheckKey));
    }

    [Fact]
    public void RewriteDialogNpc_WhenShadowMissingNpc_ShouldReportTranslationMiss()
    {
        var primary = new ProxyShadowClientState("legacy-linux");
        var shadow = new ProxyShadowClientState("reborn-server");
        primary.ApplyS2c(BuildSyncNpcPayload(npcId: 1042, templateId: 413, mpsX: 51200, mpsY: 102400, name: "CocGo"), DateTimeOffset.UtcNow);

        var rewriter = new C2sPacketRewriter(new EntityIdTranslator(primary, shadow));
        byte[] dialogC2s = new byte[9];
        dialogC2s[0] = ProtocolOpcodes.c2s_dialognpc;
        BinaryPrimitives.WriteUInt32LittleEndian(dialogC2s.AsSpan(1, 4), 1042u);

        C2sRewriteResult result = rewriter.RewriteForShadow(dialogC2s, 1u, 2u);

        Assert.False(result.WasModified);
        Assert.NotNull(result.TranslationMissReason);
        Assert.Contains("1042", result.TranslationMissReason, StringComparison.Ordinal);
    }

    private static byte[] BuildSyncNpcPayload(uint npcId, ushort templateId, int mpsX, int mpsY, string name)
    {
        var pkt = new SyncNpcFullS2CPacket(
            Camp: 0,
            Series: ElementSeries.Metal,
            LifePercent: 128,
            MapMpsY: mpsY,
            DoingState: 1,
            StateInfo: 0,
            MapMpsX: mpsX,
            MenuState: 0,
            GoldType: 0,
            Kind: EntityKind.DialogNpc,
            NpcId: npcId,
            MasterId: -1,
            TemplateId: templateId,
            Level: 10,
            RankFfId: 0,
            NameUnicode: name);
        return pkt.ToPayloadBytes();
    }
}
