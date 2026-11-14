using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    [HttpPost]
    public ActionResult<BookDto> Create(CreateBookRequest request) =>
        CreatedAtAction(nameof(Create), new BookDto(42, request.Title, request.Isbn, request.Price));
}
