using System.Net.Http.Headers;

namespace Bookstore.AppHost.Tests;

public sealed class CatalogFixture() : BookstoreAppFixture("redis", "messaging", "ordering", "inventory", "web", "web-installer")
{
    // Redis stays stopped, but its address would still be the DCP proxy, which accepts and never answers.
    // An empty connection string switches the catalog to its in-memory output cache.
    protected override void Configure(IDistributedApplicationTestingBuilder builder) =>
        builder.CreateResourceBuilder<ProjectResource>("catalog").WithEnvironment("ConnectionStrings__redis", "");

    protected override async Task WaitUntilReadyAsync(DistributedApplication app, CancellationToken ct)
    {
        await app.ResourceNotifications.WaitForResourceHealthyAsync("catalog", ct);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("keycloak", ct);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("gateway", ct);
    }
}

public sealed class CatalogTests(CatalogFixture fixture) : IClassFixture<CatalogFixture>
{
    private sealed record TokenResponse(string Access_token);

    private sealed record BookDto(string Isbn, string Title, string Author, decimal Price, string Currency);

    [Fact]
    public async Task AddedBookAppearsInTheList()
    {
        using HttpClient gateway = fixture.App.CreateHttpClient("gateway");
        gateway.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetBackofficeTokenAsync());

        using HttpResponseMessage created = await gateway.PostAsJsonAsync("/api/books/",
            new { isbn = "9780140449136", title = "Crime and Punishment", author = "Fyodor Dostoevsky", price = 310.00m });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        List<BookDto>? page = await gateway.GetFromJsonAsync<List<BookDto>>("/api/books?page=1");
        Assert.Contains(page!, b => b.Isbn == "9780140449136");
    }

    [Fact]
    public async Task AddingABookWithoutATokenIsRejected()
    {
        using HttpClient gateway = fixture.App.CreateHttpClient("gateway");

        using HttpResponseMessage response = await gateway.PostAsJsonAsync("/api/books/",
            new { isbn = "9780140268867", title = "The Odyssey", author = "Homer", price = 280.00m });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> GetBackofficeTokenAsync()
    {
        using HttpClient keycloak = fixture.App.CreateHttpClient("keycloak", "http");
        using HttpResponseMessage response = await keycloak.PostAsync("/realms/bookstore/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "bookstore-backoffice",
                ["client_secret"] = fixture.BackofficeSecret
            }));
        response.EnsureSuccessStatusCode();
        TokenResponse? token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return token!.Access_token;
    }
}
