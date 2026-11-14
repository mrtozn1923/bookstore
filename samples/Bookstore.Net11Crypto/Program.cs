using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable SYSLIB5006, SYSLIB5009 // CompositeMLKem and Hpke are [Experimental] in .NET 11
Console.WriteLine($"{RuntimeInformation.FrameworkDescription}, {RuntimeInformation.OSDescription}");

// 1) X25519 key agreement: a BCL class at last
Console.WriteLine($"-- X25519DiffieHellman.IsSupported: {X25519DiffieHellman.IsSupported}");
using (X25519DiffieHellman ordering = X25519DiffieHellman.GenerateKey())
using (X25519DiffieHellman supplier = X25519DiffieHellman.GenerateKey())
{
    byte[] fromOrdering = ordering.DeriveRawSecretAgreement(supplier.ExportPublicKey());
    byte[] fromSupplier = supplier.DeriveRawSecretAgreement(ordering.ExportPublicKey());
    Console.WriteLine($"public key {ordering.ExportPublicKey().Length} B (SPKI {ordering.ExportSubjectPublicKeyInfo().Length} B), " +
        $"shared secret {fromOrdering.Length} B, same on both sides: {fromOrdering.AsSpan().SequenceEqual(fromSupplier)}");
}

// 2) AES Key Wrap without padding (RFC 3394), checked against the RFC's test vector 4.1
using (Aes kek = Aes.Create())
{
    kek.Key = Convert.FromHexString("000102030405060708090A0B0C0D0E0F");
    byte[] wrapped = kek.EncryptKeyWrap(Convert.FromHexString("00112233445566778899AABBCCDDEEFF"));
    Console.WriteLine($"-- AES-KW: {Convert.ToHexString(wrapped)} (RFC 3394 4.1: {Convert.ToHexString(wrapped) == "1FA68B0A8112B447AEF34BD8FB5A7B829D3E862371D2CFE5"})");
    byte[] odd = RandomNumberGenerator.GetBytes(19);
    Console.WriteLine($"19-byte key, padded (RFC 5649): {kek.EncryptKeyWrapPadded(odd).Length} B");
    try
    {
        kek.EncryptKeyWrap(odd);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"19-byte key, unpadded: {ex.GetType().Name}");
    }
}

// 3) Webhook check from the second post, now a one-liner
byte[] secret = Encoding.ASCII.GetBytes("dev-only-payment-webhook-secret-v2");
byte[] body = """{"paymentId":"pay_0042","status":"captured"}"""u8.ToArray();
byte[] mac = HMACSHA256.HashData(secret, body);
byte[] changed = [.. body[..^1], (byte)'!'];
Console.WriteLine($"-- HMACSHA256.Verify: {HMACSHA256.Verify(secret, body, mac)}, one byte changed: {HMACSHA256.Verify(secret, changed, mac)}");
Console.WriteLine($"FixedTimeEquals(span, byte): [0x42] vs 0x42 {CryptographicOperations.FixedTimeEquals([0x42], 0x42)}, " +
    $"[0x42, 0x42] vs 0x42 {CryptographicOperations.FixedTimeEquals([0x42, 0x42], 0x42)}, [0x41] vs 0x42 {CryptographicOperations.FixedTimeEquals([0x41], 0x42)}");

// 4) HPKE (RFC 9180): a standard sealed box for the purchase order of the fourth and eighth posts
byte[] order = """{"purchaseOrder":"PO-2026-11-0042","lines":[{"isbn":"9780441172719","quantity":120}]}"""u8.ToArray();
byte[] info = "bookstore/purchase-order/v3"u8.ToArray();
byte[] noAssociatedData = [];
foreach (HpkeKem kem in new[] { HpkeKem.DHKEM_X25519_HKDF_SHA256, HpkeKem.MLKEM_768, HpkeKem.MLKEM768_P256 })
{
    HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_256_GCM);
    if (!Hpke.IsSupported(suite))
    {
        Console.WriteLine($"-- HPKE {kem}: not supported here");
        continue;
    }

    using Hpke supplierKey = Hpke.GenerateKey(suite);
    using Hpke bookstoreSide = Hpke.ImportEncapsulationKey(suite, supplierKey.ExportEncapsulationKey());
    bookstoreSide.Seal(order, out byte[] encapsulated, out byte[] ciphertext, noAssociatedData, info);
    byte[] opened = supplierKey.Open(encapsulated, ciphertext, noAssociatedData, info);
    Console.WriteLine($"-- HPKE {kem}: encapsulated {encapsulated.Length} B, ciphertext {ciphertext.Length} B for {order.Length} B, opened: {opened.AsSpan().SequenceEqual(order)}");
}

// 5) Composite ML-KEM: ML-KEM-768 and X25519 in one KEM
CompositeMLKemAlgorithm composite = CompositeMLKemAlgorithm.MLKem768WithX25519;
if (CompositeMLKem.IsAlgorithmSupported(composite))
{
    using CompositeMLKem key = CompositeMLKem.GenerateKey(composite);
    key.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);
    Console.WriteLine($"-- {composite.Name}: encapsulation key {key.ExportEncapsulationKey().Length} B, ciphertext {ciphertext.Length} B, " +
        $"secret {sharedSecret.Length} B, decapsulated equal: {key.Decapsulate(ciphertext).AsSpan().SequenceEqual(sharedSecret)}");
}
else
{
    Console.WriteLine($"-- {composite.Name}: not supported here");
}
