using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Brain.Skills.Mcp;

/// <summary>
/// Connects to the configured MCP servers and exposes their tools as skills. Connections are made lazily and
/// retried on later calls if a server was unreachable; the connected servers are kept for the app's lifetime.
/// </summary>
public sealed partial class McpSkillProvider : IAsyncDisposable
{
    private readonly McpOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpSkillProvider> _logger;
    private readonly Dictionary<string, List<McpSkill>> _servers = new();
    private readonly List<McpClient> _clients = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public McpSkillProvider(IOptions<McpOptions> options, ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<McpSkillProvider>();
    }

    public async Task<IReadOnlyList<ISkill>> GetSkillsAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            foreach (var (name, server) in _options.Servers)
            {
                if (!server.Enabled || _servers.ContainsKey(name))
                {
                    continue;
                }

                try
                {
                    _servers[name] = await ConnectAsync(name, server, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Could not connect to MCP server {Server}.", name);
                }
            }

            return _servers.Values.SelectMany(skills => skills).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<McpSkill>> ConnectAsync(string name, McpServerOptions server, CancellationToken cancellationToken)
    {
        IClientTransport transport;
        if (!string.IsNullOrWhiteSpace(server.Url))
        {
            transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = name,
                Endpoint = new Uri(server.Url),
                AdditionalHeaders = server.Headers,
            }, _loggerFactory);
        }
        else if (!string.IsNullOrWhiteSpace(server.Command))
        {
            transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = name,
                Command = server.Command,
                Arguments = server.Arguments,
                EnvironmentVariables = server.Environment,
            }, _loggerFactory);
        }
        else
        {
            throw new InvalidOperationException($"MCP server '{name}' needs either Url or Command.");
        }

        var client = await McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: cancellationToken);
        _clients.Add(client);

        var prefix = Sanitize(name);
        var skills = new List<McpSkill>();
        foreach (var tool in await client.ListToolsAsync(cancellationToken: cancellationToken))
        {
            if (!IsAllowed(server, tool.Name))
            {
                continue;
            }

            // OpenAI limits function names to 64 characters.
            var skillName = Sanitize($"{prefix}__{tool.Name}");
            skills.Add(new McpSkill(skillName.Length > 64 ? skillName[..64] : skillName, tool));
        }

        _logger.LogInformation("Connected to MCP server {Server} with {Count} tools.", name, skills.Count);
        return skills;
    }

    private static bool IsAllowed(McpServerOptions server, string toolName) =>
        server.AllowedTools is not { Length: > 0 } allowed
        || allowed.Any(a => toolName.Equals(a, StringComparison.OrdinalIgnoreCase)
            || toolName.EndsWith("__" + a, StringComparison.OrdinalIgnoreCase));

    private static string Sanitize(string value) => InvalidNameChars().Replace(value, "_");

    [GeneratedRegex("[^a-zA-Z0-9_-]")]
    private static partial Regex InvalidNameChars();

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            await client.DisposeAsync();
        }
    }
}
