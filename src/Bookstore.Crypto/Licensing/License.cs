using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bookstore.Crypto.Licensing;

public sealed record LicenseClaims(
    string LicenseId,
    Guid InstitutionId,
    string InstitutionName,
    int MaxUnits,
    int MaxUsers,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string[] Features);

public enum LicenseStatus
{
    Valid,
    Malformed,
    UnknownKey,
    BadSignature,
    NotYetValid,
    Expired,
}

public sealed record LicenseCheck(LicenseStatus Status, LicenseClaims? Claims = null);

// License text: "BSL1.<keyId>.<payload>.<signature>", payload and signature in Base64Url.
// The signature covers "BSL1.<keyId>.<payload>" exactly as written: nothing is re-serialized.
public static class LicenseFormat
{
    public const string Prefix = "BSL1";
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal static byte[] SigningInput(string keyId, string payload) =>
        Encoding.ASCII.GetBytes($"{Prefix}.{keyId}.{payload}");
}

public sealed class LicenseIssuer(ECDsa signingKey, string keyId)
{
    public string Issue(LicenseClaims claims)
    {
        string payload = Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(claims, LicenseFormat.Json));
        byte[] signature = signingKey.SignData(
            LicenseFormat.SigningInput(keyId, payload), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"{LicenseFormat.Prefix}.{keyId}.{payload}.{Base64Url.EncodeToString(signature)}";
    }
}

public sealed class LicenseVerifier(IReadOnlyDictionary<string, ECDsa> publicKeys, TimeProvider time)
{
    public LicenseCheck Verify(string licenseText)
    {
        string[] parts = licenseText.Trim().Split('.');
        if (parts.Length != 4 || parts[0] != LicenseFormat.Prefix)
        {
            return new(LicenseStatus.Malformed);
        }

        if (!publicKeys.TryGetValue(parts[1], out ECDsa? publicKey))
        {
            return new(LicenseStatus.UnknownKey);
        }

        byte[] signature;
        try
        {
            signature = Base64Url.DecodeFromChars(parts[3]);
        }
        catch (FormatException)
        {
            return new(LicenseStatus.Malformed);
        }

        // First the signature over the bytes as received, only then the JSON
        if (!publicKey.VerifyData(LicenseFormat.SigningInput(parts[1], parts[2]), signature,
                HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
        {
            return new(LicenseStatus.BadSignature);
        }

        LicenseClaims? claims = JsonSerializer.Deserialize<LicenseClaims>(Base64Url.DecodeFromChars(parts[2]), LicenseFormat.Json);
        if (claims is null)
        {
            return new(LicenseStatus.Malformed);
        }

        DateTimeOffset now = time.GetUtcNow();
        if (now < claims.IssuedAt)
        {
            return new(LicenseStatus.NotYetValid, claims);
        }

        return now >= claims.ExpiresAt ? new(LicenseStatus.Expired, claims) : new(LicenseStatus.Valid, claims);
    }
}
