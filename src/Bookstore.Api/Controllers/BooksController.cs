using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Dtos;
using Bookstore.Api.Filters;
using Bookstore.Api.Services;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly ILogger<BooksController> _logger;

    public BooksController(IBookService bookService, ILogger<BooksController> logger)
    {
        _bookService = bookService;
        _logger = logger;
    }

    [HttpGet]
    [ServiceFilter(typeof(ExecutionTimeFilter))]
    public async Task<ActionResult<PagedResult<BookDto>>> GetAll(
        [FromQuery] BookQueryParameters parameters, CancellationToken cancellationToken)
    {
        return await _bookService.GetPagedAsync(parameters, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return await _bookService.GetByIdAsync(id, cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<BookDto>> Create(CreateBookRequest request, CancellationToken cancellationToken)
    {
        BookDto created = await _bookService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateBookRequest request, CancellationToken cancellationToken)
    {
        bool updated = await _bookService.UpdateAsync(id, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [TypeFilter(typeof(BookExistsFilter))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Delete action is running for {Id}", id);
        await _bookService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
