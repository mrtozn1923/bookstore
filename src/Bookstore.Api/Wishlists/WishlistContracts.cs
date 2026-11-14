namespace Bookstore.Wishlists;

public sealed record WishlistLineView(string Isbn, string Title, decimal Price, DateTime AddedAtUtc);

public sealed record WishlistView(Guid Id, IReadOnlyList<WishlistLineView> Items);

public sealed record MoveToCartRequest(Guid CartId);
