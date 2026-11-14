using Bookstore.Notifications;
using NSubstitute.ExceptionExtensions;

namespace Bookstore.Payments.Tests;

public sealed class PaymentServiceTests
{
    private static readonly ChargeResult Approved = new(true, "tx-1001", null);

    private readonly IPaymentGatewayFactory gateways = Substitute.For<IPaymentGatewayFactory>();
    private readonly IPaymentGateway gateway = Substitute.For<IPaymentGateway>();
    private readonly IFraudCheck fraud = Substitute.For<IFraudCheck>();
    private readonly IPaymentAuditLog audit = Substitute.For<IPaymentAuditLog>();
    private readonly INotificationSender notifications = Substitute.For<INotificationSender>();
    private readonly PaymentService sut;
    private readonly Guid orderId = Guid.Parse("8c6f1c9e-4f1a-4d7e-9a43-2a4f3c1d0b11");

    public PaymentServiceTests()
    {
        gateways.For("TRY").Returns(gateway);
        gateway.ProviderName.Returns("card-gateway");
        sut = new PaymentService(gateways, fraud, audit, notifications);
    }

    private Task<PaymentOutcome> Pay(decimal amount) =>
        sut.PayAsync(orderId, amount, "TRY", "ada@example.com", "card-token-test", TestContext.Current.CancellationToken);

    [Fact]
    public async Task Approved_payment_sends_one_receipt_to_the_customer()
    {
        gateway.ChargeAsync(Arg.Any<ChargeRequest>(), Arg.Any<CancellationToken>()).Returns(Approved);

        PaymentOutcome outcome = await Pay(349.90m);

        Assert.Equal(PaymentOutcome.Paid, outcome);
        await notifications.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "ada@example.com" && m.Body.Contains("349.90 TRY")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Gateway_timeout_is_retried_once()
    {
        gateway.ChargeAsync(Arg.Any<ChargeRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<ChargeResult>(new TimeoutException()),
                Task.FromResult(Approved));

        PaymentOutcome outcome = await Pay(349.90m);

        Assert.Equal(PaymentOutcome.Paid, outcome);
        await gateway.Received(2).ChargeAsync(Arg.Any<ChargeRequest>(), Arg.Any<CancellationToken>());
        audit.Received(1).Write("timeout 8c6f1c9e-4f1a-4d7e-9a43-2a4f3c1d0b11 via card-gateway, retrying");
    }

    [Fact]
    public async Task Amount_above_the_card_limit_is_declined()
    {
        gateway.ChargeAsync(Arg.Any<ChargeRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<ChargeRequest>().Amount > 5_000m
                ? new ChargeResult(false, null, "limit_exceeded")
                : Approved);

        Assert.Equal(PaymentOutcome.Declined, await Pay(7_500m));
        Assert.Equal(PaymentOutcome.Paid, await Pay(349.90m));
        audit.Received(1).Write(Arg.Is<string>(e => e.EndsWith("limit_exceeded")));
    }

    [Fact]
    public async Task High_fraud_score_blocks_the_payment_before_charging()
    {
        fraud.TryScore("ada@example.com", Arg.Any<decimal>(), out Arg.Any<int>())
            .Returns(call =>
            {
                call[2] = 91;
                return true;
            });

        PaymentOutcome outcome = await Pay(349.90m);

        Assert.Equal(PaymentOutcome.Blocked, outcome);
        gateways.DidNotReceiveWithAnyArgs().For(default!);
        audit.Received().Write("blocked 8c6f1c9e-4f1a-4d7e-9a43-2a4f3c1d0b11 score=91");
    }

    [Fact]
    public async Task Audit_entries_follow_the_payment_flow()
    {
        List<string> entries = [];
        audit.When(a => a.Write(Arg.Any<string>())).Do(call => entries.Add(call.Arg<string>()));
        gateway.ChargeAsync(Arg.Any<ChargeRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChargeResult>(new TimeoutException()), Task.FromResult(Approved));

        await Pay(349.90m);

        Assert.Equal(
            [
                "timeout 8c6f1c9e-4f1a-4d7e-9a43-2a4f3c1d0b11 via card-gateway, retrying",
                "charged 8c6f1c9e-4f1a-4d7e-9a43-2a4f3c1d0b11 tx-1001",
            ],
            entries);
    }

    [Fact]
    public async Task Failing_audit_log_stops_before_the_receipt()
    {
        gateway.ChargeAsync(Arg.Any<ChargeRequest>(), Arg.Any<CancellationToken>()).Returns(Approved);
        audit.When(a => a.Write(Arg.Is<string>(e => e.StartsWith("charged"))))
            .Do(_ => throw new IOException("Audit store unavailable."));

        await Assert.ThrowsAsync<IOException>(() => Pay(349.90m));

        await notifications.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Gateway_error_other_than_timeout_is_not_retried()
    {
        gateway.ChargeAsync(Arg.Any<ChargeRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("503 Service Unavailable"));

        await Assert.ThrowsAsync<HttpRequestException>(() => Pay(349.90m));

        await gateway.Received(1).ChargeAsync(Arg.Any<ChargeRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Each_payment_sends_its_own_receipt()
    {
        gateway.ChargeAsync(Arg.Any<ChargeRequest>(), Arg.Any<CancellationToken>()).Returns(Approved);
        await Pay(349.90m);
        notifications.ClearReceivedCalls();

        await Pay(120.00m);

        await notifications.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.Body.Contains("120.00 TRY")), Arg.Any<CancellationToken>());
    }
}
