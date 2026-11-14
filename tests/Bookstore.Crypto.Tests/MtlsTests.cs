using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Bookstore.Api;
using Bookstore.Crypto.Pki;
using Microsoft.AspNetCore.Builder;

namespace Bookstore.Crypto.Tests;

// Starts the real Bookstore.Api on Kestrel with TLS and a throwaway PKI in a temp folder
public sealed class MtlsTests : IAsyncLifetime
{
    private readonly string _dir = Directory.CreateTempSubdirectory("bookstore-mtls-").FullName;
    private readonly DevPki _pki = DevPki.Create(DateTimeOffset.UtcNow.AddMinutes(-10));
    private WebApplication? _app;
    private Uri _baseAddress = null!;

    public async ValueTask InitializeAsync()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Save("root-ca.crt.pem", _pki.Root.ExportCertificatePem());
        Save("services-ca.crt.pem", _pki.Intermediate.ExportCertificatePem());
        using X509Certificate2 api = _pki.IssueServer("api.bookstore.test", ["localhost"], now.AddMinutes(-5), now.AddDays(1));
        Save("api.crt.pem", $"{api.ExportCertificatePem()}\n{_pki.Intermediate.ExportCertificatePem()}");
        Save("api.key.pem", api.GetECDsaPrivateKey()!.ExportPkcs8PrivateKeyPem());

        int port = FreePort();
        _baseAddress = new Uri($"https://localhost:{port}");
        _app = ApiHost.Build(
        [
            $"--urls={_baseAddress}",
            $"--Pki:TrustAnchors={Path.Combine(_dir, "root-ca.crt.pem")}",
            $"--Pki:Intermediates={Path.Combine(_dir, "services-ca.crt.pem")}",
            $"--Kestrel:Certificates:Default:Path={Path.Combine(_dir, "api.crt.pem")}",
            $"--Kestrel:Certificates:Default:KeyPath={Path.Combine(_dir, "api.key.pem")}",
            "--Logging:LogLevel:Default=None",
        ]);
        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        _pki.Dispose();
        Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public async Task Ordering_service_can_reserve_stock()
    {
        using X509Certificate2 ordering = _pki.IssueClient("ordering", DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        using HttpResponseMessage response = await Reserve(ordering);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        StockReservation? body = await response.Content.ReadFromJsonAsync<StockReservation>(TestContext.Current.CancellationToken);
        Assert.Equal("ordering", body!.RequestedBy);
    }

    [Fact]
    public async Task Authenticated_service_without_permission_gets_403()
    {
        using X509Certificate2 shipping = _pki.IssueClient("shipping", DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        using HttpResponseMessage response = await Reserve(shipping);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_certificate_from_another_ca_gets_403()
    {
        using DevPki rogue = DevPki.Create(DateTimeOffset.UtcNow.AddMinutes(-10));
        using X509Certificate2 fake = rogue.IssueClient("ordering", DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        using HttpResponseMessage response = await Reserve(fake);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_without_a_client_certificate_is_cut_off()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() => Reserve(null));
    }

    private async Task<HttpResponseMessage> Reserve(X509Certificate2? clientCertificate)
    {
        using HttpClient http = new(ServiceCertificates.CreateMtlsHandler([_pki.Root], clientCertificate)) { BaseAddress = _baseAddress };
        return await http.PostAsJsonAsync("/internal/stock-reservations",
            new StockReservationRequest(Guid.CreateVersion7(), "9780441172719", 2), TestContext.Current.CancellationToken);
    }

    private void Save(string name, string pem) => File.WriteAllText(Path.Combine(_dir, name), pem + "\n");

    private static int FreePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
