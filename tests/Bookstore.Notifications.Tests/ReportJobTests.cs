using System.Net;
using System.Net.Http.Json;
using Bookstore.Notifications;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bookstore.Notifications.Tests;

// The whole API with Hangfire on a temporary SQLite file; only the email sender is replaced
public sealed class ReportJobTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private sealed record Accepted(string JobId);
    private sealed record JobState(string JobId, string State, string Job);

    [Fact]
    public async Task Monthly_report_job_runs_twice_but_announces_the_month_once()
    {
        string temp = Path.Combine(Path.GetTempPath(), "bookstore-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        FakeEmailSender email = new();
        HttpClient client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Bookstore", $"Data Source={Path.Combine(temp, "bookstore.db")}");
            builder.UseSetting("ConnectionStrings:Jobs", Path.Combine(temp, "jobs.db"));
            builder.UseSetting("Notifications:FileStore:RootPath", Path.Combine(temp, "files"));
            builder.UseSetting("Notifications:Outbox:PollInterval", "00:00:00.200");
            builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<IEmailSender>(email)));
        }).CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;

        for (int run = 0; run < 2; run++)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/reports/monthly-sales", new { month = "2026-10" }, ct);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            JobState? state = null;
            for (int i = 0; i < 100 && state?.State != "Succeeded"; i++)
            {
                await Task.Delay(100, ct);
                state = await client.GetFromJsonAsync<JobState>(response.Headers.Location, ct);
            }

            Assert.Equal(("Succeeded", "MonthlySalesReportJob.RunAsync"), (state?.State, state?.Job));
        }

        await Task.Delay(1000, ct);
        EmailMessage sent = Assert.Single(email.Sent);
        Assert.Equal(("reports@example.com", "Monthly sales report 2026-10 is ready"), (sent.To.Address, sent.Subject));
    }
}
