using System.Text;
using System.Text.Json;
using ModelContextProtocol.Client;

namespace Brain.Skills.Mcp;

/// <summary>Adapts a single tool exposed by an MCP server to <see cref="ISkill"/>.</summary>
public sealed class McpSkill : ISkill
{
    private readonly McpClientTool _tool;

    public McpSkill(string name, McpClientTool tool)
    {
        Name = name;
        _tool = tool;
        Description = tool.Description;
        ParameterSchema = BinaryData.FromString(tool.JsonSchema.GetRawText());
    }

    public string Name { get; }
    public string Description { get; }
    public BinaryData ParameterSchema { get; }

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var args = new Dictionary<string, object?>();
        if (arguments.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in arguments.EnumerateObject())
            {
                args[property.Name] = property.Value.Clone();
            }
        }

        var result = await _tool.CallAsync(args, cancellationToken: cancellationToken);

        var text = new StringBuilder();
        foreach (var block in result.Content)
        {
            if (block is ModelContextProtocol.Protocol.TextContentBlock textBlock)
            {
                text.AppendLine(textBlock.Text);
            }
        }

        var output = text.Length > 0 ? text.ToString().TrimEnd() : result.StructuredContent?.ToString() ?? "(no output)";
        return result.IsError == true ? $"Failed: {output}" : output;
    }
}
