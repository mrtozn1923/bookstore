using Bookstore.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Inventory.Tests;

public class ReorderQueryTests
{
    // ToQueryString only generates SQL; it does not open a connection.
    private static BookstoreContext NewContext() =>
        new(new DbContextOptionsBuilder<BookstoreContext>()
            .UseSqlServer("Server=sql1;Database=BookstoreDb;Integrated Security=true;TrustServerCertificate=true")
            .Options);

    [Fact]
    public void Reorder_rule_is_translated_to_sql()
    {
        using BookstoreContext db = NewContext();

        string sql = db.StockItems.Where(StockItem.NeedsReorderExpression).ToQueryString();

        Assert.Contains("WHERE [s].[Quantity] < [s].[ReorderLevel]", sql);
    }

    [Fact]
    public void Calling_the_property_inside_a_query_is_not_translated()
    {
        using BookstoreContext db = NewContext();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.StockItems.Where(i => i.NeedsReorder).ToQueryString());
        Assert.Contains("could not be translated", ex.Message);
    }

    [Theory]
    [InlineData(4, 5, true)]
    [InlineData(5, 5, false)]
    public void Property_and_expression_agree(int quantity, int level, bool expected)
    {
        StockItem item = new() { Quantity = quantity, ReorderLevel = level };

        Assert.Equal(expected, item.NeedsReorder);
    }
}
