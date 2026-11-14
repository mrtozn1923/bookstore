using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Bookstore.Crypto.Licensing;

namespace Bookstore.Crypto.Tests;

public sealed class LicenseTests : IDisposable
{
    private readonly ECDsa _signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly ECDsa _publicKey = ECDsa.Create();

    private static readonly LicenseClaims Claims = new("LIC-TEST-1", Guid.CreateVersion7(), "Example Secondary School", 3, 50,
        new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 9, 1, 0, 0, 0, TimeSpan.Zero), ["catalog"]);

    public LicenseTests() => _publicKey.ImportSubjectPublicKeyInfo(_signingKey.ExportSubjectPublicKeyInfo(), out _);

    public void Dispose()
    {
        _signingKey.Dispose();
        _publicKey.Dispose();
    }

    private string Issue() => new LicenseIssuer(_signingKey, "test-2026").Issue(Claims);

    private LicenseCheck Verify(string license, int year = 2026, int month = 12) =>
        new LicenseVerifier(new Dictionary<string, ECDsa> { ["test-2026"] = _publicKey },
            new FixedTime(new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero))).Verify(license);

    [Fact]
    public void Issued_license_is_valid_and_carries_its_claims()
    {
        LicenseCheck check = Verify(Issue());
        Assert.Equal(LicenseStatus.Valid, check.Status);
        Assert.Equal(50, check.Claims!.MaxUsers);
    }

    [Fact]
    public void Edited_payload_is_rejected()
    {
        string[] parts = Issue().Split('.');
        string json = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[2])).Replace("\"maxUsers\":50", "\"maxUsers\":500");
        parts[2] = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(json));
        Assert.Equal(LicenseStatus.BadSignature, Verify(string.Join('.', parts)).Status);
    }

    [Fact]
    public void Signature_from_another_private_key_is_rejected()
    {
        using ECDsa attacker = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string forged = new LicenseIssuer(attacker, "test-2026").Issue(Claims with { MaxUsers = 5000 });
        Assert.Equal(LicenseStatus.BadSignature, Verify(forged).Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("BSL1.test-2026.only-three-parts")]
    [InlineData("BSL2.test-2026.e30.AAAA")]
    public void Malformed_text_is_rejected(string license) =>
        Assert.Equal(LicenseStatus.Malformed, Verify(license).Status);

    [Fact]
    public void Unknown_key_id_is_rejected() =>
        Assert.Equal(LicenseStatus.UnknownKey, Verify(Issue().Replace("test-2026", "test-2030")).Status);

    [Fact]
    public void Expiry_is_checked_after_the_signature() =>
        Assert.Equal(LicenseStatus.Expired, Verify(Issue(), year: 2027, month: 10).Status);

    [Fact]
    public void Public_key_alone_cannot_issue_licenses() =>
        Assert.ThrowsAny<CryptographicException>(() => new LicenseIssuer(_publicKey, "test-2026").Issue(Claims));

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
