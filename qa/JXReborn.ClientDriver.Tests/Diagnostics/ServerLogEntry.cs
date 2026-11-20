using Microsoft.Extensions.Logging;

namespace JXReborn.ClientDriver.Tests.Diagnostics;

/// <summary>
/// Immutable structured log entry captured from the in-process JXReborn host during headless E2E runs.
/// </summary>
public sealed record ServerLogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Category,
    int EventId,
    string Message,
    string? ExceptionText);
