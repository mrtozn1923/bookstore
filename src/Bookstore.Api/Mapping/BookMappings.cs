using Bookstore.Api.Dtos;
using Bookstore.Api.Models;

namespace Bookstore.Api.Mapping;

public static class BookMappings
{
    public static BookDto ToDto(this Book book)
    {
        return new BookDto(book.Id, book.Title, book.Price, book.Category?.Name ?? string.Empty);
    }

    public static Book ToEntity(this CreateBookRequest request)
    {
        return new Book
        {
            Title = request.Title,
            Price = request.Price,
            CategoryId = request.CategoryId
        };
    }

    public static void ApplyTo(this UpdateBookRequest request, Book book)
    {
        book.Title = request.Title;
        book.Price = request.Price;
    }
}
