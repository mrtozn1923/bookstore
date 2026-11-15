using System.Text;
using System.Xml;
using Bookstore.Web.Seo;

namespace Bookstore.Web.Videos;

public static class VideoSitemapWriter
{
    private const string Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private const string VideoNs = "http://www.google.com/schemas/sitemap-video/1.1";
    private static readonly XmlWriterSettings Settings = new() { Async = true, Encoding = new UTF8Encoding(false) };

    public static async Task WriteAsync(Stream output, IEnumerable<Video> videos, SiteOptions site, CancellationToken ct)
    {
        await using XmlWriter xml = XmlWriter.Create(output, Settings);
        await xml.WriteStartDocumentAsync();
        await xml.WriteStartElementAsync(null, "urlset", Ns);
        await xml.WriteAttributeStringAsync("xmlns", "video", null, VideoNs);

        foreach (Video video in videos)
        {
            ct.ThrowIfCancellationRequested();
            // iki dildeki watch page; video aynı, metadata aynı
            foreach (string loc in (string[])[$"{site.BaseUrl}/videos/{video.Slug}", $"{site.BaseUrl}/en/videos/{video.Slug}"])
            {
                await xml.WriteStartElementAsync(null, "url", Ns);
                await xml.WriteElementStringAsync(null, "loc", Ns, loc);
                await xml.WriteStartElementAsync("video", "video", VideoNs);
                await Element(xml, "thumbnail_loc", site.AbsoluteImageBase + video.ThumbnailPath);
                await Element(xml, "title", video.Title);
                await Element(xml, "description", video.Description);
                // content_loc tercih edilir; yalnız player varsa player_loc
                if (video.ContentUrl is not null)
                    await Element(xml, "content_loc", video.ContentUrl);
                if (video.PlayerUrl is not null)
                    await Element(xml, "player_loc", video.PlayerUrl);
                await Element(xml, "duration", video.DurationSeconds.ToString());
                await Element(xml, "publication_date", video.UploadDate.ToString("yyyy-MM-ddTHH:mm:sszzz"));
                if (video.Expires is DateOnly expires)
                    await Element(xml, "expiration_date", expires.ToString("yyyy-MM-dd"));
                if (video.AllowedRegions is { Length: > 0 } regions)
                {
                    await xml.WriteStartElementAsync("video", "restriction", VideoNs);
                    await xml.WriteAttributeStringAsync(null, "relationship", null, "allow");
                    await xml.WriteStringAsync(string.Join(' ', regions));
                    await xml.WriteEndElementAsync();
                }
                await xml.WriteEndElementAsync();
                await xml.WriteEndElementAsync();
            }
        }
        await xml.WriteEndElementAsync();
    }

    private static Task Element(XmlWriter xml, string name, string value) =>
        xml.WriteElementStringAsync("video", name, VideoNs, value);
}
