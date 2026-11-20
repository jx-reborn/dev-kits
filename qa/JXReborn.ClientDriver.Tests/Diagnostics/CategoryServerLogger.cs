using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace JXReborn.ClientDriver.Tests.Diagnostics;

/// <summary>
/// Per-category <see cref="ILogger"/> forwarding structured entries into a shared queue.
/// </summary>
public sealed class CategoryServerLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ConcurrentQueue<ServerLogEntry> _entries;

    public CategoryServerLogger(string categoryName, ConcurrentQueue<ServerLogEntry> entries)
    {
        _categoryName = categoryName;
        _entries = entries;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string message = formatter(state, exception);
        _entries.Enqueue(new ServerLogEntry(
            DateTimeOffset.UtcNow,
            logLevel,
            _categoryName,
            eventId.Id,
            message,
            exception?.ToString()));
    }
}
