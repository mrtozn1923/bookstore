using Bookstore.Web.Catalog;
using Bookstore.Web.Ordering;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers;

[Route("cart")]
public sealed class CartController(CartService cart) : Controller
{
    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int bookId, int quantity, CancellationToken cancellationToken)
    {
        Book? book = await cart.AddAsync(bookId, Math.Max(quantity, 1), cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        TempData["Flash"] = $"\"{book.Title}\" was added to your cart.";
        return RedirectToAction("Details", "Books", new { id = book.Id, slug = book.Slug });
    }
}
