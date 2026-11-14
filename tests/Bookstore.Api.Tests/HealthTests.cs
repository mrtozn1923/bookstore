using System.Net;

namespace Bookstore.Api.Tests;

public sealed class HealthTests(BookstoreApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health")]
    public async Task Health_endpoints_report_healthy_when_the_database_is_reachable(string path)
    {
        HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
