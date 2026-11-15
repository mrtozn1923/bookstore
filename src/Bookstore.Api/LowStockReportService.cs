using Bookstore.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api;

public sealed class LowStockReportService(IServiceScopeFactory scopeFactory, TimeProvider clock, ILogger<LowStockReportService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(5), clock);
        do
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            BookstoreDbContext db = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
            int lowStock = await db.Books.CountAsync(b => b.Stock < 5, stoppingToken);
            logger.LogInformation("Low stock report: {Count} books below 5 copies", lowStock);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
