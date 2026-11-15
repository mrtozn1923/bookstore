using Bookstore.Web.Ordering;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers.Api;

public sealed record AddCartItemRequest(int BookId, int Quantity);

[ApiController]
[Route("api/cart")]
public sealed class CartApiController(CartService cart) : ControllerBase
{
    [HttpPost("items")]
    public async Task<IActionResult> Add(AddCartItemRequest request, CancellationToken cancellationToken) =>
        await cart.AddAsync(request.BookId, request.Quantity, cancellationToken) is { } book
            ? Ok(new { book.Id, book.Title, request.Quantity })
            : NotFound();
}
