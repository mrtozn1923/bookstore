using System.Security.Cryptography;

namespace Bookstore.Crypto.Hybrid;

// The recipient's keys, one per algorithm family it accepts
public sealed record RecipientKeys(ECDiffieHellman Ecdh, MLKem? Kem = null, RSA? Rsa = null);

// Crypto agility: the algorithm travels with the data, the reader dispatches on an allow-list
public static class EnvelopeReader
{
    public static readonly IReadOnlySet<string> Accepted = new HashSet<string>
    {
        EcdhHybrid.Algorithm,   // legacy: read only, until all stored orders are re-encrypted
        RsaHybrid.Algorithm,    // legacy: read only
        PqHybrid.Algorithm,     // current
    };

    public static byte[] Open(RecipientKeys keys, HybridEnvelope envelope)
    {
        if (!Accepted.Contains(envelope.Algorithm))
        {
            throw new CryptographicException($"Algorithm not accepted: {envelope.Algorithm}");
        }

        return envelope.Algorithm switch
        {
            PqHybrid.Algorithm => PqHybrid.Decrypt(keys.Kem ?? throw new CryptographicException("No ML-KEM key."), keys.Ecdh, envelope),
            EcdhHybrid.Algorithm => EcdhHybrid.Decrypt(keys.Ecdh, envelope),
            RsaHybrid.Algorithm => RsaHybrid.Decrypt(keys.Rsa ?? throw new CryptographicException("No RSA key."), envelope),
            _ => throw new CryptographicException($"Algorithm not accepted: {envelope.Algorithm}"),
        };
    }
}
