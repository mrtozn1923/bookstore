using Bookstore.Data;
using Bookstore.Ordering;
using Microsoft.Extensions.Logging.Testing;

namespace Bookstore.Privacy.Tests;

public sealed class TelemetryTests
{
    private static readonly DateTime Now = new(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Checkout_telemetry_contains_no_personal_data()
    {
        using var testDb = new TestDb();
        await using BookstoreContext db = testDb.CreateContext();
        Customer ada = FakePeople.ToCustomer(0, Now.AddMonths(-8));
        db.Customers.Add(ada);
        await db.SaveChangesAsync();
        var request = new CheckoutRequest($"Example St. 1, {ada.City}", ada.FullName, 412.50m);

        using var telemetry = new TelemetryCapture("Bookstore.Ordering");
        var checkout = new CheckoutService(db, new FixedClock(Now), new FakeLogger<CheckoutService>(telemetry.Logs));
        await checkout.PlaceOrderAsync(ada.Id, request);

        // Canary values: the test knows the personal data it put in, so it can look for it everywhere.
        string[] leaks = [.. telemetry.FindLeaks([ada.FullName, ada.Email, ada.Phone!, request.ShippingAddress])];
        Assert.True(leaks.Length == 0, "Personal data in telemetry:\n" + string.Join("\n", leaks));

        string[] tags = [.. telemetry.UnexpectedMetricTags(new HashSet<string> { "channel" })];
        Assert.True(tags.Length == 0, "Metric tags not in the allowlist: " + string.Join(", ", tags));
    }
}
