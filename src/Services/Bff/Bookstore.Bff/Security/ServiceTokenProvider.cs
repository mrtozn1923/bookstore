using System.Text.Json.Serialization;

namespace Bookstore.Bff.Security;

// The BFF's own identity for calls that are not made on behalf of a user (the book page's stock query).
// OAuth 2.0 client credentials: client id + secret for an access token, cached until shortly before it expires.
public sealed class ServiceTokenProvider(HttpClient http, IConfiguration configuration, TimeProvider clock)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private string? token;
    private DateTimeOffset expiresAt;

    public async Task<string> GetAsync(CancellationToken ct)
    {
        if (token is not null && clock.GetUtcNow() < expiresAt)
        {
            return token;
        }

        await gate.WaitAsync(ct);
        try
        {
            if (token is not null && clock.GetUtcNow() < expiresAt)
            {
                return token;
            }

            using FormUrlEncodedContent form = new(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "bookstore-bff-service",
                ["client_secret"] = configuration["Keycloak:ServiceClientSecret"]
                    ?? throw new InvalidOperationException("Keycloak:ServiceClientSecret is not configured.")
            });
            using HttpResponseMessage response = await http.PostAsync("/realms/bookstore/protocol/openid-connect/token", form, ct);
            response.EnsureSuccessStatusCode();
            TokenResponse result = (await response.Content.ReadFromJsonAsync<TokenResponse>(ct))!;

            token = result.AccessToken;
            // Renew 30 seconds early so that a token never expires on its way to Inventory.
            expiresAt = clock.GetUtcNow().AddSeconds(result.ExpiresIn - 30);
            return token;
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
