using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Bookstore.Crypto.Pki;

public static class Eku
{
    public static readonly Oid ServerAuth = new("1.3.6.1.5.5.7.3.1", "Server Authentication");
    public static readonly Oid ClientAuth = new("1.3.6.1.5.5.7.3.2", "Client Authentication");
}

// A small, local-only PKI for development and tests: root CA -> intermediate CA -> service certificates.
// Production certificates come from your organisation's CA, never from this class.
public sealed class DevPki : IDisposable
{
    public const string ServiceDomain = "bookstore.test";

    private DevPki(X509Certificate2 root, X509Certificate2 intermediate)
    {
        Root = root;
        Intermediate = intermediate;
    }

    public X509Certificate2 Root { get; }
    public X509Certificate2 Intermediate { get; }

    public static DevPki Create(DateTimeOffset now)
    {
        using ECDsa rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        CertificateRequest rootRequest = new("CN=Bookstore Dev Root CA, O=Bookstore", rootKey, HashAlgorithmName.SHA384);
        AddCaExtensions(rootRequest, pathLength: 1);
        X509Certificate2 root = rootRequest.CreateSelfSigned(now.AddMinutes(-5), now.AddYears(10));

        using ECDsa intermediateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest intermediateRequest = new("CN=Bookstore Dev Services CA, O=Bookstore", intermediateKey, HashAlgorithmName.SHA256);
        AddCaExtensions(intermediateRequest, pathLength: 0);
        intermediateRequest.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(root, true, false));
        using X509Certificate2 intermediatePublic = intermediateRequest.Create(root, now.AddMinutes(-5), now.AddYears(3), NewSerialNumber());
        X509Certificate2 intermediate = intermediatePublic.CopyWithPrivateKey(intermediateKey);

        return new DevPki(root, intermediate);
    }

    public X509Certificate2 IssueServer(string commonName, string[] dnsNames, DateTimeOffset notBefore, DateTimeOffset notAfter) =>
        Issue(commonName, dnsNames, Eku.ServerAuth, notBefore, notAfter);

    // Service identity lives in the SAN: "<service>.bookstore.test"
    public X509Certificate2 IssueClient(string service, DateTimeOffset notBefore, DateTimeOffset notAfter) =>
        Issue($"bookstore-{service}", [$"{service}.{ServiceDomain}"], Eku.ClientAuth, notBefore, notAfter);

    public X509Certificate2 Issue(string commonName, string[] dnsNames, Oid eku, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest request = new($"CN={commonName}, O=Bookstore", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([eku], false));
        SubjectAlternativeNameBuilder san = new();
        foreach (string dnsName in dnsNames)
        {
            san.AddDnsName(dnsName);
        }

        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(Intermediate, true, false));

        using X509Certificate2 certificate = request.Create(Intermediate, notBefore, notAfter, NewSerialNumber());
        return certificate.CopyWithPrivateKey(key);
    }

    public void Dispose()
    {
        Root.Dispose();
        Intermediate.Dispose();
    }

    private static void AddCaExtensions(CertificateRequest request, int pathLength)
    {
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, pathLength, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
    }

    private static byte[] NewSerialNumber()
    {
        byte[] serial = RandomNumberGenerator.GetBytes(16);
        serial[0] &= 0x7F; // positive INTEGER
        return serial;
    }
}
