using Microsoft.Extensions.Primitives;

namespace Bookstore.Web.Catalog;

// Index'lenen sayfalar: /books/genre/{genre}, /books/genre/{genre}/{language} ve bunların ?page=n'i.
// format ve sort filtreleri kullanıcı içindir; robots.txt ile crawl dışında tutulur.
public sealed record CatalogQuery(string Genre, string? Language, BookFormat? Format, string Sort, int Page)
{
    public const int PageSize = 12;
    private static readonly string[] Languages = ["tr", "en"];
    private static readonly string[] Sorts = ["title", "price", "year"];

    // null: geçersiz istek (404). Normalized: URL'nin tek doğru yazımı; istekle farklıysa 301
    public static (CatalogQuery? Query, string? Normalized) Parse(string genre, string? language, IQueryCollection query)
    {
        if (!BookCatalog.Genres.Any(g => g.Slug == genre) || (language is not null && !Languages.Contains(language)))
            return (null, null);

        // aynı filtre iki kez (?format=ebook&format=ebook) ya da anlamsız değer: 404, redirect değil
        if (query.Any(p => p.Value.Count > 1))
            return (null, null);

        BookFormat? format = null;
        if (query.TryGetValue("format", out StringValues f))
        {
            if (!Enum.TryParse(f.ToString(), ignoreCase: true, out BookFormat parsed) || int.TryParse(f.ToString(), out _))
                return (null, null);
            format = parsed;
        }

        string sort = query.TryGetValue("sort", out StringValues s) ? s.ToString() : "title";
        if (!Sorts.Contains(sort))
            return (null, null);

        int page = 1;
        if (query.TryGetValue("page", out StringValues p))
        {
            if (!int.TryParse(p.ToString(), out page) || page < 1)
                return (null, null);
        }

        CatalogQuery result = new(genre, language, format, sort, page);
        // varsayılan değerler (?page=1, ?sort=title) atılır, parametre sırası sabitlenir
        string normalized = result.ToUrl();
        return (result, BuildRequested(genre, language, query) == normalized ? null : normalized);
    }

    public CatalogQuery WithPage(int page) => this with { Page = page };

    // parametreler her zaman aynı sırada ve & ile: format, sort, page
    public string ToUrl(string prefix = "")
    {
        List<string> parts = [];
        if (Format is BookFormat format) parts.Add($"format={format.ToString().ToLowerInvariant()}");
        if (Sort != "title") parts.Add($"sort={Sort}");
        if (Page > 1) parts.Add($"page={Page}");
        string path = Language is null ? $"{prefix}/books/genre/{Genre}" : $"{prefix}/books/genre/{Genre}/{Language}";
        return parts.Count == 0 ? path : $"{path}?{string.Join('&', parts)}";
    }

    // indexable mı: yalnız tür (+dil) ve sayfa; diğer filtreler crawl edilmez
    public bool IsIndexable => Format is null && Sort == "title";

    public IEnumerable<Book> Apply(IEnumerable<Book> books)
    {
        IEnumerable<Book> filtered = books.Where(b => b.Genre == Genre
            && (Language is null || b.Language == Language)
            && (Format is null || b.Format == Format));
        return Sort switch
        {
            "price" => filtered.OrderBy(b => b.Price),
            "year" => filtered.OrderBy(b => b.Year),
            _ => filtered.OrderBy(b => b.Title, StringComparer.Ordinal),
        };
    }

    private static string BuildRequested(string genre, string? language, IQueryCollection query)
    {
        // izlenen parametreler dışındakiler (utm_*, ref) normalizasyona girmez; canonical onları zaten atar
        string[] known = ["format", "sort", "page"];
        IEnumerable<string> parts = query.Where(q => known.Contains(q.Key)).Select(q => $"{q.Key}={q.Value}");
        string path = language is null ? $"/books/genre/{genre}" : $"/books/genre/{genre}/{language}";
        string qs = string.Join('&', parts);
        return qs.Length == 0 ? path : $"{path}?{qs}";
    }
}
