using Xunit;

namespace JXReborn.Integration.Tests.WireCaptures;

/// <summary>
/// Offline <c>game.exe</c> experience parity tests replaying real <c>legacy server</c> + <c>game.exe</c> wire captures
/// (<c>*.capture.json</c>) over encrypted TCP + 18-Hz Steppable Clock on GitHub Actions (<c>ubuntu-latest</c>)
/// without requiring <c>game.exe</c> or Docker.
/// </summary>
[Trait("Category", "LegacyData")]
public sealed class LegacyGameExeExperienceReplayIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public LegacyGameExeExperienceReplayIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Replay_RightClickEquipAndUnequipWeapon_ShouldMatchLegacyWireAndStateContract()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W01-RightClickEquippedWeapon", _output);
    }

    [Fact]
    public async Task Replay_EatManaPotion_ShouldRemoveItemAndRegenerateManaAcrossTicksLikeLegacyServer()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W02-EatManaPotionRegenTicks", _output);
    }

    [Fact]
    public async Task Replay_CastHangLongBatVuOnTrainingDummy_ShouldDeductManaBroadcastSkillAndDamageDummy()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W03-CastHangLongBatVuOnDummy", _output);
    }

    [Fact]
    public async Task Replay_EatSmallManaPotion_ShouldRemoveItemAndRegenerateFullManaDurationLikeLegacyServer()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W04-EatSmallManaPotionFullDuration", _output);
    }

    [Fact]
    public async Task Replay_NpcShopBuyMedicine_ShouldDeductSilverAndSyncItemLikeLegacyServer()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W05-NpcShopBuyMedicine", _output);
    }

    [Fact]
    public async Task Replay_RepositoryDepositAndWithdraw_ShouldSyncSilverBalancesLikeLegacyServer()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W06-RepositoryDepositAndWithdraw", _output);
    }

    [Fact]
    public async Task Replay_NovicePkModeSwitch_ShouldBroadcastAllNormalPkFlagsLikeLegacyServer()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W07-NovicePkModeSwitch", _output);
    }

    [Fact]
    public async Task Replay_MultiActorNearbyChat_ShouldDeliverNearbyMessageToPeerLikeLegacyServer()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W08-MultiActorNearbyChat", _output);
    }

    [Fact]
    public async Task Replay_MultiActorPartyInvite_ShouldSyncTeamProtocolToBothPeersLikeLegacyServer()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W09-MultiActorPartyInvite", _output);
    }

    [Fact]
    public async Task Replay_MultiActorPlayerTradeInvite_ShouldDeliverTradeApplyStartToPeerLikeLegacyServer()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W10-MultiActorPlayerTradeInvite", _output);
    }

    [Fact]
    public async Task Replay_MultiActorFriendInvite_ShouldDeliverFriendApplyToPeerLikeLegacyServer()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W11-MultiActorFriendInvite", _output);
    }

    [Fact]
    public async Task Replay_PlayerAttributeAllocation_ShouldEmitAttributeSyncPacketsLikeLegacyServer()
    {
        await OfflineWireCaptureReplayer.ReplayAndAssertAsync("W12-PlayerAttributeAllocation", _output);
    }
}
