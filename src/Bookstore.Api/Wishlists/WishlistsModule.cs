using Bookstore.Catalog;
using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Wishlists;

public static class WishlistsModule
{
    public static IEndpointRouteBuilder MapWishlistEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder wishlists = app.MapGroup("/api/wishlists").WithTags("Wishlists");
        wishlists.MapPost("/", CreateWishlist);
        wishlists.MapGet("/{wishlistId:guid}", GetWishlist).WithName("GetWishlist");
        wishlists.MapPut("/{wishlistId:guid}/items/{isbn}", AddItem);
        wishlists.MapDelete("/{wishlistId:guid}/items/{isbn}", RemoveItem);
        return app;
    }

    private static async Task<CreatedAtRoute<WishlistView>> CreateWishlist(
        BookstoreContext db, TimeProvider clock, CancellationToken ct)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        Wishlist wishlist = new() { Id = Guid.CreateVersion7(), CreatedAtUtc = now, UpdatedAtUtc = now };
        db.Wishlists.Add(wishlist);
        await db.SaveChangesAsync(ct);
        return TypedResults.CreatedAtRoute(new WishlistView(wishlist.Id, []), "GetWishlist", new { wishlistId = wishlist.Id });
    }

    private static async Task<Results<Ok<WishlistView>, NotFound>> GetWishlist(
        Guid wishlistId, BookstoreContext db, ICatalogService catalog, CancellationToken ct)
    {
        Wishlist? wishlist = await db.Wishlists.AsNoTracking().Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == wishlistId, ct);
        return wishlist is null ? TypedResults.NotFound() : TypedResults.Ok(await ToViewAsync(wishlist, catalog, ct));
    }

    // PUT /api/wishlists/{id}/items/{isbn} → adds the book; adding it again changes nothing
    private static async Task<Results<Ok<WishlistView>, NotFound<string>>> AddItem(
        Guid wishlistId, string isbn, BookstoreContext db, ICatalogService catalog, TimeProvider clock, CancellationToken ct)
    {
        Wishlist? wishlist = await db.Wishlists.Include(w => w.Items).FirstOrDefaultAsync(w => w.Id == wishlistId, ct);
        if (wishlist is null)
        {
            return TypedResults.NotFound("Wishlist not found.");
        }
        BookInfo? book = await catalog.FindByIsbnAsync(isbn, ct);
        if (book is null)
        {
            return TypedResults.NotFound($"Book {isbn} not found.");
        }

        if (wishlist.Items.All(i => i.BookId != book.Id))
        {
            DateTime now = clock.GetUtcNow().UtcDateTime;
            wishlist.Items.Add(new WishlistItem { BookId = book.Id, AddedAtUtc = now });
            wishlist.UpdatedAtUtc = now;
            await db.SaveChangesAsync(ct);
        }
        return TypedResults.Ok(await ToViewAsync(wishlist, catalog, ct));
    }

    private static async Task<Results<Ok<WishlistView>, NotFound>> RemoveItem(
        Guid wishlistId, string isbn, BookstoreContext db, ICatalogService catalog, TimeProvider clock, CancellationToken ct)
    {
        Wishlist? wishlist = await db.Wishlists.Include(w => w.Items).FirstOrDefaultAsync(w => w.Id == wishlistId, ct);
        BookInfo? book = await catalog.FindByIsbnAsync(isbn, ct);
        if (wishlist is null || book is null)
        {
            return TypedResults.NotFound();
        }

        if (wishlist.Items.RemoveAll(i => i.BookId == book.Id) > 0)
        {
            wishlist.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(ct);
        }
        return TypedResults.Ok(await ToViewAsync(wishlist, catalog, ct));
    }

    private static async Task<WishlistView> ToViewAsync(Wishlist wishlist, ICatalogService catalog, CancellationToken ct)
    {
        IReadOnlyDictionary<int, BookInfo> books = await catalog.GetByIdsAsync([.. wishlist.Items.Select(i => i.BookId)], ct);
        List<WishlistLineView> lines =
        [
            .. wishlist.Items
                .Select(i => (Book: books[i.BookId], i.AddedAtUtc))
                .OrderBy(x => x.Book.Title)
                .Select(x => new WishlistLineView(x.Book.Isbn, x.Book.Title, x.Book.Price, x.AddedAtUtc))
        ];
        return new WishlistView(wishlist.Id, lines);
    }
}
