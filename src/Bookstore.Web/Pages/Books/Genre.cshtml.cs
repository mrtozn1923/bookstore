using Bookstore.Web.Catalog;
using Bookstore.Web.Seo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Bookstore.Web.Pages.Books;

public sealed class GenreModel(BookCatalog catalog, IOptions<SiteOptions> options) : PageModel
{
    private readonly SiteOptions _site = options.Value;

    public CatalogQuery Query { get; private set; } = null!;
    public IReadOnlyList<Book> Items { get; private set; } = [];
    public int TotalPages { get; private set; }
    public string Heading { get; private set; } = "";
    public IReadOnlyList<string> NarrowingLanguages { get; private set; } = [];
    public string Lang => Request.Path.StartsWithSegments("/en") ? "en" : "tr";
    public string Prefix => Lang == "en" ? "/en" : "";

    public IActionResult OnGet(string genre, string? language)
    {
        (CatalogQuery? query, string? normalized) = CatalogQuery.Parse(genre, language, Request.Query);
        if (query is null)
            return NotFound();

        Query = query;
        List<Book> all = [.. Query.Apply(catalog.Books)];
        TotalPages = (int)Math.Ceiling(all.Count / (double)CatalogQuery.PageSize);
        // sonuçsuz kombinasyon ve var olmayan sayfa: o URL'de 404, "bulunamadı" sayfasına redirect değil
        if (all.Count == 0 || Query.Page > TotalPages)
            return NotFound();
        // sonuç kontrolünden sonra: 301'in hedefi hiçbir zaman 404 olmasın
        if (normalized is not null)
            return RedirectPermanent(Prefix + normalized);

        // sitemap'teki kuralla aynı: yalnız listeyi daraltan dil sayfaları
        int genreCount = catalog.Books.Count(b => b.Genre == genre);
        NarrowingLanguages = [.. new[] { "tr", "en" }.Where(l =>
            catalog.Books.Count(b => b.Genre == genre && b.Language == l) is int n && n > 0 && n < genreCount)];
        Items = [.. all.Skip((Query.Page - 1) * CatalogQuery.PageSize).Take(CatalogQuery.PageSize)];
        Genre g = BookCatalog.Genres.Single(x => x.Slug == genre);
        string name = Lang == "tr" ? g.NameTr : g.NameEn;
        Heading = language switch
        {
            "tr" => Lang == "tr" ? $"Türkçe {name} kitapları" : $"{name} books in Turkish",
            "en" => Lang == "tr" ? $"İngilizce {name} kitapları" : $"{name} books in English",
            _ => Lang == "tr" ? $"{name} kitapları" : $"{name} books",
        };

        // indexable sayfanın canonical'ı kendisi (?page=n dahil), ilk sayfaya canonical'lanmaz;
        // filtreli sayfa (robots.txt ile zaten crawl dışı) filtresiz türe işaret eder
        // dil filtresi listeyi daraltmıyorsa (ör. yalnız İngilizce kitabı olan tür) sayfa üst türün kopyasıdır
        bool narrows = Query.Language is null || all.Count < catalog.Books.Count(b => b.Genre == genre);
        string self = Query.IsIndexable && narrows ? Query.ToUrl()
            : (Query with { Language = narrows ? Query.Language : null, Format = null, Sort = "title", Page = 1 }).ToUrl();
        ViewData["Meta"] = new PageMeta(Lang, $"{Heading} | Bookstore", Heading, _site.BaseUrl + Prefix + self,
            _site.BaseUrl + self, _site.BaseUrl + "/en" + self);
        return Page();
    }
}
