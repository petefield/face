using System.Text.Json;

namespace Brain.Skills;

/// <summary>Turns the robot's lights on or off.</summary>
public sealed class LightsSkill : ISkill
{
    private readonly ILogger<LightsSkill> _logger;

    public LightsSkill(ILogger<LightsSkill> logger) => _logger = logger;

    public string Name => "control_lights";

    public string Description => "Turns the home lights on or off. Use this when the user asks for the lights to be switched.";

    public BinaryData ParameterSchema => SkillSchema.Create<LightsArguments>();

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryParse<LightsArguments>(out var parsed))
        {
            return Task.FromResult("Failed: 'device' and 'state' are required.");
        }

        foreach (var device in parsed.Devices)
        {
            _logger.LogInformation("Setting {Device} lights {State}.", device, parsed.State);
        }

        return Task.FromResult($"The {string.Join(", ", parsed.Devices) } lights are now {parsed.State}.".ToLowerInvariant());
    }
}
