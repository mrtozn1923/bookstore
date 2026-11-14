using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;

namespace Bookstore.Api.Tests;

// Azurite started with "--oauth basic" and a self-signed certificate checks a bearer token's issuer, audience
// and lifetime, not its signature. That is enough to exercise the user delegation SAS path without Azure.
public static class AzuriteOAuth
{
    // e.g. https://127.0.0.1:10000/devstoreaccount1 (see README.md)
    public static Uri? ServiceUri { get; } =
        Environment.GetEnvironmentVariable("AZURITE_OAUTH_BLOB_URI") is { Length: > 0 } uri ? new Uri(uri) : null;

    public static BlobServiceClient CreateServiceClient() =>
        new(ServiceUri, new FakeEntraTokenCredential(), new BlobClientOptions { Transport = new HttpClientTransport(CreateHandler()) });

    // Test code only: Azurite's self-signed certificate is accepted for loopback addresses
    public static HttpClientHandler CreateHandler() => new()
    {
        ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
            errors == System.Net.Security.SslPolicyErrors.None || request.RequestUri!.IsLoopback,
    };

    private sealed class FakeEntraTokenCredential : TokenCredential
    {
        private const string TenantId = "00000000-0000-0000-0000-000000000000";

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string header = Encode(new { alg = "RS256", typ = "JWT" });
            string payload = Encode(new
            {
                aud = "https://storage.azure.com",
                iss = $"https://sts.windows.net/{TenantId}/",
                tid = TenantId,
                oid = "11111111-1111-1111-1111-111111111111",
                iat = now.AddMinutes(-5).ToUnixTimeSeconds(),
                nbf = now.AddMinutes(-5).ToUnixTimeSeconds(),
                exp = now.AddHours(1).ToUnixTimeSeconds(),
            });
            return new AccessToken($"{header}.{payload}.unsigned", now.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));

        private static string Encode(object value) =>
            Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
