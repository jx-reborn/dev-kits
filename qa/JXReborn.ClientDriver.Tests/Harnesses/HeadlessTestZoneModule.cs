using JXReborn.ZoneEngine.WorldZones;

namespace JXReborn.ClientDriver.Tests;

/// <summary>
/// Registers worker-thread-safe test harness commands for headless E2E journey verification.
/// </summary>
internal sealed class HeadlessTestZoneModule :
    IZoneModule,
    IZoneCommandHandler<WorkerSafeTeleportZoneCommand>
{
    public void Configure(IZoneExtensionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddCommandHandler<WorkerSafeTeleportZoneCommand>(this);
    }

    public async ValueTask HandleAsync(
        WorkerSafeTeleportZoneCommand command,
        ZoneExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            bool transferred = await context.TeleportPlayerAsync(
                command.Player,
                command.TargetMapId,
                command.TargetMpsX,
                command.TargetMpsY,
                command.TargetFightMode,
                cancellationToken);
            command.Completion.TrySetResult(transferred);
        }
        catch (Exception ex)
        {
            command.Completion.TrySetException(ex);
        }
    }
}
