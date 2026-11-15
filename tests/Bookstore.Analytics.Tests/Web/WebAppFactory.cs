using Bookstore.Analytics.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Analytics.Tests.Web;

public sealed class WebAppFactory(string? measurementId = "G-TEST123") : WebApplicationFactory<Program>
{
    public FakeHandler Ga4 { get; } = new((_, _) => (System.Net.HttpStatusCode.NoContent, ""));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Analytics:MeasurementId", measurementId ?? "");
        builder.UseSetting("Analytics:ApiSecret", "fake-api-secret-for-tests");
        builder.ConfigureServices(services =>
            services.AddHttpClient<Bookstore.Analytics.Web.Analytics.Ga4MeasurementProtocolClient>()
                .ConfigurePrimaryHttpMessageHandler(() => Ga4));
    }
}
