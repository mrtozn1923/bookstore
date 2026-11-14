using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.TryAddEventingSubscriber<StartupBanner>();

var apiService = builder.AddProject<Projects.Bookstore_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Bookstore_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddJavaScriptApp("admin", "../admin-web", "start")
    .WithHttpEndpoint(env: "PORT")
    .WithReference(apiService);

builder.Build().Run();

internal sealed class StartupBanner(ILogger<StartupBanner> logger) : IDistributedApplicationEventingSubscriber
{
    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        eventing.Subscribe<BeforeStartEvent>((@event, ct) =>
        {
            logger.LogInformation("Starting Bookstore with {Count} resources", @event.Model.Resources.Count);
            return Task.CompletedTask;
        });
        return Task.CompletedTask;
    }
}
