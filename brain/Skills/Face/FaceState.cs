using System.ComponentModel;

namespace Brain.Skills;


public enum FaceState
{
    [Description("the face when neutral")]
    Neutral,
    [Description("the face when happy")]
    Happy,
    [Description("the face when angry")]
    Angry,
    [Description("the face when sleepy")]
    Sleep
}
