using System.ComponentModel;

namespace Brain.Skills;

/// <summary>Arguments for controlling the face expression.</summary>
public record SetTimerArgs(
    [property: Description("How long (in minutes) before the timer should fire.")] int durationMins);

