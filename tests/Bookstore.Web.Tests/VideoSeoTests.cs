using System.Net;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Bookstore.Web.Videos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bookstore.Web.Tests;

public sealed class VideoSeoTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly XNamespace Sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace V = "http://www.google.com/schemas/sitemap-video/1.1";

    private readonly HttpClient _client = factory.CreateClient();

    private async Task<IDocument> PageAsync(string path) =>
        new HtmlParser().ParseDocument(await _client.GetStringAsync(path));

    private static JsonElement? VideoObject(IDocument doc) =>
        doc.QuerySelectorAll("script[type='application/ld+json']")
            .Select(s => JsonDocument.Parse(s.TextContent).RootElement)
            .Where(e => e.TryGetProperty("@type", out JsonElement t) && t.GetString() == "VideoObject")
            .Cast<JsonElement?>().FirstOrDefault();

    public static TheoryData<string> Slugs => [.. new VideoCatalog().Videos.Select(v => v.Slug)];

    [Theory]
    [MemberData(nameof(Slugs))]
    public async Task Sitemap_page_and_json_ld_describe_the_same_video(string slug)
    {
        XDocument sitemap = XDocument.Parse(await _client.GetStringAsync("/sitemaps/videos.xml"));
        XElement entry = sitemap.Root!.Elements(Sm + "url")
            .Single(u => u.Element(Sm + "loc")!.Value == $"https://bookstore.example/videos/{slug}")
            .Element(V + "video")!;
        IDocument page = await PageAsync($"/videos/{slug}");
        JsonElement ld = VideoObject(page)!.Value;

        string thumbnail = entry.Element(V + "thumbnail_loc")!.Value;
        Assert.Equal(thumbnail, ld.GetProperty("thumbnailUrl").GetString());
        Assert.Equal(thumbnail, page.QuerySelector("meta[property='og:image']")!.GetAttribute("content"));
        Assert.Equal(entry.Element(V + "title")!.Value, ld.GetProperty("name").GetString());
        Assert.Equal(page.QuerySelector("h1")!.TextContent, ld.GetProperty("name").GetString());

        int seconds = int.Parse(entry.Element(V + "duration")!.Value);
        Assert.Equal(seconds, (int)XmlConvert.ToTimeSpan(ld.GetProperty("duration").GetString()!).TotalSeconds);
        Assert.InRange(seconds, 1, 28_800);
        Assert.True(entry.Element(V + "description")!.Value.Length <= 2048);

        // en az biri: content_loc tercih edilir, player_loc yedek; ikisi de watch page'den farklı olmalı
        string? content = entry.Element(V + "content_loc")?.Value;
        string? player = entry.Element(V + "player_loc")?.Value;
        Assert.True(content is not null || player is not null);
        Assert.NotEqual(entry.Parent!.Element(Sm + "loc")!.Value, content ?? player);
        if (content is not null) Assert.Equal(content, ld.GetProperty("contentUrl").GetString());
        if (player is not null) Assert.Equal(player, ld.GetProperty("embedUrl").GetString());
    }

    [Fact]
    public async Task Clips_follow_key_moments_rules()
    {
        IDocument page = await PageAsync("/videos/dune-reading-guide");
        JsonElement ld = VideoObject(page)!.Value;
        int duration = (int)XmlConvert.ToTimeSpan(ld.GetProperty("duration").GetString()!).TotalSeconds;
        JsonElement[] clips = [.. ld.GetProperty("hasPart").EnumerateArray()];

        Assert.True(duration >= 30); // Clip ve SeekToAction için en az 30 saniye
        Assert.Equal(clips.Length, clips.Select(c => c.GetProperty("startOffset").GetInt32()).Distinct().Count());
        foreach (JsonElement clip in clips)
        {
            int start = clip.GetProperty("startOffset").GetInt32();
            Assert.InRange(start, 0, duration - 1);
            Assert.Equal($"https://bookstore.example/videos/dune-reading-guide?t={start}", clip.GetProperty("url").GetString());
        }
    }

    [Fact]
    public async Task Timestamp_url_is_the_same_page_with_a_clean_canonical()
    {
        HttpResponseMessage response = await _client.GetAsync("/videos/dune-reading-guide?t=310");
        IDocument page = new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("https://bookstore.example/videos/dune-reading-guide", page.QuerySelector("link[rel=canonical]")!.GetAttribute("href"));
        Assert.EndsWith("dune-reading-guide.mp4#t=310", page.QuerySelector("video source")!.GetAttribute("src"));
    }

    [Theory]
    [InlineData("/books/dune")]
    [InlineData("/videos")]
    public async Task Pages_that_only_show_or_list_a_video_are_not_marked_as_watch_pages(string path)
    {
        IDocument page = await PageAsync(path);
        Assert.Null(VideoObject(page));
        Assert.Contains(page.QuerySelectorAll("a"), a => a.GetAttribute("href")!.StartsWith("/videos/"));
    }

    [Fact]
    public async Task Youtube_video_uses_player_loc_without_seek_markup()
    {
        JsonElement ld = VideoObject(await PageAsync("/videos/foundation-in-twelve-minutes"))!.Value;
        Assert.False(ld.TryGetProperty("contentUrl", out _));
        Assert.False(ld.TryGetProperty("potentialAction", out _)); // bölümler YouTube açıklamasında
        Assert.StartsWith("https://www.youtube-nocookie.com/embed/", ld.GetProperty("embedUrl").GetString());
    }
}
