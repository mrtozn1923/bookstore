using Bookstore.Ordering.Institutional;
using Moq.Protected;

namespace Bookstore.Ordering.Legacy.Tests;

public sealed class OrderExportJobTests
{
    [Fact]
    public async Task Export_job_publishes_every_pending_order()
    {
        IReadOnlyList<Order> pending =
        [
            new(Guid.NewGuid(), "BS-2026-000042", "library@example.org", [new OrderLine("9780441172719", 20, 11.25m)]),
            new(Guid.NewGuid(), "BS-2026-000043", "school@example.org", [new OrderLine("9780261103344", 30, 8.91m)]),
        ];
        Mock<OrderExportJob> job = new();
        job.Protected()
            .Setup<Task<IReadOnlyList<Order>>>("LoadPendingAsync", ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(pending);
        job.Protected()
            .Setup<Task>("PublishAsync", ItExpr.IsAny<Order>(), ItExpr.IsAny<CancellationToken>())
            .Returns(Task.CompletedTask);

        int published = await job.Object.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, published);
        job.Protected().Verify(
            "PublishAsync", Times.Exactly(2), ItExpr.IsAny<Order>(), ItExpr.IsAny<CancellationToken>());
    }
}
