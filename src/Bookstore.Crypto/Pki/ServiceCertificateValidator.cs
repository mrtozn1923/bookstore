using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Bookstore.Crypto.Pki;

public sealed record CertificateCheck(bool IsValid, IReadOnlyList<string> Problems, IReadOnlyList<string> Chain);

// Validates a peer certificate against our own trust anchors only (not the machine's root store).
public sealed class ServiceCertificateValidator(X509Certificate2Collection trustAnchors, X509Certificate2Collection intermediates)
{
    public X509RevocationMode RevocationMode { get; init; } = X509RevocationMode.NoCheck;

    public CertificateCheck Validate(X509Certificate2 certificate, Oid requiredUsage, string? expectedDnsName, DateTime? at = null)
    {
        using X509Chain chain = new();
        X509ChainPolicy policy = chain.ChainPolicy;
        policy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        policy.CustomTrustStore.AddRange(trustAnchors);
        policy.ExtraStore.AddRange(intermediates);
        policy.ApplicationPolicy.Add(requiredUsage);
        policy.RevocationMode = RevocationMode;
        policy.DisableCertificateDownloads = true;
        if (at is not null)
        {
            policy.VerificationTime = at.Value;
        }

        bool chainOk = chain.Build(certificate);
        List<string> problems = [.. chain.ChainStatus.Select(s => s.Status.ToString()).Distinct()];

        // The chain says nothing about names: the host name is a separate check
        if (expectedDnsName is not null && !certificate.MatchesHostname(expectedDnsName))
        {
            problems.Add($"NameMismatch({expectedDnsName})");
        }

        List<string> elements = [.. chain.ChainElements.Select(e => e.Certificate.GetNameInfo(X509NameType.SimpleName, false))];
        return new CertificateCheck(chainOk && problems.Count == 0, problems, elements);
    }
}
