using System.Diagnostics;
using Bookstore.Shipping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Polly.Timeout;
using WireMock.Server;
using WireMock.Settings;

namespace Bookstore.Shipping.Tests;

// CarrierClient with the real resilience pipeline from ShippingModule, against WireMock's carrier mappings.
public sealed class CarrierClientTests : IDisposable
{
    private readonly WireMockServer carrier;
    private readonly ServiceProvider services;

    public CarrierClientTests()
    {
        carrier = WireMockServer.Start(new WireMockServerSettings { Urls = ["http://127.0.0.1:0"] });
        carrier.ReadStaticMappings(Path.Combine(AppContext.BaseDirectory, "mappings"));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Shipping:BaseAddress"] = carrier.Url,
                ["Shipping:ApiKey"] = "fake-carrier-key-for-tests",
                ["Shipping:AttemptTimeout"] = "00:00:00.5"
            })
            .Build();
        services = new ServiceCollection()
            .AddLogging()
            .AddShippingModule(configuration, new TestEnvironment())
            .BuildServiceProvider();
    }

    [Fact]
    public async Task Retries_after_a_503_and_returns_the_second_answer()
    {
        CarrierClient client = services.GetRequiredService<CarrierClient>();

        CarrierQuote quote = await client.GetQuoteAsync(new QuoteRequest("Kars", 1, 900), TestContext.Current.CancellationToken);

        Assert.Equal("wm-kars", quote.QuoteId);
        Assert.Equal([503, 200], carrier.LogEntries.Select(e => Convert.ToInt32(e.ResponseMessage!.StatusCode)));
    }

    [Fact]
    public async Task Gives_up_when_every_attempt_times_out()
    {
        CarrierClient client = services.GetRequiredService<CarrierClient>();
        Stopwatch watch = Stopwatch.StartNew();

        await Assert.ThrowsAsync<TimeoutRejectedException>(
            () => client.GetQuoteAsync(new QuoteRequest("Van", 1, 900), TestContext.Current.CancellationToken));

        // 1 attempt + 2 retries, each cancelled after 0.5 s; the carrier would have answered after 3 s.
        Assert.InRange(watch.Elapsed, TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Sends_the_api_key_and_the_weight()
    {
        CarrierClient client = services.GetRequiredService<CarrierClient>();

        await client.GetQuoteAsync(new QuoteRequest("Ankara", 1, 1350), TestContext.Current.CancellationToken);

        WireMock.Logging.ILogEntry entry = Assert.Single(carrier.LogEntries);
        Assert.Equal("Bearer fake-carrier-key-for-tests", entry.RequestMessage!.Headers!["Authorization"].Single());
        Assert.Contains("\"weightGrams\":1350", entry.RequestMessage.Body);
    }

    public void Dispose()
    {
        services.Dispose();
        carrier.Stop();
        carrier.Dispose();
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Bookstore.Shipping.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
