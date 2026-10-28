using System.Text.Json;

namespace Brain.Skills;

/// <summary>Reports the robot's local date and time.</summary>
public class CurrentTimeSkill : ISkill
{
    private readonly TimeProvider _timeProvider;

    public CurrentTimeSkill(TimeProvider timeProvider) => _timeProvider = timeProvider;

    public string Name => "get_current_time";

    public string Description => "Gets the robot's current local date and time. Use this whenever the user asks what time or day it is.";

    public BinaryData ParameterSchema => BinaryData.FromString("""
        {
          "type": "object",
          "properties": {},
          "required": [],
          "additionalProperties": false
        }
        """);

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetLocalNow();
        return Task.FromResult(now.ToString("dddd, d MMMM yyyy 'at' HH:mm:ss zzz"));
    }
}
