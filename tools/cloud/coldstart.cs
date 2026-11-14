// Measures a Bookstore.Api cold start in a container: docker run → app answers → first and warm /api/books.
// Run: dotnet run tools/cloud/coldstart.cs -- [cpus] [image]
// BOOKSTORE_DB: SQL Server connection string as seen from inside the container.
using System.Diagnostics;

string cpus = args.Length > 0 ? args[0] : "1";
string image = args.Length > 1 ? args[1] : "bookstore-api:latest";
string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? throw new InvalidOperationException("Set BOOKSTORE_DB to the SQL Server connection string.");
string container = Environment.GetEnvironmentVariable("COLDSTART_CONTAINER") ?? "bookstore-coldstart";
const int Port = 18080;

using HttpClient http = new() { BaseAddress = new Uri($"http://localhost:{Port}"), Timeout = TimeSpan.FromSeconds(10) };
await DockerAsync("rm", "-f", container);

Stopwatch clock = Stopwatch.StartNew();
await DockerAsync("run", "-d", "--name", container, "--cpus", cpus, "--memory", "1g", "-p", $"{Port}:8080",
    "-e", $"ConnectionStrings__BookstoreDb={connectionString}", image);
TimeSpan containerStarted = clock.Elapsed;

// A path that doesn't exist: a 404 means Kestrel answers, without touching the database
while (true)
{
    try
    {
        using HttpResponseMessage ping = await http.GetAsync("/ping");
        break;
    }
    catch (HttpRequestException)
    {
        await Task.Delay(20);
    }
}
TimeSpan appAnswers = clock.Elapsed;

TimeSpan first = await TimeRequestAsync();
TimeSpan firstTotal = clock.Elapsed;
List<double> warm = [];
for (int i = 0; i < 20; i++)
{
    warm.Add((await TimeRequestAsync()).TotalMilliseconds);
}
warm.Sort();

Console.WriteLine($"cpus={cpus}");
Console.WriteLine($"  docker run returned          {containerStarted.TotalMilliseconds,7:0} ms");
Console.WriteLine($"  app answers (404 on /ping)   {appAnswers.TotalMilliseconds,7:0} ms");
Console.WriteLine($"  first /api/books took        {first.TotalMilliseconds,7:0} ms  (done at {firstTotal.TotalMilliseconds:0} ms)");
Console.WriteLine($"  warm /api/books median/max   {warm[warm.Count / 2],7:0.0} / {warm[^1]:0.0} ms");

await DockerAsync("rm", "-f", container);

async Task<TimeSpan> TimeRequestAsync()
{
    Stopwatch sw = Stopwatch.StartNew();
    using HttpResponseMessage response = await http.GetAsync("/api/books?pageSize=5");
    response.EnsureSuccessStatusCode();
    await response.Content.ReadAsStringAsync();
    return sw.Elapsed;
}

static async Task DockerAsync(params string[] arguments)
{
    using Process process = Process.Start(new ProcessStartInfo("docker", arguments) { RedirectStandardOutput = true, RedirectStandardError = true })!;
    await process.StandardOutput.ReadToEndAsync();
    await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
}
