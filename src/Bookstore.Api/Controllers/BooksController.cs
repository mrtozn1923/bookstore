using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Dtos;
using Bookstore.Api.Filters;
using Bookstore.Api.Services;

namespace Bookstore.Api.Controllers;

/// <summary>
/// Manages the books in the catalog.
/// </summary>
[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
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

    /// <summary>
    /// Returns a single book by its id.
    /// </summary>
    /// <param name="id">The book id.</param>
    /// <param name="cancellationToken">Cancelled when the client gives up.</param>
    /// <response code="200">The book was found.</response>
    /// <response code="404">There is no book with this id.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return await _bookService.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Adds a new book to the catalog.
    /// </summary>
    /// <response code="201">The book was created.</response>
    /// <response code="400">The request body is invalid.</response>
    /// <response code="409">A book with the same title already exists.</response>
    [HttpPost]
    [Authorize(Policy = "CanManageBooks")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
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
