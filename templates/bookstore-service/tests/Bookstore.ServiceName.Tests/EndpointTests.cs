using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bookstore.ServiceName.Tests;

public sealed class EndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetRoot_Always_ReturnsServiceName()
    {
        HttpClient client = factory.CreateClient();

        string body = await client.GetStringAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal("Bookstore.ServiceName", body);
    }
#if (HealthChecks)

    [Fact]
    public async Task GetHealth_Always_ReturnsOk()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
#endif
}
