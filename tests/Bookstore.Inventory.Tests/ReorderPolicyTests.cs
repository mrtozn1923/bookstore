using Bookstore.Inventory;

namespace Bookstore.Inventory.Tests;

[TestFixture]
public class ReorderPolicyTests
{
    private const string Dune = "9780441172719";
    private const string Hobbit = "9780261103344";
    private const string Emma = "9780141439587";

    [TestCase(30, 2.0, ExpectedResult = 15.0)]
    [TestCase(10, 4.0, ExpectedResult = 2.5)]
    [TestCase(0, 3.0, ExpectedResult = 0.0)]
    public double Days_of_cover_is_available_divided_by_daily_sales(int available, double dailySales) =>
        ReorderPolicy.DaysOfCover(available, dailySales);

    [Test]
    public void Days_of_cover_is_compared_with_a_tolerance() =>
        Assert.That(ReorderPolicy.DaysOfCover(10, 3), Is.EqualTo(3.33).Within(0.01));

    [Test]
    public void Negative_sales_are_rejected_with_the_parameter_name() =>
        Assert.That(
            () => ReorderPolicy.DaysOfCover(10, -1),
            Throws.TypeOf<ArgumentOutOfRangeException>().ParamName.EqualTo("averageDailySales"));

    [Test, Combinatorial]
    public void Days_of_cover_is_never_negative(
        [Values(0, 1, 100)] int available, [Range(0.5, 2.0, 0.5)] double dailySales) =>
        Assert.That(ReorderPolicy.DaysOfCover(available, dailySales), Is.GreaterThanOrEqualTo(0));

    private static IEnumerable<TestCaseDataWithReturn<int, int, bool>> WarehouseSplits()
    {
        yield return TestCaseData.Create(10, 3).SetName("13 copies in two warehouses").Returns(true);
        yield return TestCaseData.Create(10, 4).SetName("14 copies in two warehouses").Returns(false);
    }

    [TestCaseSource(nameof(WarehouseSplits))]
    public bool Stock_is_summed_across_warehouses(int istanbul, int ankara)
    {
        ReorderPolicy policy = new(minimumDaysOfCover: 7);
        StockItem[] items = [new(Dune, "Istanbul", istanbul, 0), new(Dune, "Ankara", ankara, 0)];

        return policy.TitlesToReorder(items, new Dictionary<string, double> { [Dune] = 2 }).Contains(Dune);
    }

    [Test]
    public void Only_titles_below_the_minimum_are_listed()
    {
        ReorderPolicy policy = new(minimumDaysOfCover: 7);
        StockItem[] items = [new(Dune, "Istanbul", 3, 1), new(Hobbit, "Istanbul", 40, 0), new(Emma, "Ankara", 5, 0)];
        Dictionary<string, double> sales = new() { [Dune] = 1, [Hobbit] = 2, [Emma] = 0.5 };

        IReadOnlyList<string> titles = policy.TitlesToReorder(items, sales);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(titles, Has.Exactly(1).EqualTo(Dune));
            Assert.That(titles, Does.Not.Contain(Hobbit));
            Assert.That(titles, Has.None.EqualTo(Emma));
        }
    }

    [Test]
    public void Large_warehouse_is_planned_in_one_pass()
    {
        ReorderPolicy policy = new(minimumDaysOfCover: 7);
        Dictionary<string, double> sales = InventoryTestRun.LargeWarehouse.ToDictionary(i => i.Isbn, _ => 1.0);

        IReadOnlyList<string> titles = policy.TitlesToReorder(InventoryTestRun.LargeWarehouse, sales);

        Assert.That(titles, Has.Count.EqualTo(1_400));
    }
}
