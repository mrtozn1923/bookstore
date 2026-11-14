using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Models;
using Bookstore.Api.Services;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookCatalog _catalog;

    public BooksController(IBookCatalog catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Book>> GetAll()
    {
        return Ok(_catalog.GetAll());
    }

    [HttpGet("{id:int}")]
    public ActionResult<Book> GetById(int id)
    {
        Book? book = _catalog.GetById(id);
        if (book is null)
        {
            return NotFound();
        }

        return book;
    }

    [HttpPost]
    public ActionResult<Book> Create(Book book)
    {
        Book created = _catalog.Add(book);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
