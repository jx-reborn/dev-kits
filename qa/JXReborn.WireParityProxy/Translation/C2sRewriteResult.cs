namespace JXReborn.WireParityProxy.Translation;

/// <summary>
/// Result of translating a decrypted C2S payload from Primary ID space into Shadow ID space.
/// </summary>
public sealed record C2sRewriteResult(
    byte[] ShadowPayload,
    bool WasModified,
    string? TranslationMissReason);
