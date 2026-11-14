using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Bookstore.Crypto.Pki;

public static class ServiceCertificates
{
    // PEM certificate + key. Windows (Schannel) cannot use the ephemeral key that PEM loading creates,
    // so there the certificate is re-imported through PKCS#12, the same way Kestrel loads PEM files.
    public static X509Certificate2 LoadFromPem(string certificatePath, string keyPath)
    {
        X509Certificate2 certificate = X509Certificate2.CreateFromPemFile(certificatePath, keyPath);
        if (!OperatingSystem.IsWindows())
        {
            return certificate;
        }

        using (certificate)
        {
            return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pkcs12), null);
        }
    }

    public static X509Certificate2Collection LoadAll(string pemPath)
    {
        X509Certificate2Collection certificates = [];
        certificates.ImportFromPemFile(pemPath);
        return certificates;
    }

    // HttpClient handler for service-to-service calls: our root only, host name still checked, optional client certificate
    public static SocketsHttpHandler CreateMtlsHandler(X509Certificate2Collection trustAnchors, X509Certificate2? clientCertificate)
    {
        X509ChainPolicy policy = new()
        {
            TrustMode = X509ChainTrustMode.CustomRootTrust,
            RevocationMode = X509RevocationMode.NoCheck,
            DisableCertificateDownloads = true,
        };
        policy.CustomTrustStore.AddRange(trustAnchors);

        return new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                CertificateChainPolicy = policy,
                ClientCertificates = clientCertificate is null ? null : [clientCertificate],
            },
        };
    }
}
