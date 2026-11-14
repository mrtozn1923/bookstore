using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Bookstore.Api.Controllers;
using Bookstore.Api.Dtos;
using Bookstore.Api.Exceptions;
using Bookstore.Api.Services;
using Xunit;

namespace Bookstore.Api.Tests;

public class BooksControllerTests
{
    private readonly Mock<IBookService> _service = new();
    private readonly BooksController _controller;

    public BooksControllerTests()
    {
        _controller = new BooksController(_service.Object, NullLogger<BooksController>.Instance);
    }

    [Fact]
    public async Task GetById_BookExists_ReturnsBookAsValue()
    {
        BookDto dto = new(1, "Dune", 450m, "Science Fiction");
        _service
            .Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        // "return await ..." fills Value, not Result
        Assert.Null(result.Result);
        Assert.Equal(dto, result.Value);
    }

    [Fact]
    public async Task GetById_BookMissing_LetsNotFoundExceptionThrough()
    {
        _service
            .Setup(s => s.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Book", 42));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _controller.GetById(42, CancellationToken.None));
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedAtGetById()
    {
        CreateBookRequest request = new() { Title = "Middlemarch", Price = 390m, CategoryId = 2 };
        _service
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookDto(4, "Middlemarch", 390m, "Classics"));

        var result = await _controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(BooksController.GetById), created.ActionName);
        Assert.Equal(4, created.RouteValues!["id"]);
        Assert.Equal(new BookDto(4, "Middlemarch", 390m, "Classics"), created.Value);
    }
}
