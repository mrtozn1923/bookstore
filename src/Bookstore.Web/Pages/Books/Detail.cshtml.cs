using System.Text.Json;
using Bookstore.Web.Catalog;
using Bookstore.Web.Images;
using Bookstore.Web.Seo;
using Bookstore.Web.Videos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Bookstore.Web.Pages.Books;

public sealed class DetailModel(BookCatalog catalog, VideoCatalog videos, IOptions<SiteOptions> options) : PageModel
{
    private readonly SiteOptions _site = options.Value;

    public Book Book { get; private set; } = null!;
    public IReadOnlyList<Book> Related { get; private set; } = [];
    public Video? Video { get; private set; }
    public string VideoPath => $"{(Lang == "en" ? "/en" : "")}/videos/{Video?.Slug}";
    public string Lang => Request.Path.StartsWithSegments("/en") ? "en" : "tr";
    public string ImageBase => _site.ImageBaseUrl;

    public string BookPath(Book book) => Lang == "en" ? $"/en/books/{book.Slug}" : $"/books/{book.Slug}";

    public IActionResult OnGet(string slug)
    {
        Book? book = catalog.Find(slug);
        if (book is null)
            return NotFound();

        Book = book;
        Video = videos.Videos.FirstOrDefault(v => v.BookSlug == book.Slug);
        Related = [.. catalog.Books.Where(b => b.Genre == book.Genre && b.Slug != book.Slug).Take(4)];

        string canonical = _site.BaseUrl + BookPath(book);
        // tercih edilen görsel: og:image ve primaryImageOfPage aynı, büyük kapak
        string preferredImage = CoverImages.Url(book, 1200, "jpg", _site.AbsoluteImageBase);

        Dictionary<string, object> jsonLd = new()
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "ItemPage",
            ["@id"] = canonical,
            ["url"] = canonical,
            ["primaryImageOfPage"] = preferredImage,
            ["mainEntity"] = new Dictionary<string, object>
            {
                ["@type"] = "Book",
                ["name"] = book.Title,
                ["author"] = new Dictionary<string, object> { ["@type"] = "Person", ["name"] = book.Author },
                ["isbn"] = book.Isbn,
                ["image"] = preferredImage,
            },
        };

        ViewData["Meta"] = new PageMeta(
            Lang,
            $"{book.Title} | {book.Author} | Bookstore",
            Lang == "tr" ? $"{book.Title}: {book.Author}, {book.Year}. Baskılar ve fiyatlar." : $"{book.Title} by {book.Author}, {book.Year}. Editions and prices.",
            canonical,
            $"{_site.BaseUrl}/books/{book.Slug}",
            $"{_site.BaseUrl}/en/books/{book.Slug}",
            preferredImage,
            JsonSerializer.Serialize(jsonLd));
        return Page();
    }
}
