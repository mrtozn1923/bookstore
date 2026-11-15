using System.Text.Json;
using Bookstore.Web.Seo;
using Bookstore.Web.Videos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Bookstore.Web.Pages.Videos;

public sealed class WatchModel(VideoCatalog videos, IOptions<SiteOptions> options) : PageModel
{
    private readonly SiteOptions _site = options.Value;

    public Video Video { get; private set; } = null!;
    public int Start { get; private set; }
    public string Lang => Request.Path.StartsWithSegments("/en") ? "en" : "tr";
    public string ThumbnailUrl => _site.AbsoluteImageBase + Video.ThumbnailPath;

    public IActionResult OnGet(string slug, int? t)
    {
        Video? video = videos.Find(slug);
        if (video is null)
            return NotFound();

        Video = video;
        Start = t is > 0 && t < video.DurationSeconds ? t.Value : 0;

        // ?t=95 aynı sayfa: canonical her zaman parametresiz watch page
        string tr = $"{_site.BaseUrl}/videos/{slug}";
        string en = $"{_site.BaseUrl}/en/videos/{slug}";
        string canonical = Lang == "en" ? en : tr;

        ViewData["Meta"] = new PageMeta(Lang, $"{video.Title} | Bookstore", video.Description, canonical, tr, en,
            ThumbnailUrl, JsonSerializer.Serialize(VideoJsonLd.Build(video, canonical, _site.AbsoluteImageBase)),
            Robots: "max-image-preview:large, max-video-preview:-1");
        return Page();
    }
}
