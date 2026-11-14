using Bookstore.Notifications;
using Bookstore.Ordering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WireMock.Server;

namespace Bookstore.Api.Tests;

// The real API, wired to in-process WireMock fakes instead of the external services.
public sealed class BookstoreApiFactory(
    WireMockServer payments,
    WireMockServer carrier,
    TimeProvider? timeProvider = null,
    IDictionary<string, string?>? settings = null) : WebApplicationFactory<CheckoutService> // any type from Bookstore.Api
{
    public FakeEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Payments:Provider:BaseUrl", payments.Url + "/");
        builder.UseSetting("Shipping:LegacyCarrier:BaseUrl", carrier.Url + "/");
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
            if (timeProvider is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(timeProvider);
            }
        });
    }
}

public sealed class FakeEmailSender : IEmailSender
{
    public List<string> Subjects { get; } = [];

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        lock (Subjects)
        {
            Subjects.Add(subject);
        }

        return Task.CompletedTask;
    }
}
