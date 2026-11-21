namespace JXReborn.WireParityProxy.Wire;

/// <summary>
/// Immutable representation of a decrypted JX1 protocol frame observed on the wire.
/// </summary>
public sealed record JxWireFrame(
    byte Opcode,
    byte[] DecryptedPayload,
    DateTimeOffset TimestampUtc,
    long Sequence)
{
    public string ToHexString() => Convert.ToHexString(DecryptedPayload);
}
