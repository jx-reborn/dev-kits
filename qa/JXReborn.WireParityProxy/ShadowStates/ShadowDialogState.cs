namespace JXReborn.WireParityProxy.ShadowStates;

/// <summary>
/// Tracks open NPC/script dialog state reconstructed from S2C <c>s2c_scriptaction</c> (<c>0x63</c>)
/// and C2S dialog selection packets.
/// </summary>
public sealed record ShadowDialogState(
    bool IsOpen,
    uint Sequence,
    string PromptTcvn3Hex,
    IReadOnlyList<string> OptionsTcvn3Hex,
    bool IsTalk)
{
    public static ShadowDialogState Closed { get; } = new(false, 0, string.Empty, [], false);
}
