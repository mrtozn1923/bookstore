using Bookstore.Web.Models;
using Bookstore.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers;

public sealed class CatalogController(IBookCatalog catalog) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<BookCardViewModel> books = await catalog.GetFeaturedAsync(cancellationToken);
        ViewData["Title"] = "Catalog";
        return View(books);
    }
}
