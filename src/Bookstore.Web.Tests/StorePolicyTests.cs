using System.Text.Json;
using Bookstore.Web.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bookstore.Web.Tests;

public sealed class StorePolicyTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string StoreId = "https://bookstore.example/#organization";

    private static JsonElement StoreNode(JsonLdPage page) =>
        Assert.Single(page.Nodes, n => n.TryGetProperty("@id", out JsonElement id) && id.GetString() == StoreId);

    [Theory]
    [InlineData("/shipping-and-returns", "tr")]
    [InlineData("/en/shipping-and-returns", "en")]
    public async Task Policy_page_markup_matches_the_policy_text(string path, string lang)
    {
        JsonLdPage page = await JsonLdPage.LoadAsync(factory.CreateClient(), path);
        JsonElement store = StoreNode(page);
        Assert.Equal("OnlineStore", store.GetProperty("@type").GetString());

        JsonElement returns = Assert.Single(store.GetProperty("hasMerchantReturnPolicy").EnumerateArray());
        string returnText = page.Document.QuerySelector(".return-window")!.TextContent;
        Assert.Contains($" {returns.GetProperty("merchantReturnDays").GetInt32()} ", returnText);

        // every shipping rate in the markup must appear in the visible fee sentence
        string feeText = page.Document.QuerySelector(".shipping-fee")!.TextContent;
        JsonElement shipping = Assert.Single(store.GetProperty("hasShippingService").EnumerateArray());
        foreach (JsonElement condition in shipping.GetProperty("shippingConditions").EnumerateArray())
        {
            decimal rate = condition.GetProperty("shippingRate").GetProperty("value").GetDecimal();
            Assert.True(rate == 0 || feeText.Contains(PricingService.Format(rate, lang)), $"{rate} is not on the page");
            Assert.Equal("TRY", condition.GetProperty("shippingRate").GetProperty("currency").GetString());
        }
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/books/dune-paperback")]
    [InlineData("/en/blog/csharp-books-for-beginners")]
    [InlineData("/authors/ada-kaplan")]
    public async Task Other_pages_carry_only_the_short_store_node(string path)
    {
        JsonLdPage page = await JsonLdPage.LoadAsync(factory.CreateClient(), path);
        JsonElement store = StoreNode(page);

        Assert.Equal("OnlineStore", store.GetProperty("@type").GetString());
        Assert.False(store.TryGetProperty("hasMerchantReturnPolicy", out _));
        Assert.False(store.TryGetProperty("hasShippingService", out _));
    }

    [Fact]
    public async Task Offer_points_to_the_policies_defined_on_the_policy_page()
    {
        HttpClient client = factory.CreateClient();
        JsonLdPage policyPage = await JsonLdPage.LoadAsync(client, "/shipping-and-returns");
        JsonLdPage bookPage = await JsonLdPage.LoadAsync(client, "/books/dune-paperback");

        JsonElement store = StoreNode(policyPage);
        JsonElement offer = bookPage.Single("Product").GetProperty("offers");

        // the @id references cross pages, so the test resolves them against the policy page
        Assert.Equal(
            store.GetProperty("hasMerchantReturnPolicy")[0].GetProperty("@id").GetString(),
            offer.GetProperty("hasMerchantReturnPolicy").GetProperty("@id").GetString());
        Assert.Equal(
            store.GetProperty("hasShippingService")[0].GetProperty("@id").GetString(),
            offer.GetProperty("shippingDetails").GetProperty("hasShippingService").GetProperty("@id").GetString());
    }
}
