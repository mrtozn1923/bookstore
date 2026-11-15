using Bookstore.Web.Models;

namespace Bookstore.Web.Services;

public sealed class InMemoryBookCatalog : IBookCatalog
{
    private static readonly IReadOnlyList<BookCardViewModel> Books =
    [
        new("9780441172719", "Dune", "Frank Herbert", 349.00m, "/covers/dune.svg", true),
        new("9780547928227", "The Hobbit", "J. R. R. Tolkien", 289.00m, "/covers/hobbit.svg", true),
        new("9780141439587", "Emma", "Jane Austen", 189.00m, "/covers/emma.svg", false),
    ];

    public Task<IReadOnlyList<BookCardViewModel>> GetFeaturedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Books);
}
