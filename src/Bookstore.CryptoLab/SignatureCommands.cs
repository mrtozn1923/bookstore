using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bookstore.Crypto.Licensing;

namespace Bookstore.CryptoLab;

public static class SignatureCommands
{
    public static void Formats()
    {
        byte[] data = "PO-2026-11-0042"u8.ToArray();

        using RSA rsa = RSA.Create(3072);
        byte[] pss1 = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        byte[] pss2 = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        byte[] v15a = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        byte[] v15b = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        Console.WriteLine($"RSA-3072 PSS       : {pss1.Length} bytes, two signatures equal: {pss1.AsSpan().SequenceEqual(pss2)}");
        Console.WriteLine($"RSA-3072 PKCS#1 1.5: {v15a.Length} bytes, two signatures equal: {v15a.AsSpan().SequenceEqual(v15b)}");

        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        SortedSet<int> p1363Lengths = [], derLengths = [];
        for (int i = 0; i < 1000; i++)
        {
            p1363Lengths.Add(ecdsa.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation).Length);
            derLengths.Add(ecdsa.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence).Length);
        }

        Console.WriteLine($"ECDSA P-256, 1000 signatures: IEEE P1363 lengths [{string.Join(", ", p1363Lengths)}], DER lengths [{string.Join(", ", derLengths)}]");

        // A signature in one format does not verify as the other
        byte[] ieee = ecdsa.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        Console.WriteLine($"P1363 signature verified as DER: {ecdsa.VerifyData(data, ieee, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)}");
    }

    public static void PublicKeyCannotSign()
    {
        using ECDsa issuer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa shipped = ECDsa.Create();
        shipped.ImportFromPem(issuer.ExportSubjectPublicKeyInfoPem());

        byte[] data = "license"u8.ToArray();
        byte[] signature = issuer.SignData(data, HashAlgorithmName.SHA256);
        Console.WriteLine($"verify with the shipped public key: {shipped.VerifyData(data, signature, HashAlgorithmName.SHA256)}");
        try
        {
            shipped.SignData(data, HashAlgorithmName.SHA256);
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"sign with the shipped public key: {ex.GetType().Name}");
        }
    }

    public static void LicenseDemo()
    {
        using ECDsa key2026 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa key2027 = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // The application ships only public keys, looked up by keyId
        Dictionary<string, ECDsa> shipped = new()
        {
            ["bookstore-license-2026"] = PublicOnly(key2026),
            ["bookstore-license-2027"] = PublicOnly(key2027),
        };

        LicenseClaims claims = new(
            LicenseId: "LIC-2026-0042",
            InstitutionId: Guid.Parse("0197f001-0000-7000-8000-00000000a001"),
            InstitutionName: "Example Secondary School",
            MaxUnits: 3,
            MaxUsers: 50,
            IssuedAt: new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.Zero),
            ExpiresAt: new DateTimeOffset(2027, 9, 1, 0, 0, 0, TimeSpan.Zero),
            Features: ["catalog", "purchase-approval"]);

        string license = new LicenseIssuer(key2026, "bookstore-license-2026").Issue(claims);
        string[] parts = license.Split('.');
        Console.WriteLine($"license file: {license.Length} chars");
        Console.WriteLine($"  {parts[0]}.{parts[1]}.{parts[2][..24]}...{parts[2][^8..]}.{parts[3][..12]}...");
        Console.WriteLine($"  payload: {Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[2]))[..90]}...");

        LicenseVerifier today = new(shipped, new FixedTimeProvider(new DateTimeOffset(2026, 12, 1, 9, 0, 0, TimeSpan.Zero)));
        Show("as issued", today.Verify(license));

        string json = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[2]));
        string moreUsers = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(json.Replace("\"maxUsers\":50", "\"maxUsers\":500")));
        Show("maxUsers 50 -> 500", today.Verify($"{parts[0]}.{parts[1]}.{moreUsers}.{parts[3]}"));

        // Same claims, serialized again: indented JSON, different bytes
        LicenseClaims parsed = JsonSerializer.Deserialize<LicenseClaims>(json, JsonSerializerOptions.Web)!;
        string reserialized = Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(parsed, new JsonSerializerOptions(JsonSerializerOptions.Web) { WriteIndented = true }));
        Show("same claims re-serialized", today.Verify($"{parts[0]}.{parts[1]}.{reserialized}.{parts[3]}"));

        Show("keyId switched to 2027", today.Verify($"{parts[0]}.bookstore-license-2027.{parts[2]}.{parts[3]}"));
        Show("keyId unknown", today.Verify($"{parts[0]}.bookstore-license-2030.{parts[2]}.{parts[3]}"));

        LicenseVerifier nextYear = new(shipped, new FixedTimeProvider(new DateTimeOffset(2027, 10, 1, 9, 0, 0, TimeSpan.Zero)));
        Show("checked in October 2027", nextYear.Verify(license));

        string renewed = new LicenseIssuer(key2027, "bookstore-license-2027").Issue(claims with
        {
            LicenseId = "LIC-2027-0042",
            IssuedAt = new DateTimeOffset(2027, 9, 1, 0, 0, 0, TimeSpan.Zero),
            ExpiresAt = new DateTimeOffset(2028, 9, 1, 0, 0, 0, TimeSpan.Zero),
        });
        Show("renewed with the 2027 key", nextYear.Verify(renewed));

        static void Show(string label, LicenseCheck check) =>
            Console.WriteLine($"{label,-27}: {check.Status}{(check.Claims is null ? "" : $" ({check.Claims.LicenseId}, {check.Claims.MaxUsers} users)")}");
    }

    private static ECDsa PublicOnly(ECDsa key)
    {
        ECDsa publicKey = ECDsa.Create();
        publicKey.ImportSubjectPublicKeyInfo(key.ExportSubjectPublicKeyInfo(), out _);
        return publicKey;
    }
}
