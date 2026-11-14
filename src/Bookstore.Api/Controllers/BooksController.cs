using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookstore.Api.Data;
using Bookstore.Api.Dtos;
using Bookstore.Api.Mapping;
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
    public async Task<ActionResult<IEnumerable<BookDto>>> GetAll()
    {
        List<BookDto> books = await _db.Books
            .Select(b => new BookDto(b.Id, b.Title, b.Price, b.Category!.Name))
            .ToListAsync();
        return books;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookDto>> GetById(int id)
    {
        BookDto? book = await _db.Books
            .Where(b => b.Id == id)
            .Select(b => new BookDto(b.Id, b.Title, b.Price, b.Category!.Name))
            .FirstOrDefaultAsync();
        if (book is null)
        {
            return NotFound();
        }

        return book;
    }

    [HttpPost]
    public async Task<ActionResult<BookDto>> Create(CreateBookRequest request)
    {
        Category? category = await _db.Categories.FindAsync(request.CategoryId);
        if (category is null)
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Category does not exist.");
            return ValidationProblem(ModelState);
        }

        Book book = request.ToEntity();
        book.Category = category;
        _db.Books.Add(book);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateBookRequest request)
    {
        Book? book = await _db.Books.FindAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        request.ApplyTo(book);
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
