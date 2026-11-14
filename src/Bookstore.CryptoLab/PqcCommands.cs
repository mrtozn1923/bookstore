using System.Buffers.Binary;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bookstore.Crypto;
using Bookstore.Crypto.Hybrid;
using Bookstore.Crypto.Tls;

namespace Bookstore.CryptoLab;

#pragma warning disable SYSLIB5006 // MLDsa, SlhDsa and CompositeMLDsa are [Experimental] in .NET 10
public static class PqcCommands
{
    private static readonly byte[] Data = "BSL1.bookstore-license-2026.payload"u8.ToArray();

    public static void Sizes()
    {
        Console.WriteLine(CryptoCapabilities.DescribePlatform());
        Console.WriteLine($"{"algorithm",-26}{"public key",11}{"signature / ciphertext",24}");
        using (ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        {
            Row("ECDSA P-256 (SPKI)", ecdsa.ExportSubjectPublicKeyInfo().Length, ecdsa.SignData(Data, HashAlgorithmName.SHA256).Length);
        }

        using (RSA rsa = RSA.Create(3072))
        {
            Row("RSA-3072 (SPKI)", rsa.ExportSubjectPublicKeyInfo().Length, rsa.SignData(Data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss).Length);
        }

        if (!MLKem.IsSupported || !MLDsa.IsSupported || !SlhDsa.IsSupported)
        {
            Console.WriteLine($"ML-KEM: {MLKem.IsSupported}, ML-DSA: {MLDsa.IsSupported}, SLH-DSA: {SlhDsa.IsSupported} -> post-quantum algorithms need OpenSSL 3.5+ or Windows CNG with PQC");
            return;
        }

        foreach (MLKemAlgorithm algorithm in new[] { MLKemAlgorithm.MLKem512, MLKemAlgorithm.MLKem768, MLKemAlgorithm.MLKem1024 })
        {
            using MLKem recipient = MLKem.GenerateKey(algorithm);
            using MLKem sender = MLKem.ImportEncapsulationKey(algorithm, recipient.ExportEncapsulationKey());
            sender.Encapsulate(out byte[] ciphertext, out byte[] senderSecret);
            bool same = recipient.Decapsulate(ciphertext).AsSpan().SequenceEqual(senderSecret);
            Row(algorithm.Name, recipient.ExportEncapsulationKey().Length, ciphertext.Length, $"secret {senderSecret.Length} B, same on both sides: {same}");
        }

        foreach (MLDsaAlgorithm algorithm in new[] { MLDsaAlgorithm.MLDsa44, MLDsaAlgorithm.MLDsa65, MLDsaAlgorithm.MLDsa87 })
        {
            using MLDsa key = MLDsa.GenerateKey(algorithm);
            byte[] signature = key.SignData(Data);
            Row(algorithm.Name, key.ExportMLDsaPublicKey().Length, signature.Length, $"verify: {key.VerifyData(Data, signature)}");
        }

        foreach (SlhDsaAlgorithm algorithm in new[] { SlhDsaAlgorithm.SlhDsaSha2_128s, SlhDsaAlgorithm.SlhDsaSha2_128f })
        {
            using SlhDsa key = SlhDsa.GenerateKey(algorithm);
            byte[] signature = key.SignData(Data);
            Row(algorithm.Name, key.ExportSlhDsaPublicKey().Length, signature.Length, $"verify: {key.VerifyData(Data, signature)}");
        }

        CompositeMLDsaAlgorithm composite = CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256;
        using (CompositeMLDsa key = CompositeMLDsa.GenerateKey(composite))
        {
            byte[] signature = key.SignData(Data);
            byte[] tampered = [.. signature];
            tampered[^1] ^= 1; // flip a bit in the ECDSA half
            Row(composite.Name, key.ExportCompositeMLDsaPublicKey().Length, signature.Length,
                $"verify: {key.VerifyData(Data, signature)}, ECDSA part tampered: {key.VerifyData(Data, tampered)}");
        }
    }

    // Purchase orders for suppliers: the post-quantum hybrid envelope next to the classic one
    public static void Envelope()
    {
        byte[] order = Encoding.UTF8.GetBytes("""{"purchaseOrder":"PO-2026-11-0042","supplier":"supplier-0042","lines":[{"isbn":"9780441172719","title":"Dune","quantity":120}]}""");
        using MLKem supplierKem = MLKem.GenerateKey(MLKemAlgorithm.MLKem768);
        using ECDiffieHellman supplierEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        RecipientKeys supplier = new(supplierEcdh, supplierKem);

        // Bookstore only holds the supplier's public keys
        using MLKem kemPublic = MLKem.ImportEncapsulationKey(MLKemAlgorithm.MLKem768, supplierKem.ExportEncapsulationKey());
        using ECDiffieHellman ecdhPublic = ECDiffieHellman.Create();
        ecdhPublic.ImportSubjectPublicKeyInfo(supplierEcdh.ExportSubjectPublicKeyInfo(), out _);

        HybridEnvelope classic = EcdhHybrid.Encrypt(ecdhPublic, "supplier-0042/2026", order);
        HybridEnvelope hybrid = PqHybrid.Encrypt(kemPublic, ecdhPublic, "supplier-0042/2027-pq", order);
        foreach (HybridEnvelope envelope in new[] { classic, hybrid })
        {
            int size = JsonSerializer.SerializeToUtf8Bytes(envelope).Length;
            string text = Encoding.UTF8.GetString(EnvelopeReader.Open(supplier, envelope));
            Console.WriteLine($"{envelope.Algorithm,-40} {size,5} B JSON, decrypted: {text[..34]}...");
        }

        Try("hybrid relabelled as classic", () => EnvelopeReader.Open(supplier, hybrid with { Algorithm = EcdhHybrid.Algorithm }));
        Try("unknown algorithm", () => EnvelopeReader.Open(supplier, hybrid with { Algorithm = "MLKEM512.A128GCM" }));
        Try("ML-KEM part of the key replaced", () =>
        {
            using MLKem other = MLKem.GenerateKey(MLKemAlgorithm.MLKem768);
            EnvelopeReader.Open(supplier with { Kem = other }, hybrid);
        });
    }

    // Which key exchange groups does this platform's TLS offer and pick?
    public static async Task GroupsAsync()
    {
        Console.WriteLine(CryptoCapabilities.DescribePlatform());
        TlsCommands.WireTap tap = null!;
        await using TlsCommands.LoopbackTls tls = await TlsCommands.LoopbackTls.ConnectAsync(SslProtocols.None, SslProtocols.None, null,
            s => tap = new TlsCommands.WireTap(s));
        byte[] clientHello = TlsRecords.Parse(tap.Sent.ToArray())[0].Fragment;
        byte[] serverHello = TlsRecords.Parse(tap.Received.ToArray())[0].Fragment;

        Dictionary<int, byte[]> clientExtensions = Extensions(clientHello, isClientHello: true);
        List<string> supported = [];
        for (int i = 2; i < clientExtensions[10].Length; i += 2)
        {
            supported.Add(GroupName(BinaryPrimitives.ReadUInt16BigEndian(clientExtensions[10].AsSpan(i))));
        }

        List<string> shares = [];
        ReadOnlySpan<byte> keyShares = clientExtensions[51].AsSpan(2);
        while (keyShares.Length >= 4)
        {
            int length = BinaryPrimitives.ReadUInt16BigEndian(keyShares[2..]);
            shares.Add($"{GroupName(BinaryPrimitives.ReadUInt16BigEndian(keyShares))} ({length} B)");
            keyShares = keyShares[(4 + length)..];
        }

        byte[] serverShare = Extensions(serverHello, isClientHello: false)[51];
        Console.WriteLine($"ClientHello supported_groups: {string.Join(", ", supported)}");
        Console.WriteLine($"ClientHello key_share       : {string.Join(", ", shares)}");
        Console.WriteLine($"ServerHello key_share       : {GroupName(BinaryPrimitives.ReadUInt16BigEndian(serverShare))} ({serverShare.Length - 4} B)");
        Console.WriteLine($"negotiated                  : {tls.Client.SslProtocol}, {tls.Client.NegotiatedCipherSuite}");
    }

    // Handshake body -> extensions by type (RFC 8446 4.1.2 / 4.1.3)
    private static Dictionary<int, byte[]> Extensions(byte[] handshake, bool isClientHello)
    {
        int p = 4 + 2 + 32;                 // type + length, legacy_version, random
        p += 1 + handshake[p];              // legacy_session_id
        if (isClientHello)
        {
            p += 2 + BinaryPrimitives.ReadUInt16BigEndian(handshake.AsSpan(p)); // cipher_suites
            p += 1 + handshake[p];                                             // compression_methods
        }
        else
        {
            p += 2 + 1;                                                        // cipher_suite, compression_method
        }

        int end = p + 2 + BinaryPrimitives.ReadUInt16BigEndian(handshake.AsSpan(p));
        Dictionary<int, byte[]> extensions = [];
        for (p += 2; p + 4 <= end;)
        {
            int type = BinaryPrimitives.ReadUInt16BigEndian(handshake.AsSpan(p));
            int length = BinaryPrimitives.ReadUInt16BigEndian(handshake.AsSpan(p + 2));
            extensions[type] = handshake[(p + 4)..(p + 4 + length)];
            p += 4 + length;
        }

        return extensions;
    }

    // IANA "TLS Supported Groups" registry
    private static string GroupName(int id) => id switch
    {
        23 => "secp256r1", 24 => "secp384r1", 25 => "secp521r1", 29 => "x25519", 30 => "x448",
        256 => "ffdhe2048", 257 => "ffdhe3072", 258 => "ffdhe4096", 259 => "ffdhe6144", 260 => "ffdhe8192",
        512 => "MLKEM512", 513 => "MLKEM768", 514 => "MLKEM1024",
        4587 => "SecP256r1MLKEM768", 4588 => "X25519MLKEM768", 4589 => "SecP384r1MLKEM1024",
        _ => $"0x{id:x4}",
    };

    private static void Row(string name, int publicKey, int output, string note = "") =>
        Console.WriteLine($"{name,-26}{publicKey,9} B{output,14} B   {note}");

    private static void Try(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"{label}: succeeded (!)");
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"{label}: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
#pragma warning restore SYSLIB5006
