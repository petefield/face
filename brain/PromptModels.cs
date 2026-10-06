using System.Text.Json.Serialization;

namespace Brain;

/// <summary>Request body for the /prompt endpoint.</summary>
/// <param name="Prompt">The user prompt to send to OpenAI.</param>
public record PromptRequest(string Prompt);

/// <summary>Response body for the /prompt endpoint.</summary>
/// <param name="Response">The text returned by OpenAI, with the trailing emotion JSON block removed.</param>
/// <param name="Emotion">The emotional state the model reported for this reply.</param>
public record PromptResponse(string Response, EmotionState Emotion);

/// <summary>The emotional state reported by the model alongside its reply.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmotionState
{
    Neutral,
    Happy,
    Angry,
    Sleep
}
