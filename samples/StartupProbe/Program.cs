using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;

// Starts a published Bookstore.Api several times and measures: time to the first successful response,
// memory (working set) when idle and after load, and requests per second for two endpoints.
if (args is not [string runsText, string executable, ..])
{
    Console.Error.WriteLine("usage: StartupProbe <runs> <executable> [args...]");
    return 2;
}

const string Url = "http://127.0.0.1:52506";
const string Filter = "/books?filter=price%20%3C%20300%20and%20author%20%3D%20%22Jane%20Austen%22";
int runs = int.Parse(runsText, CultureInfo.InvariantCulture);
using HttpClient client = new() { BaseAddress = new Uri(Url) };

List<double> startups = [];
for (int run = 1; run <= runs; run++)
{
    using Process app = Start(executable, args[2..]);
    double ms = await WaitForFirstResponse(client);
    startups.Add(ms);

    if (run == runs)
    {
        long idle = WorkingSetMb(app);
        double booksRps = await Load(client, "/books");
        double filterRps = await Load(client, Filter);
        long loaded = WorkingSetMb(app);
        startups.Sort();
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"startup median {startups[startups.Count / 2],6:F0} ms | idle {idle,4} MB | /books {booksRps,7:F0} rps | filter {filterRps,6:F0} rps | after load {loaded,4} MB"));
    }

    app.Kill(entireProcessTree: true);
    await app.WaitForExitAsync();
}
return 0;

static Process Start(string executable, string[] arguments)
{
    ProcessStartInfo info = new(executable, arguments) { RedirectStandardOutput = true, RedirectStandardError = true };
    info.Environment["ASPNETCORE_URLS"] = Url;
    info.Environment["Logging__LogLevel__Default"] = "Warning";
    return Process.Start(info)!;
}

static async Task<double> WaitForFirstResponse(HttpClient client)
{
    long start = Stopwatch.GetTimestamp();
    while (true)
    {
        try
        {
            using HttpResponseMessage response = await client.GetAsync("/books");
            if (response.IsSuccessStatusCode)
                return Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        catch (HttpRequestException e) when (e.InnerException is SocketException)
        {
            // the server is not listening yet
        }
        await Task.Delay(2);
    }
}

static async Task<double> Load(HttpClient client, string path)
{
    // warm-up, then a fixed 5 second window with 32 concurrent clients
    await Hammer(client, path, TimeSpan.FromSeconds(2));
    long start = Stopwatch.GetTimestamp();
    long count = await Hammer(client, path, TimeSpan.FromSeconds(5));
    return count / Stopwatch.GetElapsedTime(start).TotalSeconds;
}

static async Task<long> Hammer(HttpClient client, string path, TimeSpan duration)
{
    long count = 0;
    DateTime end = DateTime.UtcNow + duration;
    await Task.WhenAll(Enumerable.Range(0, 32).Select(async _ =>
    {
        while (DateTime.UtcNow < end)
        {
            using HttpResponseMessage response = await client.GetAsync(path);
            response.EnsureSuccessStatusCode();
            Interlocked.Increment(ref count);
        }
    }));
    return count;
}

static long WorkingSetMb(Process process)
{
    process.Refresh();
    return process.WorkingSet64 / (1024 * 1024);
}
