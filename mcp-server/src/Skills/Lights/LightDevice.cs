using System.ComponentModel;

namespace Brain.Skills;

/// <summary>The lights the robot can switch. Descriptions are passed to the model so it can pick one.</summary>
public enum LightDevice
{
    [Description("the desk lamp in the study")]
    [DeviceID("study_study_main_light")]
    Study
}
