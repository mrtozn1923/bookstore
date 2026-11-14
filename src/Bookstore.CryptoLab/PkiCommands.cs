using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Bookstore.Crypto.Pki;

namespace Bookstore.CryptoLab;

public static class PkiCommands
{
    private const string Dir = "certs";

    // Writes a local dev PKI to ./certs (git-ignored): root, intermediate, api server cert, ordering/shipping client certs
    public static void Init()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using DevPki pki = DevPki.Create(now);
        Directory.CreateDirectory(Dir);
        File.WriteAllText(Path.Combine(Dir, "root-ca.crt.pem"), pki.Root.ExportCertificatePem() + "\n");
        File.WriteAllText(Path.Combine(Dir, "services-ca.crt.pem"), pki.Intermediate.ExportCertificatePem() + "\n");

        using X509Certificate2 api = pki.IssueServer("api.bookstore.test", ["api.bookstore.test", "localhost"], now.AddMinutes(-5), now.AddDays(30));
        Write("api", api, pki.Intermediate);
        foreach (string service in new[] { "ordering", "shipping" })
        {
            using X509Certificate2 client = pki.IssueClient(service, now.AddMinutes(-5), now.AddDays(30));
            Write(service, client, pki.Intermediate);
        }

        foreach (string file in Directory.GetFiles(Dir).Order())
        {
            Console.WriteLine($"{file,-28} {new FileInfo(file).Length,5} bytes");
        }
    }

    public static void Info()
    {
        X509Certificate2Collection chain = ServiceCertificates.LoadAll(Path.Combine(Dir, "api.crt.pem"));
        using X509Certificate2 cert = chain[0];
        Console.WriteLine($"Subject      : {cert.Subject}");
        Console.WriteLine($"Issuer       : {cert.Issuer}");
        Console.WriteLine($"Serial       : {cert.SerialNumber[..8]}...");
        Console.WriteLine($"Valid        : {cert.NotBefore.ToUniversalTime():yyyy-MM-dd HH:mm} .. {cert.NotAfter.ToUniversalTime():yyyy-MM-dd HH:mm} UTC");
        Console.WriteLine($"Key          : {cert.PublicKey.Oid.FriendlyName} {cert.GetECDsaPublicKey()!.KeySize} bits, signed with {cert.SignatureAlgorithm.FriendlyName}");
        foreach (X509Extension ext in cert.Extensions)
        {
            string value = ext switch
            {
                X509BasicConstraintsExtension bc => $"CA={bc.CertificateAuthority}",
                X509KeyUsageExtension ku => ku.KeyUsages.ToString(),
                X509EnhancedKeyUsageExtension eku => string.Join(", ", eku.EnhancedKeyUsages.Cast<Oid>().Select(o => o.FriendlyName ?? o.Value)),
                X509SubjectAlternativeNameExtension san => "DNS: " + string.Join(", ", san.EnumerateDnsNames()),
                _ => Convert.ToHexString(ext.RawData)[..16] + "...",
            };
            Console.WriteLine($"  {ext.Oid!.FriendlyName,-26}{(ext.Critical ? " (critical)" : ""),-11} {value}");
        }

        string sha1 = cert.Thumbprint;
        string sha256 = cert.GetCertHashString(HashAlgorithmName.SHA256);
        Console.WriteLine($"Thumbprint SHA-1  : {sha1}");
        Console.WriteLine($"Thumbprint SHA-256: {sha256}");

        Console.WriteLine($"api.crt.pem holds {chain.Count} certificates; FindByThumbprint(SHA256) -> {chain.FindByThumbprint(HashAlgorithmName.SHA256, sha256).Count} match");
    }

    public static void Chain()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using DevPki pki = DevPki.Create(now.AddDays(-90));
        using DevPki otherPki = DevPki.Create(now.AddDays(-90));
        ServiceCertificateValidator validator = new([pki.Root], [pki.Intermediate]);

        using X509Certificate2 api = pki.IssueServer("api.bookstore.test", ["api.bookstore.test"], now.AddMinutes(-5), now.AddDays(30));
        using X509Certificate2 expired = pki.IssueServer("api.bookstore.test", ["api.bookstore.test"], now.AddDays(-60), now.AddDays(-30));
        using X509Certificate2 client = pki.IssueClient("ordering", now.AddMinutes(-5), now.AddDays(30));
        using X509Certificate2 foreign = otherPki.IssueServer("api.bookstore.test", ["api.bookstore.test"], now.AddMinutes(-5), now.AddDays(30));

        Print("valid server certificate", validator.Validate(api, Eku.ServerAuth, "api.bookstore.test"));
        Print("expired", validator.Validate(expired, Eku.ServerAuth, "api.bookstore.test"));
        Print("client cert used as server", validator.Validate(client, Eku.ServerAuth, "ordering.bookstore.test"));
        Print("issued by another root", new ServiceCertificateValidator([pki.Root], [otherPki.Intermediate]).Validate(foreign, Eku.ServerAuth, "api.bookstore.test"));
        Print("intermediate not available", new ServiceCertificateValidator([pki.Root], []).Validate(api, Eku.ServerAuth, "api.bookstore.test"));
        Print("wrong host name", validator.Validate(api, Eku.ServerAuth, "admin.bookstore.test"));
        Print("revocation Online, no CRL", new ServiceCertificateValidator([pki.Root], [pki.Intermediate]) { RevocationMode = X509RevocationMode.Online }
            .Validate(api, Eku.ServerAuth, "api.bookstore.test"));
    }

    public static void ExportPfx()
    {
        using X509Certificate2 cert = ServiceCertificates.LoadFromPem(Path.Combine(Dir, "ordering.crt.pem"), Path.Combine(Dir, "ordering.key.pem"));
        string password = Environment.GetEnvironmentVariable("PFX_PASSWORD") ?? throw new InvalidOperationException("Set PFX_PASSWORD");

        File.WriteAllBytes(Path.Combine(Dir, "ordering-default.pfx"), cert.Export(X509ContentType.Pkcs12, password));
        File.WriteAllBytes(Path.Combine(Dir, "ordering-aes.pfx"), cert.ExportPkcs12(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, password));

        using X509Certificate2 loaded = X509CertificateLoader.LoadPkcs12FromFile(Path.Combine(Dir, "ordering-aes.pfx"), password);
        Console.WriteLine($"reloaded {loaded.GetNameInfo(X509NameType.DnsName, false)}, has private key: {loaded.HasPrivateKey}");
    }

    // Plays the Ordering service calling Bookstore.Api over mTLS
    public static async Task CallAsync(string[] args)
    {
        string baseUrl = args.Length > 1 ? args[1] : "https://localhost:7443";
        X509Certificate2Collection trustAnchors = ServiceCertificates.LoadAll(Path.Combine(Dir, "root-ca.crt.pem"));
        StockReservationBody body = new(Guid.Parse("0197f001-0000-7000-8000-000000000042"), "9780441172719", 2);

        foreach (string? service in new[] { "ordering", "shipping", null })
        {
            using X509Certificate2? certificate = service is null ? null
                : ServiceCertificates.LoadFromPem(Path.Combine(Dir, $"{service}.crt.pem"), Path.Combine(Dir, $"{service}.key.pem"));
            using HttpClient http = new(ServiceCertificates.CreateMtlsHandler(trustAnchors, certificate)) { BaseAddress = new Uri(baseUrl) };
            string label = service ?? "no client certificate";
            try
            {
                using HttpResponseMessage response = await http.PostAsJsonAsync("/internal/stock-reservations", body);
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"{label,-22}: {(int)response.StatusCode} {response.StatusCode} {Shorten(content)}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"{label,-22}: {ex.GetType().Name}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }

    private sealed record StockReservationBody(Guid OrderId, string Isbn, int Quantity);

    private static string Shorten(string s) => s.Length > 90 ? s[..90] + "..." : s;

    private static void Write(string name, X509Certificate2 certificate, X509Certificate2 intermediate)
    {
        // Full chain: leaf first, then the intermediate, so peers can build the path to the root.
        // ExportCertificatePem() has no trailing newline: without "\n" the two blocks run together.
        File.WriteAllText(Path.Combine(Dir, $"{name}.crt.pem"), $"{certificate.ExportCertificatePem()}\n{intermediate.ExportCertificatePem()}\n");
        File.WriteAllText(Path.Combine(Dir, $"{name}.key.pem"), certificate.GetECDsaPrivateKey()!.ExportPkcs8PrivateKeyPem() + "\n");
    }

    private static void Print(string label, CertificateCheck check) =>
        Console.WriteLine($"{label,-28}: {(check.IsValid ? "OK" : "REJECTED")}  [{string.Join(", ", check.Problems)}]  chain: {string.Join(" > ", check.Chain)}");
}
