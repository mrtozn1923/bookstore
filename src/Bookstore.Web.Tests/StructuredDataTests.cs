using System.Text.Json;
using Bookstore.Web.Content;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Web.Tests;

public sealed class StructuredDataTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/blog/csharp-books-for-beginners")]
    [InlineData("/en/blog/csharp-books-for-beginners")]
    public async Task BlogPosting_matches_visible_content(string path)
    {
        JsonLdPage page = await JsonLdPage.LoadAsync(factory.CreateClient(), path);
        JsonElement posting = page.Single("BlogPosting");

        Assert.Equal(page.Document.QuerySelector("h1")!.TextContent, posting.GetProperty("headline").GetString());
        DateTimeOffset published = posting.GetProperty("datePublished").GetDateTimeOffset();
        Assert.Matches(@"[+-]\d{2}:\d{2}$|Z$", posting.GetProperty("datePublished").GetString()!);
        if (posting.TryGetProperty("dateModified", out JsonElement modified))
        {
            Assert.True(modified.GetDateTimeOffset() >= published, "dateModified is earlier than datePublished");
        }
        foreach (JsonElement image in posting.GetProperty("image").EnumerateArray())
        {
            Assert.StartsWith("https://", image.GetString());
        }
        // every author reference must resolve to a node on the same page
        foreach (JsonElement author in posting.GetProperty("author").EnumerateArray())
        {
            Assert.True(page.HasNode(author.GetProperty("@id").GetString()!), "dangling author @id");
        }
    }

    [Fact]
    public async Task Person_id_is_the_same_on_post_profile_and_both_languages()
    {
        HttpClient client = factory.CreateClient();
        JsonLdPage trPost = await JsonLdPage.LoadAsync(client, "/blog/csharp-books-for-beginners");
        JsonLdPage enPost = await JsonLdPage.LoadAsync(client, "/en/blog/csharp-books-for-beginners");
        JsonLdPage profile = await JsonLdPage.LoadAsync(client, "/authors/ada-kaplan");

        string? personId = trPost.Single("Person").GetProperty("@id").GetString();
        Assert.Equal(personId, enPost.Single("Person").GetProperty("@id").GetString());
        Assert.Equal(personId, profile.Single("ProfilePage").GetProperty("mainEntity").GetProperty("@id").GetString());
    }

    [Fact]
    public async Task Hostile_title_cannot_close_the_script_element()
    {
        const string title = "Why </script><script>alert(1)</script> breaks pages";
        var catalog = new PostCatalog();
        Post original = catalog.Posts[0];
        Post hostile = original with { Slug = "hostile", Title = new Localized(title, title) };

        HttpClient client = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton(new PostCatalog { Posts = [hostile] }))).CreateClient();
        JsonLdPage page = await JsonLdPage.LoadAsync(client, "/blog/hostile");

        Assert.DoesNotContain("<script>alert(1)", page.RawHtml);
        Assert.Equal(title, page.Single("BlogPosting").GetProperty("headline").GetString());
    }
}
