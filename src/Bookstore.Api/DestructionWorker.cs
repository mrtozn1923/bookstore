using Bookstore.Privacy.Retention;

namespace Bookstore.Api;

// Runs the periodic destruction once at startup and then every 24 hours.
// The regulation caps the interval at six months; daily keeps the backlog small.
public sealed class DestructionWorker(IServiceScopeFactory scopes, ILogger<DestructionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        do
        {
            using IServiceScope scope = scopes.CreateScope();
            DestructionRun run = await scope.ServiceProvider.GetRequiredService<PeriodicDestructionJob>().RunAsync(stoppingToken);
            logger.LogInformation("Destruction run: {ErasureRequests} erasure requests, {Sessions} sessions deleted, {Orders} orders anonymised",
                run.ErasureRequests, run.SessionsDeleted, run.OrdersAnonymised);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
