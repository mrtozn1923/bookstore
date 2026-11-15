using Bookstore.Web.Models;

namespace Bookstore.Web.Services;

public sealed class InMemoryBookCatalog : IBookCatalog
{
    // colors come from the back office in the real app; ColorName is the old palette name, Color the hex value
    private static readonly CategoryViewModel SciFi = new("Science Fiction", "sky", "#0284c7");
    private static readonly CategoryViewModel Fantasy = new("Fantasy", "emerald", "#059669");
    private static readonly CategoryViewModel Classics = new("Classics", "rose", "#e11d48");

    private static readonly IReadOnlyList<BookCardViewModel> Books =
    [
        new("9780441172719", "Dune", "Frank Herbert", 349.00m, "/covers/dune.svg", true, SciFi),
        new("9780547928227", "The Hobbit", "J. R. R. Tolkien", 289.00m, "/covers/hobbit.svg", true, Fantasy),
        new("9780141439587", "Emma", "Jane Austen", 189.00m, "/covers/emma.svg", false, Classics),
    ];

    public Task<IReadOnlyList<BookCardViewModel>> GetFeaturedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Books);
}
