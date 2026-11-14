using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;

// Same number format on every machine.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// Usage: dotnet run --project samples/Bookstore.Net11Lab -- compression|eviction|dns
var demo = args.FirstOrDefault() ?? "";
switch (demo)
{
    case "compression":
        await CompressionDemo();
        break;
    case "eviction":
        await EvictionDemo();
        break;
    case "dns":
        await DnsDemo();
        break;
    default:
        Console.WriteLine("Demos: compression, eviction, dns");
        break;
}

// A local server that reports what arrived: encoding, bytes on the wire, bytes after decompression.
static async Task<WebApplication> StartServerAsync()
{
    var builder = WebApplication.CreateSlimBuilder();
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.Logging.ClearProviders();
    var app = builder.Build();
    app.MapPost("/api/institutional-orders/import", async (HttpRequest request) =>
    {
        using var raw = new MemoryStream();
        await request.Body.CopyToAsync(raw);
        raw.Position = 0;
        var encoding = request.Headers.ContentEncoding.ToString();
        Stream decoded = encoding switch
        {
            "gzip" => new GZipStream(raw, CompressionMode.Decompress),
            "br" => new BrotliStream(raw, CompressionMode.Decompress),
            "zstd" => new ZstandardStream(raw, CompressionMode.Decompress),
            _ => raw
        };
        using var plain = new MemoryStream();
        await decoded.CopyToAsync(plain);
        return Results.Ok(new { encoding = encoding.Length == 0 ? "none" : encoding, wireBytes = raw.Length, jsonBytes = plain.Length });
    });
    app.MapGet("/ping", (HttpContext context) => Results.Ok(new { connection = context.Connection.Id }));
    await app.StartAsync();
    return app;
}

static async Task CompressionDemo()
{
    await using var app = await StartServerAsync();
    using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

    // An institutional order: 30 lines of the same few classics, typical for a school.
    string[] titles = ["Dune", "Emma", "The Hobbit", "Middlemarch", "Persuasion"];
    var order = new
    {
        institutionId = "school-0042",
        lines = Enumerable.Range(1, 30).Select(i => new
        {
            isbn = $"978000000{i:D4}",
            title = titles[i % titles.Length],
            quantity = 20 + i,
            unitPrice = 153.00m,
            currency = "TRY"
        })
    };

    HttpContent[] contents =
    [
        JsonContent.Create(order),
        new GZipCompressedContent(JsonContent.Create(order), CompressionLevel.Optimal),
        new BrotliCompressedContent(JsonContent.Create(order), CompressionLevel.Optimal),
        new ZstandardCompressedContent(JsonContent.Create(order), CompressionLevel.Optimal)
    ];
    foreach (var content in contents)
    {
        using var response = await client.PostAsync("api/institutional-orders/import", content);
        Console.WriteLine($"{content.GetType().Name,-28} Content-Encoding: {string.Join(",", content.Headers.ContentEncoding),-5} -> {await response.Content.ReadAsStringAsync()}");
    }
}

static async Task EvictionDemo()
{
    await using var app = await StartServerAsync();
    var handler = new SocketsHttpHandler
    {
        // .NET 11: decide per connection instead of a fixed PooledConnectionLifetime.
        // Experimental in 11.0 (SYSLIB5008): the shape may still change.
#pragma warning disable SYSLIB5008
        ShouldEvictConnection = (context, _) =>
        {
            var evict = context.Age > TimeSpan.FromSeconds(1);
            Console.WriteLine($"  check connection {context.ConnectionId}, HTTP/{context.HttpVersion}, age {context.Age.TotalMilliseconds:0} ms -> evict: {evict}");
            return Task.FromResult(evict);
        },
#pragma warning restore SYSLIB5008
        // .NET 11: limit the first burst on a new HTTP/2 connection until the server's SETTINGS arrive
        InitialHttp2MaxConcurrentStreams = 20
    };
    using var client = new HttpClient(handler) { BaseAddress = new Uri(app.Urls.First()) };
    var clock = System.Diagnostics.Stopwatch.StartNew();
    for (var i = 1; i <= 3; i++)
    {
        Console.WriteLine($"[{clock.Elapsed.TotalSeconds:0.0} s] request {i}: {await client.GetStringAsync("ping")}");
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}

static async Task DnsDemo()
{
    // .NET 11: typed DNS records with TTLs, e.g. checking an institution's e-mail domain before sending to it.
    foreach (var domain in new[] { "example.org", "no-such-school.example" })
    {
        var mx = await Dns.ResolveMxAsync(domain, CancellationToken.None);
        Console.WriteLine($"MX {domain}: {mx.ResponseCode}, records: {mx.Records.Count}, negative cache TTL: {mx.NegativeCacheTtl}");
        foreach (var record in mx.Records)
        {
            Console.WriteLine($"  preference {record.Preference}, exchange '{record.Exchange}', ttl {record.Ttl}");
        }
    }
}
