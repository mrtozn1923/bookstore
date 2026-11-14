using Bookstore.Configuration;
using Bookstore.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Tests;

public sealed class LastKnownGoodTests
{
    [Fact]
    public void A_valid_change_is_used()
    {
        SwitchableMonitor monitor = new(new PaymentsOptions { TimeoutSeconds = 3 });
        LastKnownGood<PaymentsOptions> options = new(monitor, NullLogger<LastKnownGood<PaymentsOptions>>.Instance);

        monitor.Current = () => new PaymentsOptions { TimeoutSeconds = 6 };

        Assert.Equal(6, options.Value.TimeoutSeconds);
    }

    [Fact]
    public void An_invalid_change_keeps_the_last_valid_value_until_it_is_fixed()
    {
        SwitchableMonitor monitor = new(new PaymentsOptions { TimeoutSeconds = 6, CheckoutEnabled = false });
        LastKnownGood<PaymentsOptions> options = new(monitor, NullLogger<LastKnownGood<PaymentsOptions>>.Instance);

        monitor.Current = () => throw new OptionsValidationException(
            Options.DefaultName, typeof(PaymentsOptions), ["The field TimeoutSeconds must be between 1 and 30."]);
        Assert.Equal(6, options.Value.TimeoutSeconds);
        Assert.False(options.Value.CheckoutEnabled);

        monitor.Current = () => new PaymentsOptions { TimeoutSeconds = 3, CheckoutEnabled = true };
        Assert.Equal(3, options.Value.TimeoutSeconds);
    }

    private sealed class SwitchableMonitor(PaymentsOptions initial) : IOptionsMonitor<PaymentsOptions>
    {
        public Func<PaymentsOptions> Current { get; set; } = () => initial;
        public PaymentsOptions CurrentValue => Current();
        public PaymentsOptions Get(string? name) => Current();
        public IDisposable? OnChange(Action<PaymentsOptions, string?> listener) => null;
    }
}
