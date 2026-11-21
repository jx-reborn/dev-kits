using System.Text.Json.Nodes;

namespace JXReborn.WireParityProxy.Mcp;

/// <summary>
/// Line-delimited JSON-RPC 2.0 stdio transport host for <see cref="WireParityMcpToolDispatcher"/>.
/// </summary>
public sealed class WireParityMcpStdioHost
{
    private readonly WireParityMcpToolDispatcher _dispatcher;

    public WireParityMcpStdioHost(WireParityMcpToolDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
    }

    public async Task RunAsync(TextReader input, TextWriter output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            JsonObject? request;
            try
            {
                request = JsonNode.Parse(line) as JsonObject;
            }
            catch
            {
                continue;
            }

            if (request is null)
            {
                continue;
            }

            JsonObject? response = await _dispatcher.HandleRequestAsync(request, cancellationToken).ConfigureAwait(false);
            if (response is not null)
            {
                await output.WriteLineAsync(response.ToJsonString()).ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
