using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Payments.Webhooks;

// Standard Webhooks verification: HMAC-SHA256 over "webhook-id.webhook-timestamp.body" with the shared secret.
public sealed class WebhookSignature(IConfiguration configuration, TimeProvider clock)
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(5);

    public bool IsValid(IHeaderDictionary headers, string body, out string error)
    {
        string id = headers["webhook-id"].ToString();
        string timestamp = headers["webhook-timestamp"].ToString();
        string signatures = headers["webhook-signature"].ToString();
        if (id.Length == 0 || timestamp.Length == 0 || signatures.Length == 0)
        {
            error = "missing webhook headers";
            return false;
        }

        // An old timestamp is a replay of a captured request, even when its signature is correct.
        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out long seconds) ||
            (clock.GetUtcNow() - DateTimeOffset.FromUnixTimeSeconds(seconds)).Duration() > Tolerance)
        {
            error = "timestamp outside tolerance";
            return false;
        }

        string secret = configuration["PaymentProvider:WebhookSecret"]
            ?? throw new InvalidOperationException("PaymentProvider:WebhookSecret is not configured.");
        byte[] key = Convert.FromBase64String(secret["whsec_".Length..]);
        byte[] expected = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{id}.{timestamp}.{body}"));

        // The header may carry several signatures ("v1,abc v1,def") while the secret is being rotated.
        foreach (string candidate in signatures.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (candidate.StartsWith("v1,", StringComparison.Ordinal) &&
                TryFromBase64(candidate[3..], out byte[] actual) &&
                CryptographicOperations.FixedTimeEquals(actual, expected))
            {
                error = "";
                return true;
            }
        }
        error = "signature mismatch";
        return false;
    }

    private static bool TryFromBase64(string value, out byte[] bytes)
    {
        bytes = new byte[value.Length];
        bool ok = Convert.TryFromBase64String(value, bytes, out int written);
        bytes = bytes[..(ok ? written : 0)];
        return ok;
    }
}
