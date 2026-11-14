using Moq;
using Bookstore.Api.Dtos;
using Bookstore.Api.Exceptions;
using Bookstore.Api.Services;
using Xunit;

namespace Bookstore.Api.Tests;

public class BookDiscountServiceTests
{
    private readonly Mock<IBookService> _books = new();
    private readonly BookDiscountService _sut;

    public BookDiscountServiceTests()
    {
        _sut = new BookDiscountService(_books.Object);
    }

    [Fact]
    public async Task ApplyAsync_ValidAmount_ReturnsDiscountedBook()
    {
        // Arrange: set the stage
        _books
            .Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookDto(1, "Dune", 450m, "Science Fiction"));
        _books
            .Setup(b => b.UpdateAsync(1, It.IsAny<UpdateBookRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act: the single call under test
        BookDto result = await _sut.ApplyAsync(1, 50m, CancellationToken.None);

        // Assert: the expected result
        Assert.Equal("Dune", result.Title);
        Assert.Equal(400m, result.Price);
    }

    [Fact]
    public async Task ApplyAsync_BookMissing_ThrowsNotFound()
    {
        _books
            .Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Book", 42));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.ApplyAsync(42, 50m, CancellationToken.None));
    }

    [Fact]
    public async Task ApplyAsync_AmountNotBelowPrice_ThrowsAndDoesNotUpdate()
    {
        _books
            .Setup(b => b.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookDto(2, "Emma", 280m, "Classics"));

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.ApplyAsync(2, 280m, CancellationToken.None));

        _books.Verify(
            b => b.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateBookRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_ValidAmount_UpdatesTitleAndNewPrice()
    {
        _books
            .Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookDto(1, "Dune", 450m, "Science Fiction"));
        _books
            .Setup(b => b.UpdateAsync(1, It.IsAny<UpdateBookRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.ApplyAsync(1, 50m, CancellationToken.None);

        _books.Verify(
            b => b.UpdateAsync(
                1,
                It.Is<UpdateBookRequest>(r => r.Title == "Dune" && r.Price == 400m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
