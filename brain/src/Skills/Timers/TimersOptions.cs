namespace Brain.Skills;

/// <summary>Options bound from the "Skills:timers" configuration section.</summary>
public class TimersOptions
{
    public const string SectionName = "Skills:timers";

    /// <summary>Path of the timers file. Relative paths resolve against the working directory.</summary>
    public string FilePath { get; set; } = "timers.md";
}
