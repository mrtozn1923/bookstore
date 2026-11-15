using Bookstore.Web.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers;

[Route("books")]
public sealed class BooksController(IBookCatalog catalog) : Controller
{
    private const string SortCookie = "bookstore_sort";

    // Filters live in the query string (shareable links); the last sort choice is remembered in a preference cookie
    [HttpGet("")]
    public async Task<IActionResult> Index(string? category, string? sort, CancellationToken cancellationToken)
    {
        if (sort is "title" or "price")
        {
            // Not essential: with the cookie policy this is only written after consent
            Response.Cookies.Append(SortCookie, sort, new CookieOptions { MaxAge = TimeSpan.FromDays(180), IsEssential = false });
        }
        else
        {
            sort = Request.Cookies[SortCookie] is "price" ? "price" : "title";
        }

        IReadOnlyList<Book> books = await catalog.ListAsync(category, cancellationToken);
        ViewData["Sort"] = sort;
        return View(sort == "price" ? books.OrderBy(b => b.Price).ToList() : books.OrderBy(b => b.Title).ToList());
    }

    [HttpGet("{id:int}/{slug?}")]
    public async Task<IActionResult> Details(int id, string? slug, CancellationToken cancellationToken)
    {
        Book? book = await catalog.FindAsync(id, cancellationToken);
        if (book is not null)
        {
            return View(book);
        }

        // The book was removed or the link is wrong: a real 404 with suggestions taken from the slug
        IReadOnlyList<Book> suggestions = await catalog.SuggestAsync(slug, 3, cancellationToken);
        ViewResult notFound = View("BookNotFound", suggestions);
        notFound.StatusCode = StatusCodes.Status404NotFound;
        return notFound;
    }
}
