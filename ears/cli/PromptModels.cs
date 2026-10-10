using System.Text.Json.Serialization;

namespace Cli;

public record PromptRequest(string Prompt);

public record PromptResponse(string Response);

