using System.Text.Json;
using System.Text.RegularExpressions;
using Brain.Skills;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Brain;

/// <summary>
/// Sends prompts to the OpenAI chat completions endpoint, prefixed with a system prompt loaded from disk.
/// Retains the full conversation history (shared across all callers) so each new prompt is answered with
/// the context of everything said before it. Registered <see cref="ISkill"/> implementations are offered
/// to the model as callable tools.
/// </summary>
public class OpenAiService
{
    /// <summary>Guards against a model that keeps requesting tools without ever producing an answer.</summary>
    private const int MaxToolRounds = 5;
    private readonly ChatClient _chatClient;
    private readonly OpenAiOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _systemPromptPath;
    private readonly ILogger<OpenAiService> _logger;
    private readonly List<ChatMessage> _history = new();
    private readonly SemaphoreSlim _historyLock = new(1, 1);

    // Matches a trailing ```json { ... } ``` fenced code block, e.g. {"emotion": "happy"}.
    private static readonly Regex EmotionBlockRegex = new(
        @"```json\s*(?<json>\{.*?\})\s*```\s*$",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public OpenAiService(
        IOptions<OpenAiOptions> options,
        IWebHostEnvironment environment,
        IServiceScopeFactory scopeFactory,
        ILogger<OpenAiService> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Set OpenAI:ApiKey in appsettings.json, user-secrets, or the OPENAI__APIKEY environment variable.");
        }

        _systemPromptPath = Path.IsPathRooted(_options.SystemPromptFile)
            ? _options.SystemPromptFile
            : Path.Combine(environment.ContentRootPath, _options.SystemPromptFile);

        _chatClient = new ChatClient(_options.Model, _options.ApiKey);
    }

    /// <summary>
    /// Sends the given prompt to OpenAI, preceded by the configured system prompt and the accumulated
    /// conversation history, then records both the prompt and the reply in that history for future calls.
    /// </summary>
    public async Task<PromptResponse> GetResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var systemPrompt = await ReadSystemPromptAsync(cancellationToken);

        await _historyLock.WaitAsync(cancellationToken);
        try
        {
            // Skills are resolved per call so scoped dependencies (such as FaceClient) are not captured
            // by this singleton.
            using var scope = _scopeFactory.CreateScope();
            var skills = scope.ServiceProvider.GetServices<ISkill>().ToDictionary(skill => skill.Name);

            var messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };
            messages.AddRange(_history);
            messages.Add(new UserChatMessage(prompt));

            var completionOptions = new ChatCompletionOptions();
            if (!string.IsNullOrWhiteSpace(_options.ReasoningEffort))
            {
#pragma warning disable OPENAI001 // ReasoningEffortLevel is still an evaluation API.
                completionOptions.ReasoningEffortLevel = new ChatReasoningEffortLevel(_options.ReasoningEffort.ToLowerInvariant());
#pragma warning restore OPENAI001
            }

            foreach (var skill in skills.Values)
            {
                completionOptions.Tools.Add(ChatTool.CreateFunctionTool(skill.Name, skill.Description, skill.ParameterSchema));
            }

            var reply = string.Empty;
            var newMessages = new List<ChatMessage> { new UserChatMessage(prompt) };

            for (var round = 0; round <= MaxToolRounds; round++)
            {
                var completion = await _chatClient.CompleteChatAsync(messages, completionOptions, cancellationToken);

                if (completion.Value.FinishReason != ChatFinishReason.ToolCalls)
                {
                    reply = completion.Value.Content.Count > 0 ? completion.Value.Content[0].Text : string.Empty;
                    newMessages.Add(new AssistantChatMessage(reply));
                    break;
                }

                var assistantMessage = new AssistantChatMessage(completion.Value);
                messages.Add(assistantMessage);
                newMessages.Add(assistantMessage);

                foreach (var toolCall in completion.Value.ToolCalls)
                {
                    var result = await InvokeSkillAsync(skills, toolCall, cancellationToken);
                    var toolMessage = new ToolChatMessage(toolCall.Id, result);
                    messages.Add(toolMessage);
                    newMessages.Add(toolMessage);
                }
            }

            _history.AddRange(newMessages);

            var (text, emotion) = ExtractEmotion(reply);
            return new PromptResponse(text, emotion);
        }
        finally
        {
            _historyLock.Release();
        }
    }

    private async Task<string> InvokeSkillAsync(
        IReadOnlyDictionary<string, ISkill> skills,
        ChatToolCall toolCall,
        CancellationToken cancellationToken)
    {
        if (!skills.TryGetValue(toolCall.FunctionName, out var skill))
        {
            _logger.LogWarning("Model requested unknown skill {Skill}.", toolCall.FunctionName);
            return $"Failed: no skill named '{toolCall.FunctionName}' is available.";
        }

        try
        {
            var json = toolCall.FunctionArguments.ToString();
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);

            _logger.LogInformation("Running skill {Skill} with arguments {Arguments}.", skill.Name, json);
            return await skill.ExecuteAsync(document.RootElement, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Model supplied malformed arguments for skill {Skill}.", skill.Name);
            return "Failed: the arguments were not valid JSON.";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Skill {Skill} threw an exception.", skill.Name);
            return $"Failed: {ex.Message}";
        }
    }

    /// <summary>Clears the accumulated conversation history, starting a fresh conversation.</summary>
    public async Task ResetHistoryAsync(CancellationToken cancellationToken = default)
    {
        await _historyLock.WaitAsync(cancellationToken);
        try
        {
            _history.Clear();
        }
        finally
        {
            _historyLock.Release();
        }
    }

    private async Task<string> ReadSystemPromptAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_systemPromptPath))
        {
            _logger.LogWarning("System prompt file not found at {Path}; proceeding with no system prompt.", _systemPromptPath);
            return string.Empty;
        }

        return await File.ReadAllTextAsync(_systemPromptPath, cancellationToken);
    }

    /// <summary>
    /// Splits the trailing ```json {"emotion": "..."} ``` block off the model's reply, returning the
    /// remaining text and the parsed emotion. Falls back to <see cref="EmotionState.Neutral"/> if the
    /// block is missing or malformed.
    /// </summary>
    private static (string Text, EmotionState Emotion) ExtractEmotion(string reply)
    {
        var trimmed = reply.TrimEnd();
        var match = EmotionBlockRegex.Match(trimmed);
        if (!match.Success)
        {
            return (reply, EmotionState.Neutral);
        }

        var emotion = EmotionState.Neutral;
        try
        {
            using var document = JsonDocument.Parse(match.Groups["json"].Value);
            if (document.RootElement.TryGetProperty("emotion", out var emotionProperty) &&
                Enum.TryParse(emotionProperty.GetString(), ignoreCase: true, out EmotionState parsed))
            {
                emotion = parsed;
            }
        }
        catch (JsonException)
        {
            // Malformed JSON block; fall back to neutral and leave the text untouched below.
        }

        var text = trimmed[..match.Index].TrimEnd();
        return (text, emotion);
    }
}
