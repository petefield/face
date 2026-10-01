using System.ComponentModel;

namespace Brain.Skills;

/// <summary>The lights the robot can switch. Descriptions are passed to the model so it can pick one.</summary>
/// <summary>The lights the robot can switch. Descriptions are passed to the model so it can pick one.</summary>
public enum LightDevice
{
    [Description("the main ceiling light in the living room")]
    LivingRoom,

    [Description("the desk lamp in the study")]
    Study
}
