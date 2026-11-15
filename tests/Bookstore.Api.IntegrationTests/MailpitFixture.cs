using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

[assembly: AssemblyFixture(typeof(Bookstore.Api.IntegrationTests.MailpitFixture))]

namespace Bookstore.Api.IntegrationTests;

public sealed record MailpitAddress(string Name, string Address);

public sealed record MailpitMessage(string ID, string Subject, List<MailpitAddress> To, string Snippet);

public sealed record MailpitSearchResult(int Total, List<MailpitMessage> Messages);

public sealed class MailpitFixture : IAsyncLifetime
{
    private const int SmtpPort = 1025;
    private const int HttpPort = 8025;

    private readonly IContainer container = new ContainerBuilder("axllent/mailpit:v1.33.1")
        .WithPortBinding(SmtpPort, assignRandomHostPort: true)
        .WithPortBinding(HttpPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(HttpPort).ForPath("/readyz")))
        .Build();

    private HttpClient api = new();

    public string SmtpHost => container.Hostname;

    public int SmtpMappedPort => container.GetMappedPublicPort(SmtpPort);

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
        api = new HttpClient { BaseAddress = new Uri($"http://{container.Hostname}:{container.GetMappedPublicPort(HttpPort)}/") };
    }

    public async Task<List<MailpitMessage>> MessagesToAsync(string address)
    {
        MailpitSearchResult? result = await api.GetFromJsonAsync<MailpitSearchResult>(
            $"api/v1/search?query={Uri.EscapeDataString($"to:\"{address}\"")}");
        return result!.Messages;
    }

    public async ValueTask DisposeAsync()
    {
        api.Dispose();
        await container.DisposeAsync();
    }
}
