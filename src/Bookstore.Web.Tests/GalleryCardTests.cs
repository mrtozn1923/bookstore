using System.Text.Json;

namespace Bookstore.Web.Tests;

public sealed class GalleryCardTests
{
    private static JsonElement Load(string file) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GalleryCards", file))).RootElement;

    [Theory]
    [InlineData("event.json")]
    [InlineData("discussion-forum.json")]
    [InlineData("qa-page.json")]
    [InlineData("image-metadata.json")]
    public void Card_meets_the_required_properties(string file)
    {
        JsonElement card = Load(file);

        Assert.Equal("https://schema.org", card.GetProperty("@context").GetString());
        Assert.Empty(GalleryRules.Check(card));
    }

    [Theory]
    [InlineData("event-online-only.json", "location must be a Place (virtual-only events are not supported)")]
    [InlineData("event-online-only.json", "name is missing")]
    [InlineData("qa-product-page.json", "mainEntity must be a single Question")]
    public void Rule_catches_a_known_mistake(string file, string expected)
    {
        Assert.Contains(expected, GalleryRules.Check(Load(file)));
    }
}
