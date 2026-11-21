namespace JXReborn.DualParityServer.Standalone;

/// <summary>
/// Thread-safe bounded ring buffer capturing live stdout/stderr log lines for a managed server process.
/// </summary>
public sealed class RingLogBuffer
{
    private readonly Lock _lock = new();
    private readonly Queue<string> _lines;
    private readonly int _capacity;

    public RingLogBuffer(int capacity = 250)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        }

        _capacity = capacity;
        _lines = new Queue<string>(capacity);
    }

    public void Append(string? line)
    {
        if (line is null)
        {
            return;
        }

        string stamped = $"[{DateTimeOffset.Now:HH:mm:ss}] {line}";
        lock (_lock)
        {
            if (_lines.Count >= _capacity)
            {
                _lines.Dequeue();
            }

            _lines.Enqueue(stamped);
        }
    }

    public IReadOnlyList<string> GetLines(int maxLines = 100)
    {
        lock (_lock)
        {
            if (_lines.Count <= maxLines)
            {
                return [.. _lines];
            }

            return _lines.Skip(_lines.Count - maxLines).ToArray();
        }
    }

    public string GetAllText()
    {
        lock (_lock)
        {
            return string.Join(Environment.NewLine, _lines);
        }
    }
}
