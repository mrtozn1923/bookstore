using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Logging;
using Bookstore.Api.Models;
using Bookstore.Api.Services;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookCatalog _catalog;
    private readonly ILogger<BooksController> _logger;

    public BooksController(IBookCatalog catalog, ILogger<BooksController> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Book>> GetAll()
    {
        return Ok(_catalog.GetAll());
    }

    [HttpGet("{id:int}")]
    public ActionResult<Book> GetById(int id)
    {
        _logger.LogInformation("Getting book {BookId}", id);

        Book? book = _catalog.GetById(id);
        if (book is null)
        {
            LogMessages.BookNotFound(_logger, id);
            return NotFound();
        }

        _logger.LogDebug("Book {BookId} found: {BookTitle}", id, book.Title);
        return book;
    }

    [HttpPost]
    public ActionResult<Book> Create(Book book)
    {
        using (_logger.BeginScope("Creating book {BookTitle}", book.Title))
        {
            _logger.LogInformation("Validating price {Price}", book.Price);
            Book created = _catalog.Add(book);
            _logger.LogInformation("Book saved with id {BookId}", created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
    }
}
