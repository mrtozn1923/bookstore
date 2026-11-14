using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Webhooks;

// Standard Webhooks, symmetric scheme: HMAC-SHA256 over "id.timestamp.body", sent as "v1,<base64>".
public static class WebhookSignature
{
    public const string SecretPrefix = "whsec_";

    public static string NewSecret() => SecretPrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    // One "v1,..." per secret, space separated: during rotation both the old and the new secret sign.
    public static string Sign(string messageId, long timestamp, ReadOnlySpan<byte> body, IEnumerable<string> secrets)
    {
        var signatures = new List<string>();
        foreach (var secret in secrets)
        {
            signatures.Add("v1," + Convert.ToBase64String(ComputeHmac(secret, messageId, timestamp, body)));
        }

        return string.Join(' ', signatures);
    }

    // Receiving side: any "v1," signature in the header may match any of our active secrets (rotation).
    public static WebhookVerification Verify(string? messageId, string? timestampHeader, string? signatureHeader,
        ReadOnlySpan<byte> body, IEnumerable<string> secrets, DateTimeOffset now, TimeSpan tolerance)
    {
        if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(signatureHeader)
            || !long.TryParse(timestampHeader, out var timestamp))
        {
            return WebhookVerification.MissingHeaders;
        }

        // Replay protection: a captured request is only usable within the tolerance window.
        var sent = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if (sent < now - tolerance || sent > now + tolerance)
        {
            return WebhookVerification.StaleTimestamp;
        }

        foreach (var secret in secrets)
        {
            var expected = ComputeHmac(secret, messageId, timestamp, body);
            foreach (var candidate in signatureHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!candidate.StartsWith("v1,", StringComparison.Ordinal))
                {
                    continue;
                }

                var provided = new byte[expected.Length];
                // Constant-time comparison: no early exit that leaks how many bytes matched.
                if (Convert.TryFromBase64String(candidate[3..], provided, out var written)
                    && written == expected.Length
                    && CryptographicOperations.FixedTimeEquals(provided, expected))
                {
                    return WebhookVerification.Valid;
                }
            }
        }

        return WebhookVerification.BadSignature;
    }

    public static byte[] ComputeHmac(string secret, string messageId, long timestamp, ReadOnlySpan<byte> body)
    {
        var key = Convert.FromBase64String(secret.StartsWith(SecretPrefix, StringComparison.Ordinal) ? secret[SecretPrefix.Length..] : secret);
        var prefix = Encoding.UTF8.GetBytes($"{messageId}.{timestamp}.");
        var signed = new byte[prefix.Length + body.Length];
        prefix.CopyTo(signed, 0);
        body.CopyTo(signed.AsSpan(prefix.Length));
        return HMACSHA256.HashData(key, signed);
    }
}

public enum WebhookVerification
{
    Valid,
    MissingHeaders,
    StaleTimestamp,
    BadSignature
}
