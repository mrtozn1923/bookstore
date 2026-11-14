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
    public ActionResult<IEnumerable<Book>> Search(int? categoryId, decimal? minPrice, int page = 1)
    {
        IEnumerable<Book> result = Books;
        if (categoryId is not null)
        {
            result = result.Where(b => b.CategoryId == categoryId);
        }

        if (minPrice is not null)
        {
            result = result.Where(b => b.Price >= minPrice);
        }

        return result.Skip((page - 1) * 10).Take(10).ToList();
    }

    [HttpGet("{id:int}")]
    public ActionResult<Book> GetById([FromRoute] int id)
    {
        Book? book = Books.FirstOrDefault(b => b.Id == id);
        if (book is null)
        {
            return NotFound();
        }

        return book;
    }

    [HttpGet("by-ids")]
    public ActionResult<IEnumerable<Book>> GetByIds([FromQuery] int[] ids)
    {
        return Books.Where(b => ids.Contains(b.Id)).ToList();
    }

    [HttpGet("sorted")]
    public ActionResult<IEnumerable<string>> GetSorted([FromQuery(Name = "sort_by")] string? sortBy)
    {
        IEnumerable<Book> result = sortBy == "price"
            ? Books.OrderBy(b => b.Price)
            : Books.OrderBy(b => b.Title);
        return result.Select(b => b.Title).ToList();
    }

    [HttpGet("filter")]
    public ActionResult<IEnumerable<Book>> Filter([FromQuery] BookQuery query)
    {
        IEnumerable<Book> result = Books;
        if (query.CategoryId is not null)
        {
            result = result.Where(b => b.CategoryId == query.CategoryId);
        }

        if (query.MinPrice is not null)
        {
            result = result.Where(b => b.Price >= query.MinPrice);
        }

        if (query.MaxPrice is not null)
        {
            result = result.Where(b => b.Price <= query.MaxPrice);
        }

        return result.Skip((query.Page - 1) * 10).Take(10).ToList();
    }

    [HttpPost]
    public ActionResult<Book> Create(
        [FromBody] CreateBookRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId)
    {
        Book book = new()
        {
            Id = Books.Max(b => b.Id) + 1,
            Title = request.Title,
            Price = request.Price,
            CategoryId = request.CategoryId
        };
        Books.Add(book);

        if (correlationId is not null)
        {
            Response.Headers["X-Correlation-Id"] = correlationId;
        }

        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    [HttpPost("form")]
    public ActionResult<Book> CreateFromForm([FromForm] CreateBookRequest request)
    {
        Book book = new()
        {
            Id = Books.Max(b => b.Id) + 1,
            Title = request.Title,
            Price = request.Price,
            CategoryId = request.CategoryId
        };
        Books.Add(book);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    [HttpGet("environment")]
    public ActionResult<string> GetEnvironment([FromServices] IWebHostEnvironment environment)
    {
        return environment.EnvironmentName;
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
