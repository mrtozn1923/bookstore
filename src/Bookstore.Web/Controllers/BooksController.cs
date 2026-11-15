using Bookstore.Web.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers;

[Route("books")]
public sealed class BooksController(IBookCatalog catalog) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? category, CancellationToken cancellationToken) =>
        View(await catalog.ListAsync(category, cancellationToken));

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
