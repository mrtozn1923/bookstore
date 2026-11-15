using System.Collections.Immutable;

namespace Bookstore.Ordering.Carts;

public sealed record Cart(ImmutableList<CartLine> Lines, string? CouponCode = null)
{
    public static Cart Empty { get; } = new(ImmutableList<CartLine>.Empty);

    public Cart Add(CartLine line) => this with { Lines = Lines.Add(line) };

    public Cart WithoutEmptyLines() => this with { Lines = Lines.RemoveAll(line => line.Quantity <= 0) };

    public Cart ChangeQuantity(string isbn, int quantity) =>
        this with { Lines = Lines.ConvertAll(line => line.Isbn == isbn ? line with { Quantity = quantity } : line) };
}

public readonly record struct Money(decimal Amount, string Currency);

public sealed record ShallowCart(List<CartLine> Lines, string? CouponCode = null);
