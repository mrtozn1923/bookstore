using System.Text.Json;

namespace Bookstore.Web.Tests;

// Runs only when SMOKE_BASE_URL is set, e.g. right after a deploy
public sealed class DeployedSmokeTests
{
    private static readonly string? BaseUrl = Environment.GetEnvironmentVariable("SMOKE_BASE_URL");

    [Theory]
    [InlineData("/", "WebSite")]
    [InlineData("/blog/csharp-books-for-beginners", "BlogPosting")]
    [InlineData("/en/blog/csharp-books-for-beginners", "BlogPosting")]
    [InlineData("/authors/ada-kaplan", "ProfilePage")]
    public async Task Live_page_serves_expected_type(string path, string expectedType)
    {
        if (string.IsNullOrEmpty(BaseUrl))
        {
            Assert.Skip("SMOKE_BASE_URL is not set");
        }

        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        JsonLdPage page = await JsonLdPage.LoadAsync(client, path);

        JsonElement node = page.Single(expectedType);
        Assert.StartsWith("https://", node.GetProperty("@id").GetString());
    }
}
