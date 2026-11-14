using System.Net.Http.Json;

namespace Bookstore.Payments;

public enum ChargeOutcome
{
    Skipped,
    Succeeded,
    Failed
}

public interface IPaymentGateway
{
    Task<ChargeOutcome> ChargeAsync(int orderId, decimal amount, string currency, CancellationToken ct);
}

internal sealed record ChargeRequest(string Reference, decimal Amount, string Currency);

// Calls the payment provider (in load tests: a WireMock.Net stub, never the real provider).
internal sealed class HttpPaymentGateway(HttpClient http, ILogger<HttpPaymentGateway> logger) : IPaymentGateway
{
    public async Task<ChargeOutcome> ChargeAsync(int orderId, decimal amount, string currency, CancellationToken ct)
    {
        try
        {
            using HttpResponseMessage response = await http.PostAsJsonAsync(
                "v1/charges", new ChargeRequest($"order-{orderId}", amount, currency), ct);
            return response.IsSuccessStatusCode ? ChargeOutcome.Succeeded : ChargeOutcome.Failed;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Payment for order {OrderId} failed", orderId);
            return ChargeOutcome.Failed;
        }
    }
}

// No provider configured (local development, integration tests): the order stays "Placed".
internal sealed class NoPaymentGateway : IPaymentGateway
{
    public Task<ChargeOutcome> ChargeAsync(int orderId, decimal amount, string currency, CancellationToken ct) =>
        Task.FromResult(ChargeOutcome.Skipped);
}

public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration["Payments:BaseUrl"] is { Length: > 0 } baseUrl)
        {
            services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            });
        }
        else
        {
            services.AddSingleton<IPaymentGateway, NoPaymentGateway>();
        }
        return services;
    }
}
