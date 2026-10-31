using System.Text.Json;
using System.Text.Json.Nodes;

namespace Brain.Skills;

/// <summary>Finds tools that may be useful for the current task.</summary>
public sealed class McpSearchSkill(ISkillsRegister register, ILogger<McpSearchSkill> logger) : ISkill
{
    public const string SkillName = "search_tools";

    public string Name => SkillName;
    public string Description => "Searches all available tools";
    public BinaryData ParameterSchema => SkillSchema.Create<McpSearchArguements>();

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!arguments.TryParse<McpSearchArguements>(out var parsed))
        {
            logger.LogWarning("Model supplied invalid arguments for skill {Skill}.", Name);
            return Task.FromResult("Failed: 'query' is required.");
        }

        var skills = register.Search(parsed.Query);
        var matches = skills.Select(skill => new
        {
            name = skill.Name,
            description = skill.Description,
            parameters = JsonNode.Parse(skill.ParameterSchema.ToString()),
        });

        return Task.FromResult(JsonSerializer.Serialize(matches));
    }
}
