using Bookstore.Web.Models;

namespace Bookstore.Web.Services;

public interface IBookCatalog
{
    Task<IReadOnlyList<BookCardViewModel>> GetFeaturedAsync(CancellationToken cancellationToken = default);
}
