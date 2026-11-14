using System.Buffers.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Bookstore.Crypto.Hybrid;

namespace Bookstore.CryptoLab;

public static class AsymmetricCommands
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static byte[] PurchaseOrder => """
        {"purchaseOrderId":"PO-2026-11-0042","supplierId":"supplier-0042","lines":[
          {"isbn":"9780441013593","title":"Dune","quantity":120,"unitPrice":310.00},
          {"isbn":"9780141439587","title":"Emma","quantity":60,"unitPrice":145.00}]}
        """u8.ToArray();

    public static void KeySizes()
    {
        foreach (int bits in new[] { 2048, 3072 })
        {
            using RSA rsa = RSA.Create(bits);
            Console.WriteLine($"RSA-{bits,-5} public key {rsa.ExportSubjectPublicKeyInfo().Length,3} bytes, private key {rsa.ExportPkcs8PrivateKey().Length,4} bytes");
        }

        foreach (ECCurve curve in new[] { ECCurve.NamedCurves.nistP256, ECCurve.NamedCurves.nistP384 })
        {
            using ECDiffieHellman ecdh = ECDiffieHellman.Create(curve);
            Console.WriteLine($"{curve.Oid.FriendlyName,-9} public key {ecdh.ExportSubjectPublicKeyInfo().Length,3} bytes, private key {ecdh.ExportPkcs8PrivateKey().Length,4} bytes");
        }
    }

    public static void RsaLimit()
    {
        using RSA rsa = RSA.Create(2048);
        foreach (int size in new[] { 190, 191 })
        {
            try
            {
                byte[] ciphertext = rsa.Encrypt(new byte[size], RSAEncryptionPadding.OaepSHA256);
                Console.WriteLine($"{size} bytes -> {ciphertext.Length} bytes ciphertext");
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"{size} bytes -> {ex.GetType().Name}: {ex.Message}");
            }
        }

        // Same input, two encryptions: OAEP is randomized
        byte[] a = rsa.Encrypt("Dune"u8, RSAEncryptionPadding.OaepSHA256);
        byte[] b = rsa.Encrypt("Dune"u8, RSAEncryptionPadding.OaepSHA256);
        Console.WriteLine($"same plaintext, same ciphertext: {a.AsSpan().SequenceEqual(b)}");
    }

    public static void PurchaseOrderDemo()
    {
        string folder = Path.Combine(Path.GetTempPath(), "bookstore-po-demo");
        Directory.CreateDirectory(folder);
        string passphrase = "dev-only-supplier-key-passphrase";

        // Supplier side: generate a key pair once, send only the public key to Bookstore
        using (ECDiffieHellman supplierKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
        {
            File.WriteAllText(Path.Combine(folder, "supplier-0042.pub.pem"), supplierKey.ExportSubjectPublicKeyInfoPem());
            PbeParameters pbe = new(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, iterationCount: 600_000);
            File.WriteAllText(Path.Combine(folder, "supplier-0042.key.pem"), supplierKey.ExportEncryptedPkcs8PrivateKeyPem(passphrase, pbe));
        }

        foreach (string file in Directory.GetFiles(folder).Order())
        {
            string[] lines = File.ReadAllLines(file);
            Console.WriteLine($"{Path.GetFileName(file)}: {lines[0]} ({lines.Length} lines)");
        }

        // Bookstore side: encrypt the purchase order with the supplier's public key
        using ECDiffieHellman supplierPublic = ECDiffieHellman.Create();
        supplierPublic.ImportFromPem(File.ReadAllText(Path.Combine(folder, "supplier-0042.pub.pem")));
        HybridEnvelope envelope = EcdhHybrid.Encrypt(supplierPublic, "supplier-0042/2026", PurchaseOrder);
        string poFile = Path.Combine(folder, "PO-2026-11-0042.json.enc");
        File.WriteAllText(poFile, JsonSerializer.Serialize(envelope, Json));
        Console.WriteLine($"{Path.GetFileName(poFile)}: {new FileInfo(poFile).Length} bytes for a {PurchaseOrder.Length}-byte order");
        Console.WriteLine(Shorten(File.ReadAllText(poFile)));

        // Supplier side: open the private key with the passphrase and decrypt
        using ECDiffieHellman supplierPrivate = ECDiffieHellman.Create();
        supplierPrivate.ImportFromEncryptedPem(File.ReadAllText(Path.Combine(folder, "supplier-0042.key.pem")), passphrase);
        HybridEnvelope received = JsonSerializer.Deserialize<HybridEnvelope>(File.ReadAllText(poFile), Json)!;
        string order = Encoding.UTF8.GetString(EcdhHybrid.Decrypt(supplierPrivate, received));
        Console.WriteLine($"supplier reads: {order[..54]}...");

        Try("other supplier's key", () =>
        {
            using ECDiffieHellman other = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            EcdhHybrid.Decrypt(other, received);
        });
        Try("keyId changed in transit", () => EcdhHybrid.Decrypt(supplierPrivate, received with { KeyId = "supplier-0043/2026" }));
        Try("wrong passphrase", () =>
        {
            using ECDiffieHellman k = ECDiffieHellman.Create();
            k.ImportFromEncryptedPem(File.ReadAllText(Path.Combine(folder, "supplier-0042.key.pem")), "dev-only-wrong");
        });

        Directory.Delete(folder, recursive: true);
    }

    public static void RsaFromCertificate()
    {
        // The supplier sends a certificate instead of a bare public key (chain validation: a later post)
        using RSA supplierKey = RSA.Create(3072);
        CertificateRequest request = new("CN=supplier-0042.example.com", supplierKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
        string pem = certificate.ExportCertificatePem();

        using X509Certificate2 received = X509Certificate2.CreateFromPem(pem);
        using RSA publicKey = received.GetRSAPublicKey()!;
        Console.WriteLine($"{received.Subject}, has private key: {received.HasPrivateKey}, RSA-{publicKey.KeySize}");

        HybridEnvelope envelope = RsaHybrid.Encrypt(publicKey, "supplier-0042/2026-rsa", PurchaseOrder);
        Console.WriteLine($"{envelope.Algorithm}: encapsulated key {Base64Url.DecodeFromChars(envelope.EncapsulatedKey).Length} bytes");
        Console.WriteLine($"round trip: {RsaHybrid.Decrypt(supplierKey, envelope).AsSpan().SequenceEqual(PurchaseOrder)}");
    }

    private static string Shorten(string json) =>
        string.Join('\n', json.Split('\n').Select(line =>
        {
            int colon = line.IndexOf("\": \"", StringComparison.Ordinal);
            return colon > 0 && line.Length - colon > 40 ? line[..(colon + 24)] + "...\"," : line;
        }));

    private static void Try(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"{label}: succeeded (!)");
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"{label}: {ex.GetType().Name}");
        }
    }
}
