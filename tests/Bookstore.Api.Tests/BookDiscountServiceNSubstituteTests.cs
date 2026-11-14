using NSubstitute;
using Bookstore.Api.Dtos;
using Bookstore.Api.Exceptions;
using Bookstore.Api.Services;
using Xunit;

namespace Bookstore.Api.Tests;

public class BookDiscountServiceNSubstituteTests
{
    [Fact]
    public async Task ApplyAsync_AmountNotBelowPrice_ThrowsAndDoesNotUpdate()
    {
        var books = Substitute.For<IBookService>();
        books.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(new BookDto(2, "Emma", 280m, "Classics"));
        var sut = new BookDiscountService(books);

        await Assert.ThrowsAsync<ConflictException>(
            () => sut.ApplyAsync(2, 280m, CancellationToken.None));

        await books.DidNotReceive().UpdateAsync(Arg.Any<int>(), Arg.Any<UpdateBookRequest>(), Arg.Any<CancellationToken>());
    }
}
