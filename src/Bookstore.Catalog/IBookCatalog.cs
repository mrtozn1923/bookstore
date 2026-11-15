namespace Bookstore.Catalog;

public interface IBookCatalog
{
    IReadOnlyList<Book> GetAll();

    Book? FindByIsbn(string isbn);
}
