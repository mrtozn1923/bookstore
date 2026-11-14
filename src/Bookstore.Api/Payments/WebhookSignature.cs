using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Api.Payments;

public enum WebhookSecretMatch
{
    None,
    Primary,
    Secondary
}

public static class WebhookSignature
{
    private const string Prefix = "sha256=";

    public static string Sign(string secret, ReadOnlySpan<byte> body) =>
        Prefix + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body));

    // İki secret da denenir; karşılaştırma sabit zamanlı
    public static WebhookSecretMatch Verify(string? header, ReadOnlySpan<byte> body, string primary, string? secondary)
    {
        if (string.IsNullOrEmpty(header) || !header.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return WebhookSecretMatch.None;
        }

        if (Matches(header, body, primary))
        {
            return WebhookSecretMatch.Primary;
        }

        return !string.IsNullOrEmpty(secondary) && Matches(header, body, secondary)
            ? WebhookSecretMatch.Secondary
            : WebhookSecretMatch.None;
    }

    private static bool Matches(string header, ReadOnlySpan<byte> body, string secret) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(header),
            Encoding.ASCII.GetBytes(Sign(secret, body)));
}
