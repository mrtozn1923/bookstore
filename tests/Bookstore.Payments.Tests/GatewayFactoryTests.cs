using Bookstore.Notifications;

namespace Bookstore.Payments.Tests;

public sealed class GatewayFactoryTests
{
    [Fact]
    public async Task Unconfigured_factory_returns_a_recursive_substitute()
    {
        IPaymentGatewayFactory gateways = Substitute.For<IPaymentGatewayFactory>();
        gateways.For("TRY").ChargeAsync(Arg.Any<ChargeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChargeResult(true, "tx-2001", null));
        PaymentService sut = new(
            gateways, Substitute.For<IFraudCheck>(), Substitute.For<IPaymentAuditLog>(), Substitute.For<INotificationSender>());

        PaymentOutcome outcome = await sut.PayAsync(
            Guid.NewGuid(), 120m, "TRY", "grace@example.com", "card-token-test", TestContext.Current.CancellationToken);

        Assert.Equal(PaymentOutcome.Paid, outcome);
        Assert.Same(gateways.For("TRY"), gateways.For("TRY"));
        Assert.NotSame(gateways.For("TRY"), gateways.For("EUR"));
    }
}
