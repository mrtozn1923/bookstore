using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bookstore.Ordering;

namespace Bookstore.IO.WhatsNew;

// The pre-upgrade way of doing each item; kept so the "before" snippets compile and run.
public static class Before
{
    // .NET 6 öncesi: paylaşılan FileStream'de Position + Read, lock ile.
    public static int ReadAt(FileStream file, byte[] buffer, long offset)
    {
        lock (file)
        {
            file.Position = offset;
            return file.Read(buffer, 0, buffer.Length);
        }
    }

    // .NET 7 öncesi: Read istenenden az dönebilir, döngü şart.
    public static void ReadFully(Stream stream, byte[] buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int read = stream.Read(buffer, total, buffer.Length - total);
            if (read == 0) throw new EndOfStreamException();
            total += read;
        }
    }

    public static async Task<int> CountLinesAsync(string path)
    {
        int lines = 0;
        using var reader = new StreamReader(path);
        while (await reader.ReadLineAsync() is not null) lines++;
        return lines;
    }

    // .NET 8 öncesi: snake_case için kendi naming policy'n.
    public sealed class SnakeCasePolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) =>
            string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
    }

    // .NET 8 öncesi: bilinmeyen alanları yakalamak için JsonExtensionData.
    public sealed class StrictBookDto
    {
        public string Isbn { get; set; } = "";
        public string Title { get; set; } = "";

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Unknown { get; set; }
    }

    // .NET 9 öncesi: web varsayılanları için kendi static instance'ın.
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    // .NET 9 öncesi: NDJSON'u satır satır okuyup her satırı ayrı deserialize etmek.
    public static async Task<int> CountNdjsonAsync(string path)
    {
        int count = 0;
        await foreach (string line in File.ReadLinesAsync(path))
        {
            if (JsonSerializer.Deserialize(line, OrderContext.Default.Order) is not null) count++;
        }
        return count;
    }

    // .NET 10 öncesi: her ayarı tek tek açmak (duplicate kontrolü yoktu).
    public static readonly JsonSerializerOptions Strict = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    // .NET 10 öncesi: PipeReader'ı Stream'e çevirmek.
    public static async Task<Order?> ReadFromPipeAsync(PipeReader reader) =>
        await JsonSerializer.DeserializeAsync(reader.AsStream(), OrderContext.Default.Order);

    public static async Task RunAsync()
    {
        await using (var file = new FileStream(Demo.NdjsonPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
        {
            byte[] buffer = new byte[30];
            ReadAt(file, buffer, 1_000);
            Console.WriteLine($"Position + Read @1000: {Encoding.UTF8.GetString(buffer)}");
        }

        byte[] magic = new byte[4];
        ReadFully(new MemoryStream("BKEX"u8.ToArray()), magic);
        Console.WriteLine($"Read loop: {Encoding.ASCII.GetString(magic)}");
        Console.WriteLine($"StreamReader loop: {await CountLinesAsync(Demo.NdjsonPath):N0} lines");
        Console.WriteLine($"Custom policy: {JsonSerializer.Serialize(new { OrderNumber = "BS-0000042" }, new JsonSerializerOptions { PropertyNamingPolicy = new SnakeCasePolicy() })}");

        var book = JsonSerializer.Deserialize<StrictBookDto>("""{"Isbn":"9780441172719","Title":"Dune","Price":349.90}""")!;
        Console.WriteLine($"JsonExtensionData: unknown = {string.Join(", ", book.Unknown!.Keys)}");
        Console.WriteLine($"NDJSON line by line: {await CountNdjsonAsync(Demo.NdjsonPath):N0} orders");

        var pipe = new Pipe();
        await JsonSerializer.SerializeAsync(pipe.Writer, SampleOrders.Generate(1).Single(), OrderContext.Default.Order);
        await pipe.Writer.CompleteAsync();
        Console.WriteLine($"PipeReader.AsStream: {(await ReadFromPipeAsync(pipe.Reader))!.OrderNumber}");
    }
}
