using Bookstore.Web.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers.Api;

[ApiController]
[Route("api/books")]
public sealed class BooksApiController(IBookCatalog catalog) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Book>> Get(int id, CancellationToken cancellationToken) =>
        await catalog.FindAsync(id, cancellationToken) is { } book ? book : NotFound();
}
