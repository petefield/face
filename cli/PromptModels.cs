using System.Text.Json.Serialization;

namespace Cli;

public record PromptRequest(string Prompt);

public record PromptResponse(string Response, EmotionState Emotion);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmotionState
{
    Neutral,
    Happy,
    Angry,
}
