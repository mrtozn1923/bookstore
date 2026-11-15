using System.Xml;

namespace Bookstore.Web.Videos;

public static class VideoJsonLd
{
    // yalnız watch page'de kullanılır: videonun asıl içerik olduğu tek sayfa
    public static Dictionary<string, object> Build(Video video, string watchUrl, string imageBase)
    {
        Dictionary<string, object> node = new()
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "VideoObject",
            ["name"] = video.Title,
            ["description"] = video.Description,
            ["thumbnailUrl"] = imageBase + video.ThumbnailPath,
            ["uploadDate"] = video.UploadDate.ToString("yyyy-MM-ddTHH:mm:sszzz"), // saat dilimiyle
            ["duration"] = XmlConvert.ToString(TimeSpan.FromSeconds(video.DurationSeconds)), // PT12M34S
        };
        if (video.ContentUrl is not null)
            node["contentUrl"] = video.ContentUrl;
        if (video.PlayerUrl is not null)
            node["embedUrl"] = video.PlayerUrl;
        if (video.Expires is DateOnly expires)
            node["expires"] = expires.ToString("yyyy-MM-dd");
        if (video.AllowedRegions is { Length: > 0 } regions)
            node["regionsAllowed"] = regions;

        if (video.Chapters is { Length: > 0 } chapters)
        {
            // key moments'ı biz belirliyoruz: her bölüm bir Clip, url aynı path + ?t=
            node["hasPart"] = chapters.Select((c, i) => new Dictionary<string, object>
            {
                ["@type"] = "Clip",
                ["name"] = c.Label,
                ["startOffset"] = c.StartSeconds,
                ["endOffset"] = i + 1 < chapters.Length ? chapters[i + 1].StartSeconds : video.DurationSeconds,
                ["url"] = $"{watchUrl}?t={c.StartSeconds}",
            }).ToArray();
        }
        else if (video.ContentUrl is not null)
        {
            // bölüm yoksa: Google key moments'ı kendisi bulsun, URL'de zamanın nereye yazıldığını söyle
            node["potentialAction"] = new Dictionary<string, object>
            {
                ["@type"] = "SeekToAction",
                ["target"] = $"{watchUrl}?t={{seek_to_second_number}}",
                ["startOffset-input"] = "required name=seek_to_second_number",
            };
        }
        // YouTube videosunda bölümler YouTube açıklamasındaki zaman damgalarından gelir
        return node;
    }
}
