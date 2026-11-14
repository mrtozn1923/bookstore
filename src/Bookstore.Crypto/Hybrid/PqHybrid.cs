using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Crypto.Hybrid;

// Hybrid key establishment: ML-KEM-768 and ephemeral ECDH P-256. Both shared secrets go into HKDF,
// so the data stays protected as long as either algorithm holds. AES-256-GCM for the data.
public static class PqHybrid
{
    public const string Algorithm = "MLKEM768+ECDH-P256.HKDF-SHA256.A256GCM";
    private const int Version = 2;
    private static readonly MLKemAlgorithm Kem = MLKemAlgorithm.MLKem768;

    public static HybridEnvelope Encrypt(MLKem recipientKem, ECDiffieHellman recipientEcdh, string keyId, ReadOnlySpan<byte> plaintext)
    {
        if (recipientKem.Algorithm != Kem)
        {
            throw new CryptographicException($"Expected {Kem.Name}, got {recipientKem.Algorithm.Name}.");
        }

        recipientKem.Encapsulate(out byte[] kemCiphertext, out byte[] kemSecret);
        using ECDiffieHellman ephemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        byte[] ephemeralPublic = ephemeral.ExportSubjectPublicKeyInfo();
        byte[] ecdhSecret = ephemeral.DeriveRawSecretAgreement(recipientEcdh.PublicKey);

        // Encapsulated key = ML-KEM ciphertext (fixed size) | ephemeral ECDH public key
        byte[] encapsulated = [.. kemCiphertext, .. ephemeralPublic];
        byte[] key = DeriveKey(kemSecret, ecdhSecret, encapsulated, keyId);
        try
        {
            return Aead.Seal(Version, Algorithm, keyId, encapsulated, key, plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(kemSecret);
            CryptographicOperations.ZeroMemory(ecdhSecret);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public static byte[] Decrypt(MLKem recipientKem, ECDiffieHellman recipientEcdh, HybridEnvelope envelope)
    {
        if (envelope.Version != Version || envelope.Algorithm != Algorithm)
        {
            throw new CryptographicException($"Unsupported envelope: v{envelope.Version} {envelope.Algorithm}");
        }

        byte[] encapsulated = Base64Url.DecodeFromChars(envelope.EncapsulatedKey);
        if (encapsulated.Length <= Kem.CiphertextSizeInBytes)
        {
            throw new CryptographicException("Encapsulated key is too short.");
        }

        byte[] kemSecret = recipientKem.Decapsulate(encapsulated[..Kem.CiphertextSizeInBytes]);
        using ECDiffieHellman ephemeral = ECDiffieHellman.Create();
        ephemeral.ImportSubjectPublicKeyInfo(encapsulated.AsSpan(Kem.CiphertextSizeInBytes), out _);
        if (ephemeral.ExportParameters(false).Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
        {
            throw new CryptographicException("The ephemeral key is not on P-256.");
        }

        byte[] ecdhSecret = recipientEcdh.DeriveRawSecretAgreement(ephemeral.PublicKey);
        byte[] key = DeriveKey(kemSecret, ecdhSecret, encapsulated, envelope.KeyId);
        try
        {
            return Aead.Open(envelope, key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(kemSecret);
            CryptographicOperations.ZeroMemory(ecdhSecret);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    // Both secrets in, both ciphertexts as salt: an attacker must break ML-KEM and ECDH to learn the key
    private static byte[] DeriveKey(byte[] kemSecret, byte[] ecdhSecret, byte[] encapsulated, string keyId) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, [.. kemSecret, .. ecdhSecret], 32, salt: encapsulated,
            info: Encoding.UTF8.GetBytes($"bookstore/purchase-order/v2|{Algorithm}|{keyId}"));
}
