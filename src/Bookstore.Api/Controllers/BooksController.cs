using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookstore.Api.Data;
using Bookstore.Api.Models;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly BookstoreContext _db;

    public BooksController(BookstoreContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Book>>> GetAll()
    {
        // We only read, so we spare the change tracker the work
        List<Book> books = await _db.Books.AsNoTracking().ToListAsync();
        return books;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Book>> GetById(int id)
    {
        Book? book = await _db.Books.FindAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        return book;
    }

    [HttpPost]
    public async Task<ActionResult<Book>> Create(Book book)
    {
        _db.Books.Add(book);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Book input)
    {
        Book? book = await _db.Books.FindAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        book.Title = input.Title;
        book.Price = input.Price;
        book.CategoryId = input.CategoryId;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        Book? book = await _db.Books.FindAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
