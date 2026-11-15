using System.Text.Json;
using Bookstore.Web.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bookstore.Web.Tests;

public sealed class ReviewMarkupTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/books/dune-paperback", "tr")]
    [InlineData("/en/books/dune-paperback", "en")]
    public async Task Rating_markup_matches_what_the_reader_sees(string path, string lang)
    {
        JsonLdPage page = await JsonLdPage.LoadAsync(factory.CreateClient(), path);
        JsonElement product = page.Single("Product");
        JsonElement rating = product.GetProperty("aggregateRating");

        string visible = page.Document.QuerySelector(".rating-summary")!.TextContent;
        Assert.Equal(visible, BookTexts.RatingSummary(
            rating.GetProperty("ratingValue").GetDecimal(),
            rating.GetProperty("ratingCount").GetInt32(),
            rating.GetProperty("reviewCount").GetInt32(), lang));

        // every review in the markup must be readable on the page, with its author
        string[] texts = [.. page.Document.QuerySelectorAll(".review-text").Select(e => e.TextContent)];
        string[] authors = [.. page.Document.QuerySelectorAll(".review-author").Select(e => e.TextContent)];
        foreach (JsonElement review in product.GetProperty("review").EnumerateArray())
        {
            Assert.Contains(review.GetProperty("reviewBody").GetString(), texts);
            Assert.Contains(review.GetProperty("author").GetProperty("name").GetString(), authors);
        }
    }

    [Fact]
    public async Task Rejected_and_pending_reviews_stay_out_of_page_and_markup()
    {
        JsonLdPage page = await JsonLdPage.LoadAsync(factory.CreateClient(), "/en/books/dune-paperback");

        Assert.DoesNotContain("Cheap watches", page.RawHtml);
        Assert.DoesNotContain("bent cover", page.RawHtml);
        Assert.Equal(7, page.Single("Product").GetProperty("aggregateRating").GetProperty("ratingCount").GetInt32());
    }

    [Fact]
    public async Task Book_without_reviews_publishes_no_rating()
    {
        JsonLdPage page = await JsonLdPage.LoadAsync(factory.CreateClient(), "/books/dune-hardcover");
        JsonElement product = page.Single("Product");

        Assert.False(product.TryGetProperty("aggregateRating", out _));
        Assert.False(product.TryGetProperty("review", out _));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/books/dune-paperback")]
    public async Task Store_never_rates_itself(string path)
    {
        JsonLdPage page = await JsonLdPage.LoadAsync(factory.CreateClient(), path);
        JsonElement store = Assert.Single(page.Nodes, n =>
            n.TryGetProperty("@id", out JsonElement id) && id.GetString() == "https://bookstore.example/#organization");

        // self-serving: reviews about Bookstore on Bookstore's own site are not eligible
        Assert.False(store.TryGetProperty("aggregateRating", out _));
        Assert.False(store.TryGetProperty("review", out _));
    }
}
