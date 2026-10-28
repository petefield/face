namespace Brain;

/// <summary>Options bound from the "OpenAI" configuration section.</summary>
public class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>API key for the endpoint. Ollama ignores it, but the client requires a non-empty value.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Base URL of an OpenAI-compatible endpoint, e.g. "http://192.168.1.143:11434/v1". Empty uses OpenAI.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Model to use for completions, e.g. "gpt-4o".</summary>
    public string Model { get; set; } = "gpt-4o";

    /// <summary>Number of most recent conversation turns (a user prompt plus everything it triggered) to keep as context.</summary>
    public int MaxHistoryTurns { get; set; } = 8;

    /// <summary>Path to a text file containing the system prompt. Relative paths are resolved against the content root.</summary>
    public string SystemPromptFile { get; set; } = "system-prompt.txt";

    /// <summary>
    /// Reasoning effort sent with each completion ("none", "minimal", "low", "medium", "high"), or empty to
    /// leave it unset. Reasoning models reject function tools unless this is "none" on /v1/chat/completions.
    /// </summary>
    public string ReasoningEffort { get; set; } = "none";
}
