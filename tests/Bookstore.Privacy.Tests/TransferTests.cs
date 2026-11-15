using Bookstore.Privacy.Consent;
using Bookstore.Privacy.Transfers;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Privacy.Tests;

public sealed class TransferTests
{
    private static readonly HolidayCalendar NoHolidays = new(new HashSet<DateOnly>());

    private static IConfiguration Services(params string[] keys) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(keys.Select(k => new KeyValuePair<string, string?>($"{k}:Endpoint", $"https://{k.ToLowerInvariant()}.example.net")))
            .Build();

    [Fact]
    public void Current_inventory_is_complete()
    {
        IReadOnlyList<string> problems = TransferInventoryCheck.Validate(
            Services("Telemetry", "Email", "Analytics", "Llm"), SubProcessors.All, NoHolidays);

        Assert.Empty(problems);
    }

    [Fact]
    public void Late_standard_contract_notification_is_reported()
    {
        // Signed on Thursday 1 October 2026: five business days end on Thursday 8 October.
        SubProcessor late = new("sms", "SMS", "Abroad", true, ["customer-profile"],
            TransferMechanism.StandardContract, "Standard contract 2", new(2026, 10, 1), new(2026, 10, 9));

        string problem = Assert.Single(TransferInventoryCheck.Validate(Services(), [late], NoHolidays));
        Assert.Equal("sms: standard contract signed 2026-10-01, notification due 2026-10-08, notified 2026-10-09", problem);
    }

    [Fact]
    public void A_running_service_cannot_rely_on_occasional_transfers()
    {
        SubProcessor llm = new("llm", "Recommendations", "Abroad", true, ["purchase-history-titles"], TransferMechanism.Occasional);

        string problem = Assert.Single(TransferInventoryCheck.Validate(Services(), [llm], NoHolidays));
        Assert.Contains("occasional", problem);
    }
}
