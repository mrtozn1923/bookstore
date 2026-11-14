using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Aspire.Hosting;

public static class BookstoreResourceExtensions
{
    public static IResourceBuilder<MailpitResource> AddMailpit(this IDistributedApplicationBuilder builder, string name) =>
        builder.AddResource(new MailpitResource(name))
            .WithImage("axllent/mailpit", "v1.33")
            .WithImageRegistry("docker.io")
            .WithHttpEndpoint(targetPort: 8025, name: "http")
            .WithEndpoint(targetPort: 1025, name: MailpitResource.SmtpEndpointName, scheme: "smtp");

    // Dashboard and CLI command: empties Redis, enabled only while the resource is healthy
    public static IResourceBuilder<RedisResource> WithClearCacheCommand(this IResourceBuilder<RedisResource> redis) =>
        redis.WithCommand(
            name: "clear-cache",
            displayName: "Clear cache",
            executeCommand: async context =>
            {
                string? connectionString = await redis.Resource.GetConnectionStringAsync(context.CancellationToken);
                try
                {
                    await using ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync($"{connectionString},allowAdmin=true");
                    foreach (IServer server in connection.GetServers())
                    {
                        await server.FlushAllDatabasesAsync();
                    }
                }
                catch (RedisConnectionException)
                {
                    return CommandResults.Failure("Redis is not reachable.");
                }

                return CommandResults.Success();
            },
            commandOptions: new CommandOptions
            {
                IconName = "Delete",
                UpdateState = context => context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy
                    ? ResourceCommandState.Enabled
                    : ResourceCommandState.Disabled
            });
}
