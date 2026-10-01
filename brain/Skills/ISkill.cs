using System.Text.Json;

namespace Brain.Skills;

/// <summary>
/// A capability the model can invoke during a conversation. Implementations are registered in DI and
/// surfaced to OpenAI as function tools.
/// </summary>
public interface ISkill
{
    /// <summary>Function name exposed to the model. Must be unique and match ^[a-zA-Z0-9_-]+$.</summary>
    string Name { get; }

    /// <summary>Describes to the model what the skill does and when to use it.</summary>
    string Description { get; }

    /// <summary>JSON Schema describing the skill's arguments object.</summary>
    BinaryData ParameterSchema { get; }

    /// <summary>Runs the skill and returns the result text handed back to the model.</summary>
    Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken);
}
