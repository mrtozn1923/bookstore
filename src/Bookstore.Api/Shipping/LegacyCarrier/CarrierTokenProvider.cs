using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Bookstore.Shipping.LegacyCarrier;

public sealed class LegacyCarrierOptions
{
    public required Uri BaseUrl { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}

// Singleton: one cached token for the whole process, refreshed shortly before it expires.
public sealed class CarrierTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<LegacyCarrierOptions> options,
    TimeProvider timeProvider,
    ILogger<CarrierTokenProvider> logger)
{
    public const string AuthClientName = "legacy-carrier-auth";
    private static readonly TimeSpan RefreshBeforeExpiry = TimeSpan.FromSeconds(30);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private (string Value, DateTimeOffset ExpiresAt)? _token;

    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_token is { } cached && timeProvider.GetUtcNow() < cached.ExpiresAt - RefreshBeforeExpiry)
        {
            return cached.Value;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Another request may have refreshed the token while we were waiting.
            if (_token is { } fresh && timeProvider.GetUtcNow() < fresh.ExpiresAt - RefreshBeforeExpiry)
            {
                return fresh.Value;
            }

            var client = httpClientFactory.CreateClient(AuthClientName);
            using var response = await client.PostAsync("oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = options.Value.ClientId,
                ["client_secret"] = options.Value.ClientSecret
            }), cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Empty token response.");
            _token = (body.AccessToken, timeProvider.GetUtcNow().AddSeconds(body.ExpiresIn));
            logger.LogInformation("Fetched a new carrier token, expires in {ExpiresIn} s", body.ExpiresIn);
            return body.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
