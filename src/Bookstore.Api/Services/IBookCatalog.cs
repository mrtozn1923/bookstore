using Bookstore.Api.Models;

namespace Bookstore.Api.Services;

public interface IBookCatalog
{
    IReadOnlyList<Book> GetAll();
    Book? GetById(int id);
    Book Add(Book book);
}
