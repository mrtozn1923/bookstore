using Microsoft.AspNetCore.OutputCaching;

namespace Bookstore.Catalog.Books;

public static class CachePolicies
{
    public const string BookLists = "book-lists";
    public const string Book = "book";
    public const string Categories = "categories";
    public const string BookListsTag = "book-lists";

    public static string BookTag(string isbn) => $"book:{isbn}";

    public static void Configure(OutputCacheOptions options)
    {
        options.AddPolicy(BookLists, policy => policy
            .SetVaryByQuery("search", "category", "page", "pageSize")
            .Expire(TimeSpan.FromSeconds(60))
            .Tag(BookListsTag));
        options.AddPolicy(Book, policy => policy
            .AddPolicy<BookTagPolicy>()
            .Expire(TimeSpan.FromMinutes(10)));
        options.AddPolicy(Categories, policy => policy.Expire(TimeSpan.FromMinutes(30)));
    }

    // Adds "book:{isbn}" from the route so one book can be evicted on its own.
    private sealed class BookTagPolicy : IOutputCachePolicy
    {
        public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
        {
            if (context.HttpContext.Request.RouteValues["isbn"] is string isbn)
            {
                context.Tags.Add(BookTag(isbn));
            }
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
    }
}
