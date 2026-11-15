using System.Text.Json.Serialization;

namespace Bookstore.Web.StructuredData;

public sealed record NamedNode(
    [property: JsonPropertyName("@type")] string Type,
    [property: JsonPropertyName("name")] string Name);

public sealed record UnitPriceSpecificationNode(
    [property: JsonPropertyName("priceType")] string PriceType,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("priceCurrency")] string PriceCurrency)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "UnitPriceSpecification";
}

public sealed record OfferNode(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("priceCurrency")] string PriceCurrency,
    [property: JsonPropertyName("availability")] string Availability,
    [property: JsonPropertyName("itemCondition")] string ItemCondition,
    [property: JsonPropertyName("seller")] IdRef Seller,
    [property: JsonPropertyName("priceSpecification")] UnitPriceSpecificationNode? PriceSpecification = null,
    [property: JsonPropertyName("validFrom")] DateTimeOffset? ValidFrom = null,
    [property: JsonPropertyName("validThrough")] DateTimeOffset? ValidThrough = null,
    [property: JsonPropertyName("hasMerchantReturnPolicy")] IdRef? HasMerchantReturnPolicy = null,
    [property: JsonPropertyName("shippingDetails")] OfferShippingDetailsNode? ShippingDetails = null)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "Offer";
}

// Book is co-typed with Product: isbn and bookFormat come from Book, offers from Product
public sealed record BookProductNode(
    [property: JsonPropertyName("@id"), JsonPropertyOrder(-1)] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("image")] IReadOnlyList<string> Image,
    [property: JsonPropertyName("isbn")] string Isbn,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("author")] NamedNode Author,
    [property: JsonPropertyName("bookFormat")] string BookFormat,
    [property: JsonPropertyName("numberOfPages")] int NumberOfPages,
    [property: JsonPropertyName("inLanguage")] string InLanguage,
    [property: JsonPropertyName("offers")] OfferNode Offers,
    [property: JsonPropertyName("aggregateRating")] AggregateRatingNode? AggregateRating = null,
    [property: JsonPropertyName("review")] IReadOnlyList<ReviewNode>? Review = null)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-2)]
    public IReadOnlyList<string> Type => ["Product", "Book"];
}
