using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class ProductionDemo
{
    // A throwaway CA and a server certificate for localhost, written to lab/tls (git-ignored, lab only)
    public static void MakeCertificates(string directory)
    {
        Directory.CreateDirectory(directory);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        using RSA caKey = RSA.Create(2048);
        CertificateRequest caRequest = new("CN=Bookstore Lab CA", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        using X509Certificate2 ca = caRequest.CreateSelfSigned(now.AddMinutes(-5), now.AddYears(1));

        using RSA serverKey = RSA.Create(2048);
        CertificateRequest serverRequest = new("CN=localhost", serverKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        SubjectAlternativeNameBuilder names = new();
        names.AddDnsName("localhost");
        names.AddIpAddress(IPAddress.Loopback);
        serverRequest.CertificateExtensions.Add(names.Build());
        serverRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], false));
        using X509Certificate2 server = serverRequest.Create(ca, now.AddMinutes(-5), now.AddDays(90), RandomNumberGenerator.GetBytes(8));

        File.WriteAllText(Path.Combine(directory, "ca.crt"), ca.ExportCertificatePem());
        File.WriteAllText(Path.Combine(directory, "redis.crt"), server.ExportCertificatePem());
        File.WriteAllText(Path.Combine(directory, "redis.key"), serverKey.ExportPkcs8PrivateKeyPem());
        Console.WriteLine($"wrote ca.crt, redis.crt, redis.key to {directory} (server certificate valid until {server.NotAfter:yyyy-MM-dd})");
    }

    // The same server three ways: plain TCP, TLS without trusting the lab CA, TLS trusting it
    public static async Task TlsConnectAsync(string caFile)
    {
        foreach ((string label, string connection, bool trust) in new[]
                 {
                     ("plain", "localhost:6379,connectTimeout=2000", false),
                     ("tls, CA not trusted", "localhost:6379,ssl=true,connectTimeout=2000", false),
                     ("tls, lab CA trusted", "localhost:6379,ssl=true,connectTimeout=2000", true)
                 })
        {
            ConfigurationOptions options = ConfigurationOptions.Parse(connection);
            if (trust)
            {
                options.TrustIssuer(caFile);                              // accept certificates signed by this CA only
            }
            try
            {
                await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(options);
                TimeSpan ping = await mux.GetDatabase().PingAsync();
                Console.WriteLine($"{label,-20}: connected, PING {ping.TotalMilliseconds:F1} ms");
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine($"{label,-20}: {ex.Message.Split('\n')[0]}");
            }
        }
    }

    // A 3-second script blocks the server; a client with a 1-second timeout reports what it saw
    public static async Task TimeoutMessageAsync(string connectionString)
    {
        await using ConnectionMultiplexer admin = await ConnectionMultiplexer.ConnectAsync(connectionString + ",allowAdmin=true,name=report-job");
        await using ConnectionMultiplexer api = await ConnectionMultiplexer.ConnectAsync(connectionString + ",asyncTimeout=1000,name=bookstore-api");
        await admin.GetServers()[0].ConfigSetAsync("latency-monitor-threshold", "100");
        await admin.GetServers()[0].SlowlogResetAsync();

        Task script = admin.GetDatabase().ScriptEvaluateAsync(
            "local n = 0 for i = 1, 1000000000 do n = n + 1 end return n");    // about 3 s of pure CPU, no writes
        await Task.Delay(100);
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            await api.GetDatabase().StringGetAsync("bookstore:prices:9780441013593");
            Console.WriteLine($"GET returned after {sw.ElapsedMilliseconds} ms");
        }
        catch (RedisTimeoutException ex)
        {
            Console.WriteLine($"after {sw.ElapsedMilliseconds} ms: {ex.Message}");
        }
        await script;
    }
}
