using System.ComponentModel;

namespace Brain.Skills;

/// <summary>Arguments for controlling the face expression.</summary>
public record FaceArguments(
    [property: Description("The desired face expression state.")] FaceState State);

