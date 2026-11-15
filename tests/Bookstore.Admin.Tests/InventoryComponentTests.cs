using Bookstore.Admin.Client.Contracts;
using Bookstore.Admin.Components.Inventory;

namespace Bookstore.Admin.Tests;

public sealed class InventoryComponentTests : BunitContext
{
    private static readonly StockView Emma = new("9780141439587", "Emma", OnHand: 25, Reserved: 0, Available: 25);

    [Theory]
    [InlineData(0, 5, "<span class=\"badge bg-danger\">Out of stock</span>")]
    [InlineData(3, 5, "<span class=\"badge bg-warning text-dark\">Low: 3 left</span>")]
    [InlineData(8, 5, "")]
    public void Badge_depends_on_available_stock_and_threshold(int available, int threshold, string expected)
    {
        IRenderedComponent<LowStockBadge> badge = Render<LowStockBadge>(p => p
            .Add(b => b.Available, available)
            .Add(b => b.Threshold, threshold));

        badge.MarkupMatches(expected);
    }

    [Fact]
    public async Task Restock_raises_the_callback_and_shows_saving_until_it_completes()
    {
        TaskCompletionSource done = new();
        RestockRequest? raised = null;
        IRenderedComponent<BookRow> row = Render<BookRow>(p => p
            .Add(r => r.Item, Emma)
            .Add(r => r.LowStockThreshold, 5)
            .Add(r => r.OnRestock, (RestockRequest request) => { raised = request; return done.Task; }));

        row.Find("input").Change("15");
        row.Find("button").Click();

        Assert.Equal(new RestockRequest("9780141439587", 15), raised);
        Assert.Equal("Saving...", row.Find("button").TextContent.Trim());
        Assert.True(row.Find("button").HasAttribute("disabled"));

        done.SetResult();
        await row.WaitForAssertionAsync(() => Assert.Equal("Restock", row.Find("button").TextContent.Trim()));
    }

    [Fact]
    public void A_threshold_change_that_does_not_move_the_badge_does_not_render_the_row()
    {
        IRenderedComponent<BookRow> row = Render<BookRow>(p => p.Add(r => r.Item, Emma).Add(r => r.LowStockThreshold, 5));
        int before = row.RenderCount;

        row.Render(p => p.Add(r => r.LowStockThreshold, 6));
        Assert.Equal(before, row.RenderCount);

        row.Render(p => p.Add(r => r.LowStockThreshold, 30));
        Assert.Equal(before + 1, row.RenderCount);
        Assert.Contains("Low: 25 left", row.Markup);
    }

    [Fact]
    public void The_list_shows_its_empty_template_when_nothing_matches()
    {
        IRenderedComponent<BookList> list = Render<BookList>(p => p.Add(l => l.Items, []).Add(l => l.LowStockThreshold, 5));

        Assert.Equal("No books match the filter.", list.Find("tbody td").TextContent.Trim());
    }
}
