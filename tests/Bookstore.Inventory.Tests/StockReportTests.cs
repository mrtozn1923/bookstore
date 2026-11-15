using Bookstore.Inventory;

namespace Bookstore.Inventory.Tests;

public class StockReportTests
{
    private static List<StockItem> Items() =>
    [
        new() { Id = 1, Isbn = "9780441013593", Title = "Dune", Warehouse = "Ankara", Quantity = 3, UnitCost = 7.50m },
        new() { Id = 2, Isbn = "9780261103344", Title = "The Hobbit", Warehouse = "Istanbul", Quantity = 40, UnitCost = 90.00m },
        new() { Id = 3, Isbn = "9780141439587", Title = "Emma", Warehouse = "Ankara", Quantity = 12, UnitCost = 35.25m, ReorderLevel = 15 },
        new() { Id = 4, Isbn = "9780141439549", Title = "Middlemarch", Warehouse = "Istanbul", Quantity = 6, UnitCost = 48.00m, ReorderLevel = 10 }
    ];

    [Fact]
    public Task Build_report() =>
        Verify(new StockReport().Build(Items()));

    [Fact]
    public Task Reorder_email() =>
        Verify(new StockReport().ReorderEmailBody(Items()));

    [Fact]
    public void Pick_list_has_no_reorder_marks() =>
        Assert.Equal(
            $"Pick list for Ankara:{Environment.NewLine}9780441013593 Dune (Ankara): 3{Environment.NewLine}9780141439587 Emma (Ankara): 12{Environment.NewLine}",
            new StockReport().PickList(Items(), "Ankara"));
}
