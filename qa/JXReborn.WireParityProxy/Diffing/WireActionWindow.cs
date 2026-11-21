using JXReborn.WireParityProxy.Wire;

namespace JXReborn.WireParityProxy.Diffing;

/// <summary>
/// Captures a single C2S action and all Primary vs Shadow S2C responses observed within its time window.
/// </summary>
public sealed class WireActionWindow
{
    private readonly List<JxWireFrame> _primaryS2c = [];
    private readonly List<JxWireFrame> _shadowS2c = [];
    private readonly Lock _sync = new();

    public WireActionWindow(
        int actionSequence,
        string actionLabel,
        JxWireFrame primaryC2s,
        byte[] shadowC2sPayload,
        bool c2sWasRewritten,
        string? translationMissReason)
    {
        ActionSequence = actionSequence;
        ActionLabel = actionLabel;
        PrimaryC2s = primaryC2s;
        ShadowC2sPayload = shadowC2sPayload;
        C2sWasRewritten = c2sWasRewritten;
        TranslationMissReason = translationMissReason;
        OpenedAtUtc = primaryC2s.TimestampUtc;
    }

    public int ActionSequence { get; }

    public string ActionLabel { get; }

    public JxWireFrame PrimaryC2s { get; }

    public byte[] ShadowC2sPayload { get; }

    public bool C2sWasRewritten { get; }

    public string? TranslationMissReason { get; }

    public DateTimeOffset OpenedAtUtc { get; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public void AddPrimaryS2c(JxWireFrame frame)
    {
        lock (_sync)
        {
            _primaryS2c.Add(frame);
        }
    }

    public void AddShadowS2c(JxWireFrame frame)
    {
        lock (_sync)
        {
            _shadowS2c.Add(frame);
        }
    }

    public (IReadOnlyList<JxWireFrame> Primary, IReadOnlyList<JxWireFrame> Shadow) CloseAndSnapshot(DateTimeOffset closedAtUtc)
    {
        lock (_sync)
        {
            ClosedAtUtc = closedAtUtc;
            return (_primaryS2c.ToArray(), _shadowS2c.ToArray());
        }
    }
}
