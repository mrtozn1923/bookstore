namespace Bookstore.Payments.Tests;

public sealed class SettlementTrackerTests
{
    [Fact]
    public void Settlements_raised_by_the_gateway_are_summed()
    {
        IPaymentGateway gateway = Substitute.For<IPaymentGateway>();
        using SettlementTracker tracker = new(gateway);

        gateway.Settled += Raise.EventWith(new PaymentSettledEventArgs("tx-1001", 349.90m));
        gateway.Settled += Raise.EventWith(gateway, new PaymentSettledEventArgs("tx-1002", 120.00m));

        Assert.Equal(["tx-1001", "tx-1002"], tracker.SettledTransactions);
        Assert.Equal(469.90m, tracker.SettledTotal);
    }

    [Fact]
    public void Disposed_tracker_stops_listening()
    {
        IPaymentGateway gateway = Substitute.For<IPaymentGateway>();
        SettlementTracker tracker = new(gateway);
        tracker.Dispose();

        gateway.Settled += Raise.EventWith(new PaymentSettledEventArgs("tx-1003", 50m));

        Assert.Empty(tracker.SettledTransactions);
    }
}
