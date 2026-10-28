using ModelContextProtocol;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Blinky.McpServer.Music;
using Brain.Skills;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Blinky.McpServer;

/// <summary>Bridges the shared <see cref="ISkill"/> implementations to MCP tools.</summary>
public static class SkillTools
{
    public static IMcpServerBuilder WithSkillTools(this IMcpServerBuilder builder)
    {
        builder.Services.AddSingleton<IConfigureOptions<McpServerOptions>, SkillToolsConfigurator>();
        return builder;
    }

    private sealed class SkillToolsConfigurator(IServiceScopeFactory scopes) : IConfigureOptions<McpServerOptions>
    {
        public void Configure(McpServerOptions options)
        {
            options.Handlers.ListToolsHandler = (request, ct) =>
            {
                using var scope = scopes.CreateScope();
                var tools = scope.ServiceProvider.GetServices<ISkill>().Select(skill => new Tool
                {
                    Name = skill.Name,
                    Description = skill.Description,
                    InputSchema = JsonDocument.Parse(skill.ParameterSchema).RootElement.Clone(),
                }).ToList();
                return ValueTask.FromResult(new ListToolsResult { Tools = tools });
            };

            options.Handlers.CallToolHandler = async (request, ct) =>
            {
                using var scope = scopes.CreateScope();
                var skill = scope.ServiceProvider.GetServices<ISkill>()
                    .FirstOrDefault(s => s.Name == request.Params?.Name);
                if (skill is null)
                {
                    throw new McpException($"Unknown tool '{request.Params?.Name}'.");
                }

                var arguments = JsonSerializer.SerializeToElement(request.Params!.Arguments ?? new Dictionary<string, JsonElement>());
                var text = await skill.ExecuteAsync(arguments, ct);
                return new CallToolResult { Content = [new TextContentBlock { Text = text }] };
            };
        }
    }
}
