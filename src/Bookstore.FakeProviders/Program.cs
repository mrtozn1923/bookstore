using Bookstore.FakeProviders;

// Usage: dotnet run --project src/Bookstore.FakeProviders -- [--payments Normal] [--payments-delay 5] [--carrier Normal] [--rates-delay 0.05] [--smtp Normal] [--receiver Normal]
var payments = ReadMode(args, "--payments");
var carrier = ReadMode(args, "--carrier");
var smtp = ReadMode(args, "--smtp");
var receiver = ReadMode(args, "--receiver");

using var paymentServer = FakePaymentProvider.Start(9091, new FakePaymentSettings
{
    Mode = payments,
    Delay = TimeSpan.FromSeconds(ReadSeconds(args, "--payments-delay", 5))
});
using var carrierServer = FakeLegacyCarrier.Start(9092, carrier);
using var ratesPrimary = FakeExchangeRates.Start(9093, "primary", TimeSpan.FromSeconds(ReadSeconds(args, "--rates-delay", 0.05)));
using var ratesSecondary = FakeExchangeRates.Start(9094, "secondary", TimeSpan.FromMilliseconds(50));
using var receiverServer = FakeWebhookReceiver.Start(9095, receiver, message => Console.WriteLine($"Receiver: {message}"));
// --smtp Error: no SMTP server at all (connection refused).
await using var smtpServer = smtp == FakeMode.Error ? null : new FakeSmtpServer(2525, smtp);

Console.WriteLine($"Fake payment provider : {paymentServer.Url} ({payments})");
Console.WriteLine($"Fake legacy carrier   : {carrierServer.Url} ({carrier})");
Console.WriteLine($"Fake exchange rates   : {ratesPrimary.Url} (primary), {ratesSecondary.Url} (secondary)");
Console.WriteLine($"Fake SMTP server      : {(smtpServer is null ? "not running" : $"localhost:{smtpServer.Port}")} ({smtp})");
Console.WriteLine($"Fake webhook receiver : {receiverServer.Url}{FakeWebhookReceiver.Path} ({receiver})");
Console.WriteLine("Press Ctrl+C to stop.");

var stop = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.TrySetResult(); };
await stop.Task;

static double ReadSeconds(string[] args, string name, double defaultValue)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? double.Parse(args[index + 1], System.Globalization.CultureInfo.InvariantCulture) : defaultValue;
}

static FakeMode ReadMode(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length
        ? Enum.Parse<FakeMode>(args[index + 1], ignoreCase: true)
        : FakeMode.Normal;
}
