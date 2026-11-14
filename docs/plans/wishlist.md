# Plan: wishlist

Goal: an anonymous customer keeps a list of books to buy later and sees each book's current title and price.

## Decisions (reviewed)
- Module folder `src/Bookstore.Api/Wishlists/`, SQL schema `wishlists`. No separate service class:
  endpoints use `BookstoreContext` and `ICatalogService`, like `OrderingModule`.
- A wishlist is identified by a Guid (`Guid.CreateVersion7()`), like a cart. No quantity: a book is either on the list or not.
- Prices are not stored; `GET` shows the current price from the catalog.
- Adding the same book twice is not an error (idempotent `PUT`). Unique index on (WishlistId, BookId).

## Endpoints
- `POST /api/wishlists` → 201 + `WishlistView`, Location `/api/wishlists/{id}`
- `GET /api/wishlists/{id}` → 200 `WishlistView` | 404
- `PUT /api/wishlists/{id}/items/{isbn}` → 200 `WishlistView` | 404 (wishlist or book not found)
- `DELETE /api/wishlists/{id}/items/{isbn}` → 200 `WishlistView` | 404

`WishlistView(Guid Id, IReadOnlyList<WishlistLineView> Items)`,
`WishlistLineView(string Isbn, string Title, decimal Price, DateTime AddedAtUtc)`, items ordered by title.

## Steps
- [x] 1. Integration tests in `tests/Bookstore.Api.Tests/WishlistTests.cs` (red)
- [x] 2. Entities `Wishlist`, `WishlistItem` + configuration + `DbSet` + migration `AddWishlists`
- [x] 3. `WishlistsModule` with the four endpoints; register in `Program.cs` (green)
- [x] 4. Review the diff, run all tests, commit
