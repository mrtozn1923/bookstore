using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Models;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    // A temporary in-memory list until we have a database
    private static readonly List<Book> Books = new()
    {
        new Book { Id = 1, Title = "Dune", Price = 450m, CategoryId = 1 },
        new Book { Id = 2, Title = "Emma", Price = 280m, CategoryId = 2 },
        new Book { Id = 3, Title = "The Hobbit", Price = 320m, CategoryId = 1 }
    };

    [HttpGet]
    public ActionResult<IEnumerable<Book>> GetAll()
    {
        return Books;
    }

    [HttpGet("{id:int}")]
    public ActionResult<Book> GetById(int id)
    {
        Book? book = Books.FirstOrDefault(b => b.Id == id);
        if (book is null)
        {
            return NotFound();
        }

        return book;
    }

    [HttpPost]
    public ActionResult<Book> Create(Book book)
    {
        book.Id = Books.Count == 0 ? 1 : Books.Max(b => b.Id) + 1;
        Books.Add(book);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, Book book)
    {
        Book? existing = Books.FirstOrDefault(b => b.Id == id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Title = book.Title;
        existing.Price = book.Price;
        existing.CategoryId = book.CategoryId;
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        Book? existing = Books.FirstOrDefault(b => b.Id == id);
        if (existing is null)
        {
            return NotFound();
        }

        Books.Remove(existing);
        return NoContent();
    }
}
