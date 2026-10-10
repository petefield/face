using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Brain.Skills;

/// <summary>Builds the JSON Schema a skill advertises to the model from its argument type.</summary>
public static class SkillSchema
{
    private static readonly JsonSchemaExporterOptions ExporterOptions = new()
    {
        TreatNullObliviousAsNonNullable = true,
        TransformSchemaNode = Transform,
    };

    /// <summary>
    /// Produces a schema for <typeparamref name="T"/>. Property names follow the same camelCase
    /// convention used when the arguments are parsed, <see cref="DescriptionAttribute"/> becomes the
    /// schema description, and enums become a list of allowed string values. A description on an
    /// individual enum member is appended to the property's description, since JSON Schema has
    /// nowhere to put per-value documentation.
    /// </summary>
    public static BinaryData Create<T>() =>
        BinaryData.FromString(SkillArguments.SerializerOptions.GetJsonSchemaAsNode(typeof(T), ExporterOptions).ToJsonString());

    private static JsonNode Transform(JsonSchemaExporterContext context, JsonNode schema)
    {
        if (schema is not JsonObject obj)
        {
            return schema;
        }

        var descriptions = new List<string>();

        var attributeProvider = context.PropertyInfo?.AttributeProvider ?? context.TypeInfo.Type;
        if (attributeProvider?.GetCustomAttributes(typeof(DescriptionAttribute), inherit: true).FirstOrDefault()
            is DescriptionAttribute description)
        {
            descriptions.Add(description.Description);
        }

        if (DescribeEnumMembers(context.TypeInfo.Type) is { } memberDescriptions)
        {
            descriptions.Add(memberDescriptions);
        }

        if (descriptions.Count > 0)
        {
            obj["description"] = string.Join(" ", descriptions);
        }

        if (obj.ContainsKey("properties"))
        {
            obj["additionalProperties"] = false;
        }

        return obj;
    }

    private static string? DescribeEnumMembers(Type type)
    {
        if (!type.IsEnum)
        {
            return null;
        }

        var namingPolicy = SkillArguments.SerializerOptions.PropertyNamingPolicy ?? JsonNamingPolicy.CamelCase;

        var described = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (
                Value: namingPolicy.ConvertName(field.Name),
                Text: field.GetCustomAttribute<DescriptionAttribute>()?.Description))
            .Where(member => !string.IsNullOrWhiteSpace(member.Text))
            .Select(member => $"\"{member.Value}\" is {member.Text}")
            .ToList();

        return described.Count == 0 ? null : $"Allowed values: {string.Join("; ", described)}.";
    }
}
