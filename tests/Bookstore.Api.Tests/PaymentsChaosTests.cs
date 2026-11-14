using System.Net;
using System.Net.Http.Json;
using Bookstore.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Bookstore.Api.Tests;

public sealed class PaymentsChaosTests
{
    private static readonly PaymentRequest Request = new(42, 249.90m, "TRY");

    [Fact]
    public async Task Enabled_chaos_with_rate_1_fails_every_payment_outside_production()
    {
        IPaymentGateway payments = Build(Environments.Development, chaosEnabled: true);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => payments.AuthorizeAsync(Request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Chaos_is_not_registered_in_production_even_if_enabled_in_configuration()
    {
        IPaymentGateway payments = Build(Environments.Production, chaosEnabled: true);

        PaymentResult result = await payments.AuthorizeAsync(Request, TestContext.Current.CancellationToken);

        Assert.Equal("authorized", result.Status);
    }

    [Fact]
    public async Task Disabled_chaos_lets_payments_through()
    {
        IPaymentGateway payments = Build(Environments.Development, chaosEnabled: false);

        PaymentResult result = await payments.AuthorizeAsync(Request, TestContext.Current.CancellationToken);

        Assert.Equal("authorized", result.Status);
    }

    [Fact]
    public async Task One_503_from_the_provider_is_retried_once_with_the_same_idempotency_key()
    {
        FailsOnce provider = new();
        IPaymentGateway payments = Build(Environments.Production, chaosEnabled: false, provider);

        PaymentResult result = await payments.AuthorizeAsync(Request, TestContext.Current.CancellationToken);

        Assert.Equal("authorized", result.Status);
        Assert.Equal(["order-42", "order-42"], provider.IdempotencyKeys);
    }

    private static IPaymentGateway Build(string environment, bool chaosEnabled, HttpMessageHandler? provider = null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:BaseAddress"] = "http://payments.example.com",
                ["Chaos:Enabled"] = chaosEnabled.ToString(),
                ["Chaos:FaultRate"] = "1"
            })
            .Build();
        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddPaymentsModule(new HostingEnvironment { EnvironmentName = environment });
        // The "provider" always authorizes; only the chaos strategy can make a payment fail.
        services.AddHttpClient(nameof(IPaymentGateway)).ConfigurePrimaryHttpMessageHandler(() => provider ?? new AlwaysAuthorizes());
        return services.BuildServiceProvider().GetRequiredService<IPaymentGateway>();
    }

    private sealed class AlwaysAuthorizes : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PaymentResult("ch_42", "authorized"))
            });
    }

    // Answers 503 to the first call and authorizes the second; remembers the Idempotency-Key of every call.
    private sealed class FailsOnce : HttpMessageHandler
    {
        public List<string> IdempotencyKeys { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            IdempotencyKeys.Add(request.Headers.GetValues("Idempotency-Key").Single());
            return Task.FromResult(IdempotencyKeys.Count == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new PaymentResult("ch_42", "authorized")) });
        }
    }
}
