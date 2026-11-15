using System.Text.Json;
using Bookstore.Web.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bookstore.Web.Tests;

public sealed class BookProductTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly DateTimeOffset DuringSale = new(2026, 11, 15, 12, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset AfterSale = new(2026, 12, 1, 0, 0, 0, TimeSpan.FromHours(3));

    private HttpClient ClientAt(DateTimeOffset now) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(now)))).CreateClient();

    [Theory]
    [InlineData("/books/dune-paperback", "tr")]
    [InlineData("/en/books/dune-paperback", "en")]
    [InlineData("/books/dune-hardcover", "tr")]
    public async Task Product_markup_matches_the_visible_page(string path, string lang)
    {
        JsonLdPage page = await JsonLdPage.LoadAsync(ClientAt(DuringSale), path);
        JsonElement product = page.Single("Product");
        JsonElement offer = product.GetProperty("offers");

        Assert.True(JsonLdPage.HasType(product, "Book"), "isbn is only valid when Product is co-typed with Book");
        Assert.Equal(page.Document.QuerySelector("h1")!.TextContent, product.GetProperty("name").GetString());

        string isbn = product.GetProperty("isbn").GetString()!;
        Assert.True(Isbn.IsValid13(isbn), $"bad ISBN-13 check digit: {isbn}");
        Assert.Equal(page.Document.QuerySelector(".isbn span")!.TextContent, isbn);

        // merchant listings need an Offer with a price above zero
        decimal price = offer.GetProperty("price").GetDecimal();
        Assert.True(price > 0, "merchant listings require price > 0");
        Assert.Equal("TRY", offer.GetProperty("priceCurrency").GetString());
        Assert.Equal(page.Document.QuerySelector(".price-now")!.TextContent, PricingService.Format(price, lang));

        bool inStock = offer.GetProperty("availability").GetString() == "https://schema.org/InStock";
        Assert.Equal(inStock, page.Document.QuerySelector("form button") is not null);
        Assert.True(page.HasNode(offer.GetProperty("seller").GetProperty("@id").GetString()!), "dangling seller @id");
        Assert.All(product.GetProperty("image").EnumerateArray(), image => Assert.StartsWith("https://", image.GetString()));
    }

    [Fact]
    public async Task Running_campaign_adds_strikethrough_price_and_sale_window()
    {
        JsonLdPage page = await JsonLdPage.LoadAsync(ClientAt(DuringSale), "/books/dune-paperback");
        JsonElement offer = page.Single("Product").GetProperty("offers");
        JsonElement regular = offer.GetProperty("priceSpecification");

        Assert.Equal("https://schema.org/StrikethroughPrice", regular.GetProperty("priceType").GetString());
        Assert.True(regular.GetProperty("price").GetDecimal() > offer.GetProperty("price").GetDecimal());
        Assert.True(offer.GetProperty("validThrough").GetDateTimeOffset() > DuringSale);
        Assert.NotNull(page.Document.QuerySelector(".price-was"));
    }

    [Fact]
    public async Task Ended_campaign_leaves_no_sale_markup_behind()
    {
        JsonLdPage page = await JsonLdPage.LoadAsync(ClientAt(AfterSale), "/books/dune-paperback");
        JsonElement offer = page.Single("Product").GetProperty("offers");

        Assert.Equal(450.00m, offer.GetProperty("price").GetDecimal());
        Assert.False(offer.TryGetProperty("priceSpecification", out _));
        Assert.False(offer.TryGetProperty("validThrough", out _));
        Assert.Null(page.Document.QuerySelector(".price-was"));
    }
}
