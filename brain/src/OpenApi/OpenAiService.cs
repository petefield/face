using System.ClientModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Brain.Skills;
using Brain.Skills.Mcp;
using Microsoft.Extensions.Options;
using OpenAI;
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
    private readonly string _endpointDisplay;
    private readonly OpenAiOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly McpSkillProvider _mcpSkills;
    private readonly string _systemPromptPath;
    private readonly ILogger<OpenAiService> _logger;
    private readonly List<ChatMessage> _history = new();
    private readonly SemaphoreSlim _historyLock = new(1, 1);

    public OpenAiService(
        IOptions<OpenAiOptions> options,
        IWebHostEnvironment environment,
        IServiceScopeFactory scopeFactory,
        McpSkillProvider mcpSkills,
        ILogger<OpenAiService> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _mcpSkills = mcpSkills;
        _logger = logger;

        var hasEndpoint = !string.IsNullOrWhiteSpace(_options.Endpoint);
        if (string.IsNullOrWhiteSpace(_options.ApiKey) && !hasEndpoint)
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Set OpenAI:ApiKey in appsettings.json, user-secrets, or the OPENAI__APIKEY environment variable.");
        }

        _systemPromptPath = Path.IsPathRooted(_options.SystemPromptFile)
            ? _options.SystemPromptFile
            : Path.Combine(environment.ContentRootPath, _options.SystemPromptFile);

        _endpointDisplay = hasEndpoint ? _options.Endpoint : "https://api.openai.com/v1";
        var apiKey = string.IsNullOrWhiteSpace(_options.ApiKey) ? "none" : _options.ApiKey;
        _chatClient = hasEndpoint
            ? new ChatClient(_options.Model, new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(_options.Endpoint) })
            : new ChatClient(_options.Model, apiKey);
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
            var skills = scope.ServiceProvider.GetServices<ISkill>()
                .Concat(await _mcpSkills.GetSkillsAsync(cancellationToken))
                .ToDictionary(skill => skill.Name);

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

            var emptyRetries = 0;
            for (var round = 0; round <= MaxToolRounds; round++)
            {
                _logger.LogInformation(
                    "LLM request (round {Round}) to {Endpoint} model {Model}: {MessageCount} messages, {ToolCount} tools. Last message: {Last}",
                    round, _endpointDisplay, _options.Model, messages.Count, completionOptions.Tools.Count, Describe(messages[^1]));

                var stopwatch = Stopwatch.StartNew();
                var completion = await _chatClient.CompleteChatAsync(messages, completionOptions, cancellationToken);
                stopwatch.Stop();

                var usage = completion.Value.Usage;
                _logger.LogInformation(
                    "LLM response (round {Round}) from {Endpoint} in {ElapsedMs} ms: finish={FinishReason}, tokens in/out={InputTokens}/{OutputTokens}, text: {Text}",
                    round,
                    _endpointDisplay,
                    stopwatch.ElapsedMilliseconds,
                    completion.Value.FinishReason,
                    usage?.InputTokenCount,
                    usage?.OutputTokenCount,
                    completion.Value.Content.Count > 0 ? completion.Value.Content[0].Text : string.Empty);

                foreach (var call in completion.Value.ToolCalls)
                {
                    _logger.LogInformation("LLM tool call {ToolCallId}: {Skill}({Arguments})", call.Id, call.FunctionName, call.FunctionArguments);
                }

                var isEmpty = completion.Value.ToolCalls.Count == 0
                    && (completion.Value.Content.Count == 0 || string.IsNullOrWhiteSpace(completion.Value.Content[0].Text));
                if (isEmpty && emptyRetries < 2)
                {
                    // Small local models sometimes return nothing at all; asking again usually works.
                    emptyRetries++;
                    _logger.LogWarning("LLM returned an empty response; retrying ({Attempt}/2).", emptyRetries);
                    round--;
                    continue;
                }

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
                    _logger.LogInformation("Tool result {ToolCallId} from {Skill}: {Result}", toolCall.Id, toolCall.FunctionName, result);
                    var toolMessage = new ToolChatMessage(toolCall.Id, result);
                    messages.Add(toolMessage);
                    newMessages.Add(toolMessage);
                }
            }

            _history.AddRange(newMessages);
            TrimHistory();

            // The speech engine rejects emoji, which small models add despite the prompt.
            var text = System.Text.RegularExpressions.Regex.Replace(reply, @"[\p{So}\p{Cs}\uFE0F\u200D]", string.Empty).Trim();

            return new PromptResponse(text);
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

    private static string Describe(ChatMessage message) => message switch
    {
        UserChatMessage => $"user: {message.Content.FirstOrDefault()?.Text}",
        ToolChatMessage => $"tool: {message.Content.FirstOrDefault()?.Text}",
        _ => message.GetType().Name,
    };

    /// <summary>
    /// Drops the oldest turns beyond <see cref="OpenAiOptions.MaxHistoryTurns"/>. Trimming always starts at a
    /// user message so tool calls are never separated from their results.
    /// </summary>
    private void TrimHistory()
    {
        var max = Math.Max(1, _options.MaxHistoryTurns);
        var turnStarts = new List<int>();
        for (var i = 0; i < _history.Count; i++)
        {
            if (_history[i] is UserChatMessage)
            {
                turnStarts.Add(i);
            }
        }

        if (turnStarts.Count > max)
        {
            _history.RemoveRange(0, turnStarts[turnStarts.Count - max]);
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
}
