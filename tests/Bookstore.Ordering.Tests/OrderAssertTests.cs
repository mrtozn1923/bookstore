using static Bookstore.Ordering.Tests.Books;

namespace Bookstore.Ordering.Tests;

public sealed class OrderAssertTests
{
    [Fact]
    public void Order_has_two_copies_of_dune()
    {
        OrderLine line = OrderAssert.HasLine(DuneAndHobbit(), Dune, 2);

        Assert.Equal(12.50m, line.UnitPrice);
    }
}
