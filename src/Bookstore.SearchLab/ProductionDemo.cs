using System.Security.Cryptography.X509Certificates;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;

namespace Bookstore.SearchLab;

// Post 12: checks against a production-like cluster.
public static class ProductionDemo
{
    // ca-fingerprint <http_ca.crt>: the value for Elasticsearch:CertificateFingerprint, without openssl.
    public static void CaFingerprint(string[] args)
    {
        using X509Certificate2 ca = X509Certificate2.CreateFromPem(File.ReadAllText(args[0]));
        Console.WriteLine($"Subject: {ca.Subject}");
        Console.WriteLine($"SHA-256: {ca.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256)}");
    }

    // tls-check: one request over HTTPS with the settings from the environment variables.
    public static async Task TlsCheckAsync()
    {
        ElasticsearchClient client = LabClient.Create();
        Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse response = await client.Indices.ExistsAsync(SearchClientFactory.BooksIndex);
        Console.WriteLine(response.ApiCallDetails.HasSuccessfulStatusCode || response.ApiCallDetails.HttpStatusCode == 404
            ? $"{LabClient.Options.Url}: HTTP {response.ApiCallDetails.HttpStatusCode}, books exists: {response.Exists}"
            : $"{LabClient.Options.Url}: failed, {response.ApiCallDetails.OriginalException?.GetType().Name}: {response.ApiCallDetails.OriginalException?.Message} {response.ApiCallDetails.OriginalException?.InnerException?.Message}");
    }
}
