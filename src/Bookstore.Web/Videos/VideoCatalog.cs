namespace Bookstore.Web.Videos;

public sealed class VideoCatalog
{
    public IReadOnlyList<Video> Videos { get; } =
    [
        new("dune-reading-guide", "Dune Reading Guide: Where to Start and What to Watch For",
            "A spoiler-free guide to Frank Herbert's Dune: the setting, the main houses and which edition to pick.",
            new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.FromHours(3)), 754,
            ContentUrl: "https://media.bookstore.example/videos/dune-reading-guide.mp4", YouTubeId: null, BookSlug: "dune",
            Chapters: [new(0, "Introduction"), new(95, "Arrakis and the spice"), new(310, "The great houses"), new(600, "Which edition to buy")]),
        new("foundation-in-twelve-minutes", "Asimov's Foundation in Twelve Minutes",
            "An overview of Isaac Asimov's Foundation trilogy and the idea of psychohistory, recorded at the Bookstore reading club.",
            new DateTimeOffset(2026, 9, 3, 18, 30, 0, TimeSpan.FromHours(3)), 731,
            ContentUrl: null, YouTubeId: "bkFoundat12", BookSlug: "foundation"),
        new("back-to-school-2026", "Back to School 2026: Institutional Orders",
            "How schools and libraries order 20 or more books with the institutional discount.",
            new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.FromHours(3)), 95,
            ContentUrl: "https://media.bookstore.example/videos/back-to-school-2026.mp4", YouTubeId: null,
            Expires: new DateOnly(2026, 12, 31), AllowedRegions: ["TR"]),
    ];

    public Video? Find(string slug) => Videos.FirstOrDefault(v => v.Slug == slug);
}
