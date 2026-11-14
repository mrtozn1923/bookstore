using System.Security.Cryptography.X509Certificates;
using Bookstore.Crypto.Pki;

namespace Bookstore.Crypto.Tests;

public sealed class ServiceCertificateValidatorTests : IDisposable
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private readonly DevPki _pki = DevPki.Create(Now.AddDays(-90));
    private readonly DevPki _otherPki = DevPki.Create(Now.AddDays(-90));

    public void Dispose()
    {
        _pki.Dispose();
        _otherPki.Dispose();
    }

    private ServiceCertificateValidator Validator => new([_pki.Root], [_pki.Intermediate]);

    private X509Certificate2 Server(DevPki pki, int fromDays = 0, int toDays = 30) =>
        pki.IssueServer("api.bookstore.test", ["api.bookstore.test"], Now.AddDays(fromDays).AddMinutes(-5), Now.AddDays(toDays));

    [Fact]
    public void Certificate_from_our_chain_with_the_right_name_and_usage_is_valid()
    {
        using X509Certificate2 api = Server(_pki);
        CertificateCheck check = Validator.Validate(api, Eku.ServerAuth, "api.bookstore.test");
        Assert.True(check.IsValid, string.Join(", ", check.Problems));
        Assert.Equal(["api.bookstore.test", "Bookstore Dev Services CA", "Bookstore Dev Root CA"], check.Chain);
    }

    [Fact]
    public void Expired_certificate_is_rejected()
    {
        using X509Certificate2 expired = Server(_pki, fromDays: -60, toDays: -30);
        Assert.Contains("NotTimeValid", Validator.Validate(expired, Eku.ServerAuth, "api.bookstore.test").Problems);
    }

    [Fact]
    public void Client_certificate_cannot_act_as_a_server()
    {
        using X509Certificate2 client = _pki.IssueClient("ordering", Now.AddMinutes(-5), Now.AddDays(30));
        Assert.Contains("NotValidForUsage", Validator.Validate(client, Eku.ServerAuth, "ordering.bookstore.test").Problems);
    }

    [Fact]
    public void Certificate_from_another_root_is_rejected_even_with_its_intermediate()
    {
        using X509Certificate2 foreign = Server(_otherPki);
        CertificateCheck check = new ServiceCertificateValidator([_pki.Root], [_otherPki.Intermediate])
            .Validate(foreign, Eku.ServerAuth, "api.bookstore.test");
        Assert.False(check.IsValid);
    }

    [Fact]
    public void Valid_chain_with_the_wrong_host_name_is_rejected()
    {
        using X509Certificate2 api = Server(_pki);
        CertificateCheck check = Validator.Validate(api, Eku.ServerAuth, "admin.bookstore.test");
        Assert.Equal(["NameMismatch(admin.bookstore.test)"], check.Problems);
    }
}
