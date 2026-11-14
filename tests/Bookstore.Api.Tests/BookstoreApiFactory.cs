using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bookstore.Data;
using Bookstore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: AssemblyFixture(typeof(Bookstore.Api.Tests.BookstoreApiFactory))]

namespace Bookstore.Api.Tests;

// One API host and one fresh database for the whole test run.
// The database is the development one with a "_Tests" suffix: BookstoreDb → BookstoreDb_Tests.
public sealed class BookstoreApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Ada = "ada@example.com";        // Customer
    public const string Grace = "grace@example.com";    // StoreStaff

    private readonly string connectionString = TestConnectionString();
    private readonly Dictionary<string, string> tokens = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:BookstoreDb", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "false");
    }

    public async ValueTask InitializeAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        // Demo users and passwords come from appsettings.Development.json.
        tokens[Ada] = await LoginAsync(Ada, "Bookstore-Demo-1");
        tokens[Grace] = await LoginAsync(Grace, "Bookstore-Demo-2");
    }

    // A client that sends the demo user's bearer token with every request.
    public HttpClient CreateClientFor(string email)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens[email]);
        return client;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        HttpResponseMessage response = await CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        TokenResponse? token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return token!.AccessToken;
    }

    private static string TestConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        string baseConnection = configuration.GetConnectionString("BookstoreDb") is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException("Connection string 'BookstoreDb' is missing (see README.md).");

        SqlConnectionStringBuilder csb = new(baseConnection);
        csb.InitialCatalog += "_Tests";
        return csb.ConnectionString;
    }
}
