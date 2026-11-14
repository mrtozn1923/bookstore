using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Dtos;
using Bookstore.Api.Filters;
using Bookstore.Api.Services;

namespace Bookstore.Api.Controllers;

[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
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
    [AllowAnonymous]
    [ServiceFilter(typeof(ExecutionTimeFilter))]
    public async Task<ActionResult<PagedResult<BookDto>>> GetAll(
        [FromQuery] BookQueryParameters parameters, CancellationToken cancellationToken)
    {
        return await _bookService.GetPagedAsync(parameters, cancellationToken);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<BookDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return await _bookService.GetByIdAsync(id, cancellationToken);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageBooks")]
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
    [Authorize(Roles = "Admin")]
    [TypeFilter(typeof(BookExistsFilter))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Delete action is running for {Id}", id);
        await _bookService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/discount")]
    [Authorize(Policy = "ExperiencedStaff")]
    public async Task<ActionResult<BookDto>> Discount(int id, DiscountRequest request, CancellationToken cancellationToken)
    {
        BookDto book = await _bookService.GetByIdAsync(id, cancellationToken);
        UpdateBookRequest update = new() { Title = book.Title, Price = book.Price - request.Amount };
        await _bookService.UpdateAsync(id, update, cancellationToken);
        return await _bookService.GetByIdAsync(id, cancellationToken);
    }
}
