using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Bookstore.Contracts;

namespace Bookstore.Contracts.Tests;

// Every message in Bookstore.Contracts has an approved JSON schema in schemas/. A change to a record that changes
// what goes over the wire fails here, and the diff shows up in the pull request with the new schema.
public sealed class MessageSchemaTests
{
    // camelCase property names, as in the payloads Wolverine puts on RabbitMQ
    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

    public static TheoryData<string> Messages() =>
    [
        .. typeof(Exchanges).Assembly.GetTypes()
            .Where(t => t is { IsPublic: true, IsClass: true, Namespace: not null } && t.GetMethod("<Clone>$") is not null)
            .Select(t => t.FullName!)
            .Order()
    ];

    [Theory]
    [MemberData(nameof(Messages))]
    public void Wire_format_matches_the_approved_schema(string typeName)
    {
        Type type = typeof(Exchanges).Assembly.GetType(typeName)!;
        string actual = JsonSchemaExporter.GetJsonSchemaAsNode(Wire, type).ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        string approvedPath = Path.Combine(SourceFolder(), "schemas", $"{typeName}.json");

        if (!File.Exists(approvedPath))
        {
            File.WriteAllText(approvedPath + ".received", actual);
            Assert.Fail($"No approved schema for {typeName}. Review {approvedPath}.received and rename it to approve.");
        }

        JsonNode approved = JsonNode.Parse(File.ReadAllText(approvedPath))!;
        JsonNode current = JsonNode.Parse(actual)!;
        string[] removed = [.. Properties(approved).Except(Properties(current))];
        string[] added = [.. Properties(current).Except(Properties(approved))];

        // Removing or renaming a field (or changing its type) breaks consumers that are already deployed
        Assert.True(removed.Length == 0, $"{typeName}: breaking change, removed or changed: {string.Join(", ", removed)}");
        // Adding a field is allowed by our versioning rule (post 06), but the approved schema has to be updated on purpose
        Assert.True(added.Length == 0, $"{typeName}: new fields {string.Join(", ", added)}; update schemas/{typeName}.json");
    }

    // "orderId:string/uuid", "lines[].isbn:string", ...: one entry per field with its JSON type and format
    private static IEnumerable<string> Properties(JsonNode schema, string prefix = "")
    {
        if (schema["properties"] is not JsonObject properties)
        {
            yield break;
        }
        foreach ((string name, JsonNode? property) in properties)
        {
            string type = property?["type"]?.ToJsonString() ?? "?";
            string format = property?["format"]?.GetValue<string>() is { } f ? "/" + f : "";
            yield return $"{prefix}{name}:{type}{format}";
            if (property?["items"] is JsonNode items)
            {
                foreach (string nested in Properties(items, $"{prefix}{name}[]."))
                {
                    yield return nested;
                }
            }
        }
    }

    private static string SourceFolder([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
