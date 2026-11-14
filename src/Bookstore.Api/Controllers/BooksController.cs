using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Models;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private static readonly List<Book> Books = new()
    {
        new Book { Id = 1, Title = "Dune", Price = 450m, CategoryId = 1 },
        new Book { Id = 2, Title = "Emma", Price = 280m, CategoryId = 2 },
        new Book { Id = 3, Title = "The Hobbit", Price = 320m, CategoryId = 1 }
    };

    [HttpGet]
    public IEnumerable<Book> GetAll() => Books;

    [HttpGet("{id:int:min(1)}")]
    public ActionResult<Book> GetById(int id)
    {
        var book = Books.FirstOrDefault(b => b.Id == id);
        return book is null ? NotFound() : book;
    }

    [HttpGet("{title}")]
    public ActionResult<Book> GetByTitle(string title)
    {
        var book = Books.FirstOrDefault(b => b.Title.Replace(" ", "-").Equals(title, StringComparison.OrdinalIgnoreCase));
        return book is null ? NotFound() : book;
    }

    [HttpGet("/api/categories/{categoryId:int}/books")]
    public IEnumerable<Book> GetByCategory(int categoryId) =>
        Books.Where(b => b.CategoryId == categoryId);

    [HttpGet("[action]")]
    public int Count() => Books.Count;
}
