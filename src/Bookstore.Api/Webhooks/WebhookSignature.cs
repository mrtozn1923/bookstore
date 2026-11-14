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
