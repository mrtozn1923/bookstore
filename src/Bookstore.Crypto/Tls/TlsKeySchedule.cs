using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Crypto.Tls;

public sealed record TlsTrafficKeys(byte[] Key, byte[] Iv);

// TLS 1.3 key schedule pieces (RFC 8446 section 7). For learning and diagnostics only:
// SslStream does all of this inside the platform TLS library.
public static class TlsKeySchedule
{
    // HKDF-Expand-Label(Secret, Label, Context, Length): info = length(2) | "tls13 " + label (1+n) | context (1+m)
    public static byte[] ExpandLabel(HashAlgorithmName hash, ReadOnlySpan<byte> secret, string label, ReadOnlySpan<byte> context, int length)
    {
        byte[] fullLabel = Encoding.ASCII.GetBytes("tls13 " + label);
        byte[] info = [(byte)(length >> 8), (byte)length, (byte)fullLabel.Length, .. fullLabel, (byte)context.Length, .. context];
        byte[] output = new byte[length];
        HKDF.Expand(hash, secret, output, info);
        return output;
    }

    // Derive-Secret(Secret, Label, Messages) with the transcript hash already computed
    public static byte[] DeriveSecret(HashAlgorithmName hash, ReadOnlySpan<byte> secret, string label, ReadOnlySpan<byte> transcriptHash) =>
        ExpandLabel(hash, secret, label, transcriptHash, transcriptHash.Length);

    // [sender]_write_key and [sender]_write_iv from a traffic secret
    public static TlsTrafficKeys TrafficKeys(HashAlgorithmName hash, ReadOnlySpan<byte> trafficSecret, int keyLength) =>
        new(ExpandLabel(hash, trafficSecret, "key", [], keyLength), ExpandLabel(hash, trafficSecret, "iv", [], 12));
}
