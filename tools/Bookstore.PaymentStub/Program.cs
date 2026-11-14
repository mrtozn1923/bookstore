using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

// Stand-in for the payment provider during load tests: never send load to a real provider or its sandbox.
// Usage: dotnet run -c Release -- [port] [minDelayMs] [maxDelayMs]
int port = args.Length > 0 ? int.Parse(args[0]) : 5099;
int minDelay = args.Length > 1 ? int.Parse(args[1]) : 0;
int maxDelay = args.Length > 2 ? int.Parse(args[2]) : minDelay;

using WireMockServer server = WireMockServer.Start(new WireMockServerSettings { Port = port });

IResponseBuilder response = Response.Create()
    .WithStatusCode(201)
    .WithHeader("Content-Type", "application/json")
    .WithBody("""{ "id": "ch_test", "status": "succeeded" }""");
if (maxDelay > 0)
{
    // A real provider is never equally fast: every response waits a random time in [minDelay, maxDelay].
    response = response.WithRandomDelay(minDelay, maxDelay);
}

server.Given(Request.Create().WithPath("/v1/charges").UsingPost()).RespondWith(response);

Console.WriteLine($"Payment stub on http://localhost:{port}/v1/charges, delay {minDelay}-{maxDelay} ms. Ctrl+C to stop.");
using CancellationTokenSource stop = new();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
try
{
    await Task.Delay(Timeout.Infinite, stop.Token);
}
catch (OperationCanceledException)
{
}
Console.WriteLine($"Charges received: {server.LogEntries.Count()}");
