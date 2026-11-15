using System.Text.Json.Serialization;

namespace Bookstore.Web.StructuredData;

// Every node carries @type and @id first; the remaining properties use schema.org names
public abstract record SchemaNode(
    [property: JsonPropertyName("@type"), JsonPropertyOrder(-2)] string Type,
    [property: JsonPropertyName("@id"), JsonPropertyOrder(-1)] string Id);

// A reference to a node that is fully described elsewhere in the graph
public sealed record IdRef([property: JsonPropertyName("@id")] string Id);

public sealed record OrganizationNode(
    string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("logo")] string? Logo = null,
    [property: JsonPropertyName("sameAs")] IReadOnlyList<string>? SameAs = null)
    : SchemaNode("Organization", Id);

public sealed record WebSiteNode(
    string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("inLanguage")] IReadOnlyList<string> InLanguage,
    [property: JsonPropertyName("publisher")] IdRef Publisher)
    : SchemaNode("WebSite", Id);

public sealed record PersonNode(
    string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("jobTitle")] string? JobTitle = null,
    [property: JsonPropertyName("worksFor")] IdRef? WorksFor = null,
    [property: JsonPropertyName("sameAs")] IReadOnlyList<string>? SameAs = null)
    : SchemaNode("Person", Id);

public sealed record ProfilePageNode(
    string Id,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("inLanguage")] string InLanguage,
    [property: JsonPropertyName("isPartOf")] IdRef IsPartOf,
    [property: JsonPropertyName("mainEntity")] IdRef MainEntity)
    : SchemaNode("ProfilePage", Id);

public sealed record BlogPostingNode(
    string Id,
    [property: JsonPropertyName("headline")] string Headline,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("image")] IReadOnlyList<string> Image,
    [property: JsonPropertyName("datePublished")] DateTimeOffset DatePublished,
    [property: JsonPropertyName("dateModified")] DateTimeOffset? DateModified,
    [property: JsonPropertyName("author")] IReadOnlyList<IdRef> Author,
    [property: JsonPropertyName("publisher")] IdRef Publisher,
    [property: JsonPropertyName("mainEntityOfPage")] string MainEntityOfPage,
    [property: JsonPropertyName("inLanguage")] string InLanguage,
    [property: JsonPropertyName("isPartOf")] IdRef IsPartOf)
    : SchemaNode("BlogPosting", Id);

public sealed record ListItemNode(
    [property: JsonPropertyName("position")] int Position,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("item")] string? Item)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "ListItem";
}

public sealed record BreadcrumbListNode(
    string Id,
    [property: JsonPropertyName("itemListElement")] IReadOnlyList<ListItemNode> ItemListElement)
    : SchemaNode("BreadcrumbList", Id);

// Root document: one @context, all nodes in @graph
public sealed record JsonLdDocument([property: JsonPropertyName("@graph")] IReadOnlyList<object> Graph)
{
    [JsonPropertyName("@context"), JsonPropertyOrder(-1)]
    public string Context => "https://schema.org";
}
