using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace JXReborn.ClientDriver.Tests.Diagnostics;

/// <summary>
/// Thread-safe in-memory <see cref="ILoggerProvider"/> capturing server logs during headless client journeys.
/// </summary>
public sealed class ServerLogCollector : ILoggerProvider
{
    private readonly ConcurrentQueue<ServerLogEntry> _entries = new();

    public IReadOnlyList<ServerLogEntry> SnapshotEntries() => [.. _entries];

    public ILogger CreateLogger(string categoryName) => new CategoryServerLogger(categoryName, _entries);

    public void Dispose()
    {
    }
}
