namespace Bookstore.Web.Catalog;

public interface IBookCatalog
{
    Task<IReadOnlyList<Book>> ListAsync(string? category, CancellationToken cancellationToken);
    Task<Book?> FindAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Book>> SuggestAsync(string? hint, int count, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> CategoriesAsync(CancellationToken cancellationToken);
}
