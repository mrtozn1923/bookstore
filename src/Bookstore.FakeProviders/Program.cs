using Bookstore.FakeProviders;

// Usage: dotnet run --project src/Bookstore.FakeProviders -- [--payments Normal] [--carrier Normal]
var payments = ReadMode(args, "--payments");
var carrier = ReadMode(args, "--carrier");

using var paymentServer = FakePaymentProvider.Start(9091, payments);
using var carrierServer = FakeLegacyCarrier.Start(9092, carrier);

Console.WriteLine($"Fake payment provider : {paymentServer.Url} ({payments})");
Console.WriteLine($"Fake legacy carrier   : {carrierServer.Url} ({carrier})");
Console.WriteLine("Press Ctrl+C to stop.");

var stop = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.TrySetResult(); };
await stop.Task;

static FakeMode ReadMode(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length
        ? Enum.Parse<FakeMode>(args[index + 1], ignoreCase: true)
        : FakeMode.Normal;
}
