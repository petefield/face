namespace Brain.Skills.Mcp;

public class McpOptions
{
    public const string SectionName = "Mcp";

    /// <summary>MCP servers to connect to, keyed by a short name used to prefix their tool names.</summary>
    public Dictionary<string, McpServerOptions> Servers { get; set; } = new();
}

public class McpServerOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>URL of a streamable HTTP / SSE server. When set, takes precedence over <see cref="Command"/>.</summary>
    public string? Url { get; set; }

    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>Executable for a stdio server.</summary>
    public string? Command { get; set; }

    public string[]? Arguments { get; set; }

    public Dictionary<string, string?>? Environment { get; set; }

    /// <summary>
    /// When non-empty, only tools whose name equals or ends with "__" plus one of these entries are exposed.
    /// </summary>
    public string[]? AllowedTools { get; set; }
}
