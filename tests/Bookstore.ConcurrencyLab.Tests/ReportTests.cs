using Bookstore.ConcurrencyLab.Reports;

namespace Bookstore.ConcurrencyLab.Tests;

public class ReportTests
{
    private static readonly Order[] Orders = SampleOrders.Create(2_000, 22);

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void Parallel_report_equals_single_threaded_report(int workers) =>
        Assert.Equal(MonthlyReport.Build(Orders, 1), MonthlyReport.Build(Orders, workers));

    [Fact]
    public void Per_day_chain_is_deterministic_but_differs_from_single_chain()
    {
        (decimal revenue, string perDay) = MonthlyReport.BuildPerDay(Orders, 8);

        Assert.Equal(MonthlyReport.BuildPerDay(Orders, 1), (revenue, perDay));
        Assert.Equal(MonthlyReport.Build(Orders, 1).Revenue, revenue);
        Assert.NotEqual(MonthlyReport.Build(Orders, 1).AuditHash, perDay);
    }
}
