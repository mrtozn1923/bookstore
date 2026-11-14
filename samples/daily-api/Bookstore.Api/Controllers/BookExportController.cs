using Bookstore.Api.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/books/export")]
public class BookExportController(BookCatalog catalog) : ControllerBase
{
    [HttpGet]
    [HttpGet("~/api/books/export.{format}")]
    [FormatFilter]
    [Produces("application/json", "text/csv")]
    public IEnumerable<Book> Get() => catalog.All.OrderBy(b => b.Title);
}
