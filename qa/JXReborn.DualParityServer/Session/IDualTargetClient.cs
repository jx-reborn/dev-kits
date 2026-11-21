using JXReborn.ClientDriver;

namespace JXReborn.DualParityServer.Session;

/// <summary>
/// Testable abstraction over <see cref="JxHeadlessClient"/> representing one connected <c>game.exe</c> target
/// (<c>legacy-linux</c> or <c>reborn-server</c>).
/// </summary>
public interface IDualTargetClient : IAsyncDisposable
{
    string TargetName { get; }

    string PipeName { get; }

    int? ProcessId { get; }

    Task<WorldDomSnapshot> GetStateAsync(CancellationToken cancellationToken = default);

    Task MoveToAsync(int mpsX, int mpsY, bool run = true, CancellationToken cancellationToken = default);

    Task InteractNpcByDwIdAsync(uint npcDwId, CancellationToken cancellationToken = default);

    Task SelectDialogAsync(int optionIndex, CancellationToken cancellationToken = default);

    Task ConfirmTalkAsync(CancellationToken cancellationToken = default);

    Task UseItemAsync(int arrayIndex, int place, int gridX, int gridY, CancellationToken cancellationToken = default);

    Task SwitchPkAsync(int pkMode, CancellationToken cancellationToken = default);

    Task SendChatAsync(int channel, string messageUnicode, CancellationToken cancellationToken = default);

    Task<string?> TryCaptureScreenshotAsync(string outputPath, CancellationToken cancellationToken = default);
}
