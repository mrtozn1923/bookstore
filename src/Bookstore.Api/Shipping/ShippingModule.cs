using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http.HttpResults;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Bookstore.Shipping;

public sealed record ShippingQuoteRequest(
    [property: Required, StringLength(100)] string City,
    [property: Range(1, 99)] int Books);

public sealed record ShippingQuoteView(string QuoteId, string City, int Books, decimal Price, string Currency, int EstimatedDays);

public static class ShippingModule
{
    private const int GramsPerBook = 450;

    public static IServiceCollection AddShippingModule(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ShippingOptions options = configuration.GetSection(ShippingOptions.SectionName).Get<ShippingOptions>()
            ?? new ShippingOptions();
        if (options.ExtraRootCertificates.Count > 0 && !environment.IsDevelopment())
        {
            throw new InvalidOperationException("Shipping:ExtraRootCertificates is only allowed in Development.");
        }

        services.AddHttpClient<CarrierClient>(http =>
            {
                http.BaseAddress = options.BaseAddress
                    ?? throw new InvalidOperationException("Shipping:BaseAddress is missing.");
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            })
            .ConfigurePrimaryHttpMessageHandler(() => CreatePrimaryHandler(options, environment))
            .AddStandardResilienceHandler(resilience =>
            {
                // 2 s per attempt, at most 2 retries
                resilience.AttemptTimeout.Timeout = options.AttemptTimeout;
                resilience.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
                resilience.Retry.Delay = TimeSpan.FromMilliseconds(200);
                resilience.TotalRequestTimeout.Timeout = options.AttemptTimeout * (options.MaxRetryAttempts + 2);
                resilience.CircuitBreaker.SamplingDuration = options.AttemptTimeout * 10;
            });
        return services;
    }

    public static IEndpointRouteBuilder MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/shipping/quotes", GetQuote)
            .WithTags("Shipping")
            .RequireAuthorization();
        return app;
    }

    // POST /api/shipping/quotes  { "city": "Ankara", "books": 3 }  → the checkout page shows the carrier's price
    private static async Task<Results<Ok<ShippingQuoteView>, ProblemHttpResult>> GetQuote(
        ShippingQuoteRequest request, CarrierClient carrier, ILogger<CarrierClient> logger, CancellationToken ct)
    {
        try
        {
            CarrierQuote quote = await carrier.GetQuoteAsync(
                new QuoteRequest(request.City, 1, request.Books * GramsPerBook), ct);
            return TypedResults.Ok(new ShippingQuoteView(
                quote.QuoteId, request.City, request.Books, quote.Price, quote.Currency, quote.EstimatedDays));
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            logger.LogWarning("Carrier quote failed: {Error}", ex.GetType().Name);
            return TypedResults.Problem("The carrier did not return a quote. Try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static SocketsHttpHandler CreatePrimaryHandler(ShippingOptions options, IHostEnvironment environment)
    {
        // UseProxy is on by default: HTTPS_PROXY, HTTP_PROXY and NO_PROXY apply here too.
        SocketsHttpHandler handler = new() { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
        if (options.ExtraRootCertificates.Count == 0)
        {
            return handler;
        }

        X509Certificate2Collection roots = [];
        foreach (string path in options.ExtraRootCertificates)
        {
            roots.ImportFromPemFile(Path.GetFullPath(path, environment.ContentRootPath));
        }
        handler.SslOptions.RemoteCertificateValidationCallback =
            (_, certificate, _, errors) => IsTrusted(certificate, errors, roots);
        return handler;
    }

    private static bool IsTrusted(X509Certificate? certificate, SslPolicyErrors errors, X509Certificate2Collection roots)
    {
        if (errors == SslPolicyErrors.None)
        {
            return true;
        }
        // A wrong host name or a missing certificate is never accepted; only the chain is re-checked.
        if (certificate is null || errors != SslPolicyErrors.RemoteCertificateChainErrors)
        {
            return false;
        }

        using X509Chain chain = new();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.AddRange(roots);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        using X509Certificate2 leaf = X509CertificateLoader.LoadCertificate(certificate.GetRawCertData());
        return chain.Build(leaf);
    }
}
