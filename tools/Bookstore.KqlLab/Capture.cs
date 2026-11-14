using System.IO.Compression;

namespace Bookstore.KqlLab;

// A local stand-in for the Application Insights ingestion endpoint: the Azure Monitor exporter POSTs its
// telemetry items to {IngestionEndpoint}/v2.1/track; we append them to a JSON Lines file instead of storing them.
public static class Capture
{
    public static async Task RunAsync(string dataDir, string url)
    {
        Directory.CreateDirectory(dataDir);
        string file = Path.Combine(dataDir, "envelopes.jsonl");
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(url);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        WebApplication app = builder.Build();

        SemaphoreSlim gate = new(1, 1);
        long total = 0;
        app.MapPost("/v2.1/track", async (HttpRequest request) =>
        {
            Stream body = request.Headers.ContentEncoding.ToString().Contains("gzip", StringComparison.OrdinalIgnoreCase)
                ? new GZipStream(request.Body, CompressionMode.Decompress)
                : request.Body;
            using StreamReader reader = new(body);
            string text = await reader.ReadToEndAsync();
            string[] items = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            await gate.WaitAsync();
            try
            {
                await File.AppendAllLinesAsync(file, items);
                total += items.Length;
            }
            finally
            {
                gate.Release();
            }
            Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss} received {items.Length} items, total {total}");
            // The same answer shape as the real endpoint, so the exporter does not retry.
            return Results.Ok(new { itemsReceived = items.Length, itemsAccepted = items.Length, errors = Array.Empty<object>() });
        });

        Console.WriteLine($"Listening on {url}/v2.1/track, writing to {file}");
        await app.RunAsync();
    }
}
