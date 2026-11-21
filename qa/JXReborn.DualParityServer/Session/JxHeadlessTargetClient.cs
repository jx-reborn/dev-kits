using JXReborn.ClientDriver;

namespace JXReborn.DualParityServer.Session;

/// <summary>
/// Concrete <see cref="IDualTargetClient"/> delegating to <see cref="JxHeadlessClient"/>.
/// </summary>
public sealed class JxHeadlessTargetClient : IDualTargetClient
{
    private readonly JxHeadlessClient _client;

    public JxHeadlessTargetClient(string targetName, JxHeadlessClient client)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        ArgumentNullException.ThrowIfNull(client);
        TargetName = targetName;
        _client = client;
    }

    public string TargetName { get; }

    public string PipeName => _client.PipeName;

    public int? ProcessId => _client.ProcessId;

    public Task<WorldDomSnapshot> GetStateAsync(CancellationToken cancellationToken = default) =>
        _client.GetStateAsync(cancellationToken);

    public async Task MoveToAsync(int mpsX, int mpsY, bool run = true, CancellationToken cancellationToken = default) =>
        await _client.MoveToAsync(mpsX, mpsY, mode: run ? 2 : 1, cancellationToken: cancellationToken);

    public async Task InteractNpcByDwIdAsync(uint npcDwId, CancellationToken cancellationToken = default) =>
        await _client.InteractNpcByDwIdAsync(npcDwId, cancellationToken: cancellationToken);

    public async Task SelectDialogAsync(int optionIndex, CancellationToken cancellationToken = default) =>
        await _client.SelectDialogAsync(optionIndex, cancellationToken: cancellationToken);

    public async Task ConfirmTalkAsync(CancellationToken cancellationToken = default) =>
        await _client.ConfirmTalkAsync(cancellationToken: cancellationToken);

    public async Task UseItemAsync(int arrayIndex, int place, int gridX, int gridY, CancellationToken cancellationToken = default) =>
        await _client.UseItemAsync(arrayIndex, place, gridX, gridY, cancellationToken: cancellationToken);

    public async Task SwitchPkAsync(int pkMode, CancellationToken cancellationToken = default) =>
        await _client.SwitchPkAsync(pkMode, cancellationToken: cancellationToken);

    public async Task SendChatAsync(int channel, string messageUnicode, CancellationToken cancellationToken = default) =>
        await _client.SendChatAsync(channel, messageUnicode, cancellationToken: cancellationToken);

    public async Task<string?> TryCaptureScreenshotAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        try
        {
            ScreenshotResult result = await _client.CaptureScreenshotAsync(outputPath, cancellationToken);
            return result.BytesWritten > 0 ? result.Path : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public ValueTask DisposeAsync() => _client.DisposeAsync();
}
