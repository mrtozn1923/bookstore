using Bookstore.Inventory;

namespace Bookstore.Inventory.Tests;

[TestFixture]
public class InMemoryStockLedgerTests
{
    private const string Dune = "9780441172719";
    private InMemoryStockLedger ledger = null!;

    [SetUp]
    public void SetUp() => ledger = new InMemoryStockLedger().Receive(Dune, 10);

    [Test]
    public async Task Reserving_more_than_available_reports_the_available_quantity()
    {
        InsufficientStockException? ex = await Assert.ThrowsAsync<InsufficientStockException>(
            () => ledger.ReserveAsync(Dune, 25, TestContext.CurrentContext.CancellationToken));

        Assert.That(ex?.Available, Is.EqualTo(10));
    }

    [Test]
    public async Task Reservation_reduces_available_stock()
    {
        await ledger.ReserveAsync(Dune, 4, TestContext.CurrentContext.CancellationToken);

        Assert.That(ledger.Available(Dune), Is.EqualTo(6));
    }
}
