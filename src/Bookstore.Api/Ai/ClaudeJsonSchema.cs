using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Bookstore.Api.Ai;

// Builds a JSON Schema from a C# type with System.Text.Json and adapts it to Claude's structured outputs subset.
public static class ClaudeJsonSchema
{
    // Not JsonSerializerDefaults.Web: that allows numbers as strings, and the schema would say so.
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        RespectNullableAnnotations = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    private static readonly JsonSchemaExporterOptions ExporterOptions = new()
    {
        TreatNullObliviousAsNonNullable = true,
        TransformSchemaNode = (context, schema) =>
        {
            if (schema is not JsonObject node)
            {
                return schema;
            }

            // Claude requires additionalProperties: false on every object.
            if (IsObjectSchema(node))
            {
                node["additionalProperties"] = false;
            }

            // [Description] on a property becomes the schema's description; the model reads it.
            DescriptionAttribute? description = context.PropertyInfo?.AttributeProvider?
                .GetCustomAttributes(typeof(DescriptionAttribute), inherit: true)
                .OfType<DescriptionAttribute>()
                .FirstOrDefault();
            if (description is not null)
            {
                node["description"] = description.Description;
            }

            return node;
        },
    };

    public static JsonObject For<T>() =>
        (JsonObject)SerializerOptions.GetJsonSchemaAsNode(typeof(T), ExporterOptions);

    public static Dictionary<string, JsonElement> AsSchemaDictionary<T>() =>
        For<T>().ToDictionary(kv => kv.Key, kv => JsonSerializer.SerializeToElement(kv.Value));

    private static bool IsObjectSchema(JsonObject node) => node["type"] switch
    {
        JsonValue value => value.GetValue<string>() == "object",
        JsonArray types => types.Any(t => t?.GetValue<string>() == "object"),
        _ => false,
    };
}
