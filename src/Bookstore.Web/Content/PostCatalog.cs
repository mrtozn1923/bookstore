namespace Bookstore.Web.Content;

public sealed record Localized(string Tr, string En)
{
    public string Get(string lang) => lang == "en" ? En : Tr;
}

public sealed record Author(string Slug, string Name, Localized JobTitle, Localized Bio, IReadOnlyList<string> SameAs);

public sealed record Post(
    string Slug,
    Localized Title,
    Localized Summary,
    Localized Body,
    Localized Category,
    string CategorySlug,
    string AuthorSlug,
    DateTimeOffset PublishedAt,
    DateTimeOffset? UpdatedAt,
    string ImagePath);

public sealed class PostCatalog
{
    public IReadOnlyList<Author> Authors { get; init; } =
    [
        new("ada-kaplan", "Ada Kaplan",
            new("Kıdemli Yazılım Geliştirici", "Senior Software Developer"),
            new("Bookstore'un backend ekibinde .NET ve arama altyapısı üzerinde çalışıyor.",
                "Works on .NET and search infrastructure in the Bookstore backend team."),
            ["https://github.com/ada-kaplan-example", "https://www.linkedin.com/in/ada-kaplan-example"]),
    ];

    public IReadOnlyList<Post> Posts { get; init; } =
    [
        new("csharp-books-for-beginners",
            new("Yeni Başlayanlar İçin 5 C# Kitabı", "5 C# Books for Beginners"),
            new("İlk C# kitabını seçerken dile değil, alıştırmalara bak: bu listedeki beş kitap da her bölümü çalışan bir projeyle bitiriyor.",
                "When choosing your first C# book, look at the exercises, not the language: all five books on this list end every chapter with a working project."),
            new("Kitapları üç ölçüte göre seçtik: güncel .NET sürümü, alıştırma kalitesi ve okur yorumları.",
                "We picked the books by three criteria: current .NET version, exercise quality and reader reviews."),
            new("Kitap Önerileri", "Book Picks"), "book-picks", "ada-kaplan",
            new DateTimeOffset(2026, 11, 10, 9, 30, 0, TimeSpan.FromHours(3)),
            new DateTimeOffset(2026, 11, 12, 14, 5, 0, TimeSpan.FromHours(3)),
            "/images/csharp-books-16x9.png"),
    ];

    public Post? FindPost(string slug) => Posts.FirstOrDefault(p => p.Slug == slug);

    public Author? FindAuthor(string slug) => Authors.FirstOrDefault(a => a.Slug == slug);
}
