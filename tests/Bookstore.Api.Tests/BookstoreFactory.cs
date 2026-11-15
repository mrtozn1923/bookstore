using System.Security.Claims;
using System.Text.Encodings.Web;
using Azure.Storage.Blobs;
using Bookstore.Api.Uploads;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Tests;

// Needs Azurite and clamd: Storage__ConnectionString and ClamAv__Port come from the environment
public sealed class BookstoreFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Uploads:SasProtocol", "HttpsAndHttp");  // Azurite listens on plain HTTP locally
        builder.ConfigureTestServices(services => services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null));
    }

    public async ValueTask InitializeAsync()
    {
        BlobServiceClient blobs = Services.GetRequiredService<BlobServiceClient>();
        UploadOptions options = Services.GetRequiredService<IOptions<UploadOptions>>().Value;
        await blobs.GetBlobContainerClient(options.QuarantineContainer).CreateIfNotExistsAsync();
        await blobs.GetBlobContainerClient(options.ApprovedContainer).CreateIfNotExistsAsync();
    }

    public HttpClient ClientFor(string publisherId)
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestAuthHandler.Header, publisherId);
        return client;
    }
}

public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string Header = "X-Test-Publisher";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Header, out var publisher))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        ClaimsIdentity identity = new([new Claim("publisher_id", publisher.ToString())], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
