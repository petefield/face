using System.ComponentModel;

namespace Brain.Skills;

/// <summary>Arguments for controlling the lights.</summary>
public record McpSearchArguements(
    [property: Description("a descripiton of the task to be performed")] string Query);

