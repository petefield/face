using System.Text.Json.Serialization;

namespace Brain;

/// <summary>Request body for the /prompt endpoint.</summary>
/// <param name="Prompt">The user prompt to send to OpenAI.</param>
public record PromptRequest(string Prompt);

/// <summary>Response body for the /prompt endpoint.</summary>
/// <param name="Response">The text returned by OpenAI, with the trailing emotion JSON block removed.</param>
public record PromptResponse(string Response);

