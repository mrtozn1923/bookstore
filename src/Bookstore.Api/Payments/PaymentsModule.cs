using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bookstore.Payments;

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    // Shared with the payment provider. Development uses a readable fake value; other environments read it from a secret store.
    public string WebhookSecret { get; set; } = "";

    public TimeSpan SignatureTolerance { get; set; } = TimeSpan.FromMinutes(5);
}

public sealed record PaymentEvent(string EventId, string Type, int OrderId, decimal Amount, string Currency);

public static class PaymentsModule
{
    public const string TimestampHeader = "X-Payment-Timestamp";
    public const string SignatureHeader = "X-Payment-Signature";

    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        services.AddOptions<PaymentsOptions>()
            .BindConfiguration(PaymentsOptions.SectionName)
            .Validate(o => o.WebhookSecret.Length >= 16, "Payments:WebhookSecret must be at least 16 characters.")
            .ValidateOnStart();
        services.AddMemoryCache();
        return services;
    }

    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        // The provider calls this without a user token; the signature is the authentication.
        app.MapPost("/api/payments/webhooks", ReceiveWebhook).WithTags("Payments").AllowAnonymous();
        return app;
    }

    private static async Task<Results<Ok, ProblemHttpResult>> ReceiveWebhook(
        HttpRequest request, IOptions<PaymentsOptions> options, TimeProvider clock, IMemoryCache seen,
        ILogger<PaymentEvent> logger, CancellationToken ct)
    {
        using MemoryStream buffer = new();
        await request.Body.CopyToAsync(buffer, ct);
        byte[] body = buffer.ToArray();

        string? failure = Verify(request.Headers, body, options.Value, clock.GetUtcNow());
        if (failure is not null)
        {
            logger.LogWarning("Rejected payment webhook: {Reason}", failure);
            return TypedResults.Problem("Invalid webhook signature.", statusCode: StatusCodes.Status401Unauthorized);
        }

        PaymentEvent? payment = JsonSerializer.Deserialize<PaymentEvent>(body, JsonSerializerOptions.Web);
        if (payment is null)
        {
            return TypedResults.Problem("Empty event.", statusCode: StatusCodes.Status400BadRequest);
        }

        // Providers retry deliveries; the same event must not be processed twice.
        if (!seen.TryGetValue(payment.EventId, out _))
        {
            seen.Set(payment.EventId, true, TimeSpan.FromHours(24));
            logger.LogInformation("Payment event {EventId} {Type} for order {OrderId}: {Amount} {Currency}",
                payment.EventId, payment.Type, payment.OrderId, payment.Amount, payment.Currency);
            // The real handler marks the order Paid in the same step (External Service Integration and Resilience 08).
        }
        else
        {
            logger.LogInformation("Payment event {EventId} already processed", payment.EventId);
        }
        return TypedResults.Ok();
    }

    // Signature = hex(HMAC-SHA256(secret, "{timestamp}.{raw body}")), header value "v1={hex}".
    private static string? Verify(IHeaderDictionary headers, byte[] body, PaymentsOptions options, DateTimeOffset now)
    {
        if (!long.TryParse(headers[TimestampHeader], NumberStyles.None, CultureInfo.InvariantCulture, out long unixSeconds))
        {
            return "missing timestamp";
        }
        // Blocks replays of old requests (default tolerance: 5 minutes).
        TimeSpan age = now - DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        if (age.Duration() > options.SignatureTolerance)
        {
            return $"timestamp outside tolerance ({age.TotalSeconds:F0}s)";
        }

        string? header = headers[SignatureHeader];
        if (header is null || !header.StartsWith("v1=", StringComparison.Ordinal))
        {
            return "missing signature";
        }

        byte[] expected = Sign(options.WebhookSecret, unixSeconds, body);
        byte[] actual;
        try
        {
            actual = Convert.FromHexString(header.AsSpan(3));
        }
        catch (FormatException)
        {
            return "malformed signature";
        }
        return CryptographicOperations.FixedTimeEquals(expected, actual) ? null : "signature mismatch";
    }

    public static byte[] Sign(string secret, long unixSeconds, byte[] body)
    {
        byte[] signed = [.. Encoding.UTF8.GetBytes($"{unixSeconds}."), .. body];
        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), signed);
    }
}
