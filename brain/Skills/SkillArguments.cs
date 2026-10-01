using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Brain.Skills;

/// <summary>Helpers for turning the raw argument payload the model supplies into a typed object.</summary>
public static class SkillArguments
{
    /// <summary>Shared by parsing and schema generation so the two can never describe different shapes.</summary>
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        // Schema export requires a resolver to be set explicitly.
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    /// <summary>
    /// Deserializes the arguments into <typeparamref name="T"/>. Returns false, rather than throwing,
    /// when the model supplies something that does not fit the skill's schema.
    /// </summary>
    public static bool TryParse<T>(this JsonElement arguments, [NotNullWhen(true)] out T? value)
    {
        try
        {
            value = arguments.Deserialize<T>(SerializerOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }
}
