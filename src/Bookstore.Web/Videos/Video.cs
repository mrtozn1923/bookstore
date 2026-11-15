namespace Bookstore.Web.Videos;

public sealed record Chapter(int StartSeconds, string Label);

public sealed record Video(
    string Slug,
    string Title,
    string Description,
    DateTimeOffset UploadDate,
    int DurationSeconds,
    string? ContentUrl,    // kendi sunucumuzdaki dosya (mp4)
    string? YouTubeId,     // ya da üçüncü parti player
    string? BookSlug = null,
    Chapter[]? Chapters = null,
    DateOnly? Expires = null,
    string[]? AllowedRegions = null)
{
    // her video için tek ve sabit thumbnail URL'i: deploy'dan deploy'a değişmez
    public string ThumbnailPath => $"/images/videos/{Slug}-1280x720.jpg";

    public string? PlayerUrl => YouTubeId is null ? null : $"https://www.youtube-nocookie.com/embed/{YouTubeId}";
}
