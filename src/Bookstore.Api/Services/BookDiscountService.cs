using Bookstore.Api.Dtos;
using Bookstore.Api.Exceptions;

namespace Bookstore.Api.Services;

public interface IBookDiscountService
{
    Task<BookDto> ApplyAsync(int bookId, decimal amount, CancellationToken cancellationToken = default);
}

public class BookDiscountService : IBookDiscountService
{
    private readonly IBookService _books;

    public BookDiscountService(IBookService books)
    {
        _books = books;
    }

    public async Task<BookDto> ApplyAsync(int bookId, decimal amount, CancellationToken cancellationToken = default)
    {
        BookDto book = await _books.GetByIdAsync(bookId, cancellationToken);

        // the discounted price must stay above zero
        if (amount >= book.Price)
        {
            throw new ConflictException($"The discount must be less than the price of '{book.Title}'.");
        }

        decimal newPrice = book.Price - amount;
        UpdateBookRequest update = new() { Title = book.Title, Price = newPrice };

        bool updated = await _books.UpdateAsync(bookId, update, cancellationToken);
        if (!updated)
        {
            throw new NotFoundException("Book", bookId);
        }

        return book with { Price = newPrice };
    }
}
