using Bookstore.Ordering;

namespace Bookstore.Ordering.Tests;

public class WishlistTests
{
    private const string Dune = "9780441013593";

    [Fact]
    public void Add_keeps_a_book_once()
    {
        Wishlist wishlist = [];
        wishlist.Add(Dune);
        wishlist.Add(Dune);

        Assert.Single(wishlist);
    }

    [Fact]
    public void AddRange_keeps_a_book_once_too()
    {
        Wishlist wishlist = [];
        wishlist.Add(Dune);
        wishlist.AddRange([Dune, Dune]);

        Assert.Single(wishlist);
    }
}
