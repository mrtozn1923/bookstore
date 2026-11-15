using System.Text.Json;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace Bookstore.Web.Tests;

// The JSON-LD blocks of one rendered page, flattened into a node list
public sealed class JsonLdPage(string rawHtml, IHtmlDocument document, IReadOnlyList<JsonElement> nodes)
{
    public string RawHtml { get; } = rawHtml;
    public IHtmlDocument Document { get; } = document;
    public IReadOnlyList<JsonElement> Nodes { get; } = nodes;

    public static async Task<JsonLdPage> LoadAsync(HttpClient client, string path)
    {
        string html = await client.GetStringAsync(path, TestContext.Current.CancellationToken);
        // a real HTML parser, not a regex: attribute values may be entity-encoded
        IHtmlDocument document = new HtmlParser().ParseDocument(html);
        var nodes = new List<JsonElement>();
        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
        {
            JsonElement root = JsonDocument.Parse(script.TextContent).RootElement;
            Assert.Equal("https://schema.org", root.GetProperty("@context").GetString());
            nodes.AddRange(root.TryGetProperty("@graph", out JsonElement graph) ? graph.EnumerateArray() : [root]);
        }
        return new JsonLdPage(html, document, nodes);
    }

    public JsonElement Single(string type) => Assert.Single(Nodes, n => HasType(n, type));

    // "@type" is a string or, for co-typed nodes such as ["Product", "Book"], an array
    public static bool HasType(JsonElement node, string type)
    {
        JsonElement value = node.GetProperty("@type");
        return value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Any(t => t.GetString() == type)
            : value.GetString() == type;
    }

    public bool HasNode(string id) => Nodes.Any(n => n.TryGetProperty("@id", out JsonElement x) && x.GetString() == id);
}
