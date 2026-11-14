using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Dtos;
using Bookstore.Api.Services;

namespace Bookstore.Api.Controllers.V2;

public record Money(decimal Amount, string Currency);

public record BookDto(int Id, string Title, Money Price, string CategoryName);

[ApiController]
[ApiVersion("2.0")]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<PagedResult<BookDto>> GetAll(
        [FromQuery] BookQueryParameters parameters, CancellationToken cancellationToken)
    {
        PagedResult<Dtos.BookDto> page = await _bookService.GetPagedAsync(parameters, cancellationToken);

        // same data, only the shape of the price changes
        List<BookDto> items = page.Items
            .Select(b => new BookDto(b.Id, b.Title, new Money(b.Price, "TRY"), b.CategoryName))
            .ToList();

        return new PagedResult<BookDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
