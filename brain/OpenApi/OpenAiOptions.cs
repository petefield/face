namespace Brain;

/// <summary>Options bound from the "OpenAI" configuration section.</summary>
public class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>API key for the OpenAI endpoint.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Model to use for completions, e.g. "gpt-4o".</summary>
    public string Model { get; set; } = "gpt-4o";

    /// <summary>Path to a text file containing the system prompt. Relative paths are resolved against the content root.</summary>
    public string SystemPromptFile { get; set; } = "system-prompt.txt";

    /// <summary>
    /// Reasoning effort sent with each completion ("none", "minimal", "low", "medium", "high"), or empty to
    /// leave it unset. Reasoning models reject function tools unless this is "none" on /v1/chat/completions.
    /// </summary>
    public string ReasoningEffort { get; set; } = "none";
}
