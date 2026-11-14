namespace Bookstore.Api;

public interface IBookService
{
    IEnumerable<Book> GetAll();
    Book? GetById(int id);
    Book Add(string title, decimal price);
}
