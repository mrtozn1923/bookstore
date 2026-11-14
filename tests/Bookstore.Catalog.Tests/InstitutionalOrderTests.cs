using AutoFixture.Xunit3;
using Bookstore.Ordering;
using Bookstore.Ordering.Institutional;
using NSubstitute;

namespace Bookstore.Catalog.Tests;

public sealed class InstitutionalOrderTests
{
    [Theory, BookstoreAutoData]
    public async Task Nineteen_copies_are_rejected(InstitutionalCheckout sut)
    {
        (CustomerAccount customer, IReadOnlyList<OrderLine> lines) = new InstitutionalOrderBuilder().WithCopies(19).Build();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.PlaceAsync(customer, lines, TestContext.Current.CancellationToken));

        Assert.Equal("Institutional orders need at least 20 copies; got 19.", ex.Message);
    }

    [Theory]
    [BookstoreInlineAutoData(20)]
    [BookstoreInlineAutoData(150)]
    public async Task Institutional_order_is_saved_with_every_copy(
        int copies,
        [Frozen] IStockService stock,
        [Frozen] IOrderRepository orders,
        InstitutionalCheckout sut)
    {
        stock.TryReserve(default!, default, out _).ReturnsForAnyArgs(true);
        (CustomerAccount customer, IReadOnlyList<OrderLine> lines) =
            new InstitutionalOrderBuilder().WithCopies(copies).AcrossTitles(3).Build();

        await sut.PlaceAsync(customer, lines, TestContext.Current.CancellationToken);

        await orders.Received(1).SaveAsync(
            Arg.Is<Order>(o => o.Lines.Sum(l => l.Quantity) == copies), Arg.Any<CancellationToken>());
    }
}
