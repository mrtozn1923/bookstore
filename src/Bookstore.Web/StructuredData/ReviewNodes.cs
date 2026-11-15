using System.Text.Json.Serialization;

namespace Bookstore.Web.StructuredData;

public sealed record AggregateRatingNode(
    [property: JsonPropertyName("ratingValue")] decimal RatingValue,
    [property: JsonPropertyName("ratingCount")] int RatingCount,
    [property: JsonPropertyName("reviewCount")] int ReviewCount)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "AggregateRating";

    [JsonPropertyName("bestRating")]
    public int BestRating => 5;

    [JsonPropertyName("worstRating")]
    public int WorstRating => 1;
}

public sealed record RatingNode([property: JsonPropertyName("ratingValue")] int RatingValue)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "Rating";

    [JsonPropertyName("bestRating")]
    public int BestRating => 5;
}

public sealed record ReviewNode(
    [property: JsonPropertyName("author")] NamedNode Author,
    [property: JsonPropertyName("datePublished")] string DatePublished,
    [property: JsonPropertyName("reviewBody")] string ReviewBody,
    [property: JsonPropertyName("reviewRating")] RatingNode ReviewRating)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "Review";
}
