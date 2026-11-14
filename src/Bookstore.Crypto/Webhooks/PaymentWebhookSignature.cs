using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Crypto.Webhooks;

public enum WebhookVerdict
{
    Valid,
    MalformedHeader,
    StaleTimestamp,
    BadSignature,
}

// Header format: "t=<unix seconds>,v1=<hex HMAC-SHA256>[,v1=<hex>...]"
// Signed bytes: "<t>." followed by the raw request body, exactly as received
public static class PaymentWebhookSignature
{
    public static string CreateHeader(ReadOnlySpan<byte> secret, long timestamp, ReadOnlySpan<byte> body)
    {
        byte[] mac = ComputeMac(secret, timestamp, body);
        return $"t={timestamp},v1={Convert.ToHexStringLower(mac)}";
    }

    internal static byte[] ComputeMac(ReadOnlySpan<byte> secret, long timestamp, ReadOnlySpan<byte> body)
    {
        byte[] prefix = Encoding.ASCII.GetBytes(timestamp.ToString(CultureInfo.InvariantCulture) + ".");
        byte[] signed = new byte[prefix.Length + body.Length];
        prefix.CopyTo(signed, 0);
        body.CopyTo(signed.AsSpan(prefix.Length));
        return HMACSHA256.HashData(secret, signed);
    }
}

public sealed class PaymentWebhookVerifier(IReadOnlyList<byte[]> secrets, TimeProvider time, TimeSpan tolerance)
{
    public WebhookVerdict Verify(string? header, ReadOnlySpan<byte> rawBody)
    {
        if (!TryParse(header, out long timestamp, out List<byte[]> candidates))
        {
            return WebhookVerdict.MalformedHeader;
        }

        DateTimeOffset sentAt = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if ((time.GetUtcNow() - sentAt).Duration() > tolerance)
        {
            return WebhookVerdict.StaleTimestamp;
        }

        // Current secret and, during rotation, the previous one
        foreach (byte[] secret in secrets)
        {
            byte[] expected = PaymentWebhookSignature.ComputeMac(secret, timestamp, rawBody);
            foreach (byte[] candidate in candidates)
            {
                if (Tags.AreEqual(expected, candidate))
                {
                    return WebhookVerdict.Valid;
                }
            }
        }

        return WebhookVerdict.BadSignature;
    }

    private static bool TryParse(string? header, out long timestamp, out List<byte[]> signatures)
    {
        timestamp = 0;
        signatures = [];
        if (string.IsNullOrEmpty(header))
        {
            return false;
        }

        foreach (string part in header.Split(',', StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("t=", StringComparison.Ordinal))
            {
                if (!long.TryParse(part.AsSpan(2), NumberStyles.None, CultureInfo.InvariantCulture, out timestamp))
                {
                    return false;
                }
            }
            else if (part.StartsWith("v1=", StringComparison.Ordinal) && part.Length == 3 + 2 * HMACSHA256.HashSizeInBytes)
            {
                byte[] mac = new byte[HMACSHA256.HashSizeInBytes];
                if (Convert.FromHexString(part.AsSpan(3), mac, out _, out _) == OperationStatus.Done)
                {
                    signatures.Add(mac);
                }
            }
        }

        return timestamp > 0 && signatures.Count > 0;
    }
}
