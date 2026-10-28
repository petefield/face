using System.Globalization;
using System.Text.Json;

namespace Brain.Skills;

/// <summary>Reports the robot's local date and time.</summary>
public class SetTimerSkill : ISkill
{
    private readonly TimeProvider _timeProvider;
    private readonly TimersFile _timersFile;

    public SetTimerSkill(TimeProvider timeProvider, TimersFile timersFile)
    {
        _timeProvider = timeProvider;
        _timersFile = timersFile;
    }

    public string Name => "set_timer";

    public string Description => "Sets a timer";

    public BinaryData ParameterSchema => SkillSchema.Create<SetTimerArgs>();

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryParse<SetTimerArgs>(out var parsed))
        {
            return "Failed: 'state' is required.";
        }

        if (parsed.durationMins <= 0)
        {
            return "You can't schedule a timer for the past";
        }

        var fireAt = _timeProvider.GetUtcNow().AddMinutes(parsed.durationMins);

        await _timersFile.AppendLineAsync(fireAt.ToString("o", CultureInfo.InvariantCulture), cancellationToken);

        return $"New timer scheduled to fire at {fireAt}";
    }
}
