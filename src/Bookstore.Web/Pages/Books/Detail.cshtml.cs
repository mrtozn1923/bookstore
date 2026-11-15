using Bookstore.Web.Catalog;
using Bookstore.Web.StructuredData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bookstore.Web.Pages.Books;

public sealed class DetailModel(BookCatalog catalog, PricingService pricing) : PageModel
{
    public BookEdition Book { get; private set; } = default!;
    public PriceQuote Quote { get; private set; } = default!;
    public string Lang { get; private set; } = "tr";

    public IActionResult OnGet(string slug)
    {
        BookEdition? book = catalog.FindBySlug(slug);
        if (book is null)
        {
            return NotFound();
        }

        Book = book;
        Quote = pricing.Quote(book);   // one quote for the page and the markup
        Lang = Request.Path.StartsWithSegments("/en") ? "en" : "tr";

        ViewData["Lang"] = Lang;
        ViewData["Title"] = $"{book.Title} ({BookTexts.FormatName(book.Format, Lang)})";
        ViewData["JsonLd"] = new SiteGraph("https://bookstore.example").Book(book, Quote, Lang);
        return Page();
    }
}
