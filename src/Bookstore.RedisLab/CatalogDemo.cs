using System.Diagnostics;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.DataTypes;
using NRedisStack.Search.Literals.Enums;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class CatalogDemo
{
    private static readonly string[] Categories = ["classic", "science-fiction", "dystopia", "romance", "fantasy", "history", "children"];
    private static readonly string[] Words =
        ["çöl", "gezegen", "savaş", "evlilik", "aile", "yolculuk", "ejderha", "imparatorluk", "devlet", "gelecek",
         "kasaba", "deniz", "dağ", "şehir", "aşk", "hazine", "kral", "bilim", "zaman", "ada"];

    // 100,000 generated books: memory as JSON vs hash, index build time and size, query latency
    public static async Task ScaleAsync(string connectionString)
    {
        const int count = 100_000;
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        await server.FlushDatabaseAsync();
        Random random = new(42);

        var books = Enumerable.Range(0, count).Select(i =>
        {
            string[] cats = [.. Categories.OrderBy(_ => random.Next()).Take(random.Next(1, 4))];
            return new
            {
                isbn = $"978{i:D10}", title = $"Book {i}", author = $"Author {i % 5000}", year = 1800 + random.Next(226),
                priceKurus = random.Next(5_000, 60_000), stock = random.Next(0, 50), categories = cats,
                description = string.Join(' ', Enumerable.Range(0, 12).Select(_ => Words[random.Next(Words.Length)]))
            };
        }).ToArray();

        long before = await UsedMemoryAsync(server);
        foreach (var chunk in books.Select((b, i) => (b, i)).Chunk(2_000))
        {
            await Task.WhenAll(chunk.Select(x => db.JSON().SetAsync($"bench:json:{x.i}", "$", x.b)));
        }
        long afterJson = await UsedMemoryAsync(server);
        foreach (var chunk in books.Select((b, i) => (b, i)).Chunk(2_000))
        {
            await Task.WhenAll(chunk.Select(x => db.HashSetAsync($"bench:hash:{x.i}",
            [
                new("isbn", x.b.isbn), new("title", x.b.title), new("author", x.b.author), new("year", x.b.year),
                new("priceKurus", x.b.priceKurus), new("stock", x.b.stock), new("categories", string.Join(',', x.b.categories)),
                new("description", x.b.description)
            ])));
        }
        long afterHash = await UsedMemoryAsync(server);
        Console.WriteLine($"{count:N0} books: JSON {(afterJson - before) / 1048576.0:F1} MB, hash {(afterHash - afterJson) / 1048576.0:F1} MB");
        Console.WriteLine($"MEMORY USAGE one book: json={await MemoryUsageAsync(db, "bench:json:1")} B, hash={await MemoryUsageAsync(db, "bench:hash:1")} B");

        ISearchCommandsAsync ft = db.FT();
        foreach ((string name, IndexDataType type, string prefix) in new[]
                 { ("idx:bench-json", IndexDataType.JSON, "bench:json:"), ("idx:bench-hash", IndexDataType.HASH, "bench:hash:") })
        {
            bool json = type == IndexDataType.JSON;
            Stopwatch sw = Stopwatch.StartNew();
            await ft.CreateAsync(name, new FTCreateParams().On(type).Prefix(prefix).Language("turkish"),
                new Schema()
                    .AddTextField(new FieldName(json ? "$.title" : "title", "title"), weight: 2.0)
                    .AddTextField(new FieldName(json ? "$.description" : "description", "description"))
                    .AddTagField(new FieldName(json ? "$.categories[*]" : "categories", "category"))
                    .AddNumericField(new FieldName(json ? "$.priceKurus" : "priceKurus", "price"), sortable: true));
            InfoResult info;
            while ((info = await ft.InfoAsync(name)).PercentIndexed < 1)
            {
                await Task.Delay(50);
            }
            Console.WriteLine($"{name}: indexed {info.NumDocs:N0} docs in {sw.ElapsedMilliseconds:N0} ms, " +
                              $"index memory {await IndexMemoryAsync(db, name):F1} MB");
        }

        foreach (string q in new[] { "@category:{science\\-fiction} @price:[-inf 20000]", "@description:(ejderha hazine)" })
        {
            Query query = new Query(q).SetSortBy("price", true).Limit(0, 10).ReturnFields("title", "price");
            SearchResult result = await ft.SearchAsync("idx:bench-json", query);
            List<double> times = [];
            for (int i = 0; i < 200; i++)
            {
                long start = Stopwatch.GetTimestamp();
                await ft.SearchAsync("idx:bench-json", query);
                times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }
            times.Sort();
            Console.WriteLine($"{q}: {result.TotalResults:N0} matches, p50 {times[100]:F2} ms, p99 {times[198]:F2} ms");
        }
    }

    private static async Task<long> UsedMemoryAsync(IServer server)
    {
        await Task.Delay(200);
        return long.Parse((await server.InfoAsync("memory")).SelectMany(g => g).First(p => p.Key == "used_memory").Value);
    }

    // FT.INFO total_index_memory_sz_mb
    private static async Task<double> IndexMemoryAsync(IDatabase db, string index)
    {
        RedisResult[] info = (RedisResult[])(await db.ExecuteAsync("FT.INFO", index))!;
        for (int i = 0; i < info.Length - 1; i += 2)
        {
            if ((string?)info[i] == "total_index_memory_sz_mb")
            {
                return double.Parse((string)info[i + 1]!, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        return double.NaN;
    }

    private static async Task<long> MemoryUsageAsync(IDatabase db, string key) =>
        (long)await db.ExecuteAsync("MEMORY", "USAGE", key);
}
