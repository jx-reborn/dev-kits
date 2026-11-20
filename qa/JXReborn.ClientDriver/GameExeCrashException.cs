namespace JXReborn.ClientDriver;

/// <summary>
/// Exception raised when a headless <c>game.exe</c> process exits unexpectedly or records a native VEH crash dump.
/// </summary>
public sealed class GameExeCrashException : Exception
{
    public GameExeCrashException(
        string message,
        int? exitCode = null,
        string? crashDumpPath = null,
        string? crashDumpJson = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ExitCode = exitCode;
        CrashDumpPath = crashDumpPath;
        CrashDumpJson = crashDumpJson;
    }

    public int? ExitCode { get; }

    public string? CrashDumpPath { get; }

    public string? CrashDumpJson { get; }
}
