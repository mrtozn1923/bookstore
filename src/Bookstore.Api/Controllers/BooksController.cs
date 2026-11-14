using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Dtos;
using Bookstore.Api.Services;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BookDto>>> GetAll(CancellationToken cancellationToken)
    {
        return await _bookService.GetAllAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookDto>> GetById(int id, CancellationToken cancellationToken)
    {
        BookDto? book = await _bookService.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        return book;
    }

    [HttpPost]
    public async Task<ActionResult<BookDto>> Create(CreateBookRequest request, CancellationToken cancellationToken)
    {
        BookDto? created = await _bookService.CreateAsync(request, cancellationToken);
        if (created is null)
        {
            return Conflict($"A book titled '{request.Title}' already exists.");
        }

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateBookRequest request, CancellationToken cancellationToken)
    {
        bool updated = await _bookService.UpdateAsync(id, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        bool deleted = await _bookService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
