using System.Net;
using System.Net.Http.Json;

namespace Bookstore.Api.Tests;

public sealed class InfoTests(BookstoreApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Liveness_does_not_need_the_database_checks()
    {
        HttpResponseMessage response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_is_healthy_when_the_database_answers()
    {
        HttpResponseMessage response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Info_reports_version_environment_and_provider()
    {
        InfoResponse? info = await client.GetFromJsonAsync<InfoResponse>("/api/info", TestContext.Current.CancellationToken);

        Assert.NotNull(info);
        Assert.Equal("SqlServer", info.Database);
        Assert.False(string.IsNullOrEmpty(info.Version));
        Assert.False(string.IsNullOrEmpty(info.Pod));
    }

    private sealed record InfoResponse(string Version, string Environment, string Pod, string Database);
}
