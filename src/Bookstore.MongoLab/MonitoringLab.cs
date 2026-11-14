using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Bookstore.MongoLab;

public static class MonitoringLab
{
    // The driver (3.7+) creates spans on the "MongoDB.Driver" ActivitySource; any listener can collect them
    public static async Task TraceAsync(IServiceProvider services)
    {
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == MongoTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => Console.WriteLine(
                $"{activity.DisplayName,-24} {activity.Duration.TotalMilliseconds,6:0.0} ms  " +
                string.Join(", ", activity.Tags.Where(t => t.Key != "db.mongodb.lsid").Select(t => $"{t.Key}={t.Value}")))
        };
        ActivitySource.AddActivityListener(listener);

        IMongoCollection<Book> books = services.GetRequiredService<IMongoCollection<Book>>();
        Book dune = await books.Find(b => b.Isbn == "9798900000268").FirstAsync();
        Console.WriteLine($"found {dune.Title}");
    }
}
