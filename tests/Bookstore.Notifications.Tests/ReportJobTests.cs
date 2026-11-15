using System.Net;
using System.Net.Http.Json;
using Bookstore.Notifications;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bookstore.Notifications.Tests;

public sealed class ReportJobTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private sealed record JobStatus(Guid Id, string State, string? FilePath);

    [Fact]
    public async Task Monthly_report_is_accepted_then_completed_and_announced_by_email()
    {
        FakeEmailSender email = new();
        HttpClient client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Reports:SimulatedRenderTime", "00:00:00.200");
            builder.UseSetting("Notifications:FileStore:RootPath", Path.Combine(Path.GetTempPath(), "bookstore-test-" + Guid.NewGuid().ToString("N")));
            builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<IEmailSender>(email)));
        }).CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;

        HttpResponseMessage accepted = await client.PostAsJsonAsync("/api/reports/monthly-sales", new { month = "2026-10", notifyEmail = "selin@example.com" }, ct);
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);

        JobStatus? status = null;
        for (int i = 0; i < 50 && status?.State != "Completed"; i++)
        {
            await Task.Delay(100, ct);
            status = await client.GetFromJsonAsync<JobStatus>(accepted.Headers.Location, ct);
        }

        Assert.Equal("Completed", status?.State);
        EmailMessage sent = Assert.Single(email.Sent);
        Assert.Equal("selin@example.com", sent.To.Address);
        Assert.Contains($"/api/reports/{status!.Id}/file", sent.TextBody);
    }
}
