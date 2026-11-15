namespace Bookstore.Notifications.Tests;

public class NotifierTests
{
    [Fact]
    public Task Shipped_notifications() =>
        Combination()
            .Verify(
                (int customerId, int books, bool viaSms, bool urgent) =>
                {
                    Inbox inbox = new();
                    FakeSmsGateway sms = new();
                    Notifier notifier = new(inbox, sms, TestData.Phones, TestData.SmsOptOut);

#pragma warning disable CS0618 // The old method keeps its contract until 2.0.
                    int code = notifier.SendNotification(TestData.OrderOf(customerId, books), viaSms, urgent);
#pragma warning restore CS0618

                    return new { Code = code, Inbox = inbox.All.Select(n => n.Text).ToList(), Sms = sms.Sent };
                },
                [1, 2, 3],
                [0, 1, 2],
                [false, true],
                [false, true]);

    [Fact]
    public void Sms_goes_to_the_customer_and_the_inbox()
    {
        Inbox inbox = new();
        FakeSmsGateway sms = new();
        Notifier notifier = new(inbox, sms, TestData.Phones, TestData.SmsOptOut);

        SmsResult result = notifier.NotifyShippedBySms(TestData.OrderOf(1, 2));

        Assert.Equal(SmsResult.Sent, result);
        Assert.Equal(["555-0101: Your order of 2 books has shipped."], sms.Sent);
        Assert.Single(inbox.All);
    }

    [Fact]
    public void Opted_out_customer_is_skipped_not_an_error()
    {
        Notifier notifier = new(new Inbox(), new FakeSmsGateway(), TestData.Phones, TestData.SmsOptOut);

        Assert.Equal(SmsResult.Skipped, notifier.NotifyShippedBySms(TestData.OrderOf(2, 1)));
    }

    [Fact]
    public void Empty_order_is_a_bug_in_the_caller()
    {
        Notifier notifier = new(new Inbox(), new FakeSmsGateway(), TestData.Phones, TestData.SmsOptOut);

        Assert.Throws<ArgumentException>(() => notifier.NotifyShipped(TestData.OrderOf(1, 0)));
    }
}
