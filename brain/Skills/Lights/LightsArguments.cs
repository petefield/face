using System.ComponentModel;

namespace Brain.Skills;

/// <summary>Arguments for controlling the lights.</summary>
public record LightsArguments(
    [property: Description("Which devices to control.")] LightDevice[] Devices,
    [property: Description("Whether the lights should be on or off.")] LightState State);

