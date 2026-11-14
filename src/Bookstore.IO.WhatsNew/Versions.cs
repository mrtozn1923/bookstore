using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using Bookstore.Ordering;
using Microsoft.Win32.SafeHandles;

namespace Bookstore.IO.WhatsNew;

public static class Demo
{
    public static readonly string NdjsonPath = Path.Join("out", "orders.ndjson");

    public static void Try(string label, Action action)
    {
        try { action(); }
        catch (Exception ex) { Console.WriteLine($"{label}: {ex.GetType().Name}: {ex.Message}"); }
    }
}

public static class Net6
{
    public static async Task RunAsync()
    {
        Directory.CreateDirectory("out");
        var options = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Options = FileOptions.Asynchronous,
            PreallocationSize = 1_000_000
        };
        await using (var file = new FileStream(Demo.NdjsonPath, options))
        {
            foreach (Order order in SampleOrders.Generate(1_000))
            {
                await JsonSerializer.SerializeAsync(file, order, OrderContext.Default.Order);
                file.WriteByte((byte)'\n');
            }
        }
        Console.WriteLine($"FileStreamOptions: {new FileInfo(Demo.NdjsonPath).Length:N0} bytes written");

        using SafeFileHandle handle = File.OpenHandle(Demo.NdjsonPath);
        byte[] first = new byte[30], second = new byte[30];
        // İki okuma aynı handle'ı paylaşır; Position yok, offset her çağrıda verilir.
        await Task.WhenAll(
            RandomAccess.ReadAsync(handle, first, fileOffset: 0).AsTask(),
            RandomAccess.ReadAsync(handle, second, fileOffset: 1_000).AsTask());
        Console.WriteLine($"RandomAccess @0:    {Encoding.UTF8.GetString(first)}");
        Console.WriteLine($"RandomAccess @1000: {Encoding.UTF8.GetString(second)}");

        JsonNode node = JsonNode.Parse(File.ReadLines(Demo.NdjsonPath).First())!;
        node["Status"] = "Paid";
        node.AsObject().Remove("Items");
        Console.WriteLine($"JsonNode: {node.ToJsonString()}");
    }
}

public static class Net7
{
    public sealed class BookRequest
    {
        public required string Isbn { get; init; }
        public int Quantity { get; init; } = 1;
    }

    public static async Task RunAsync()
    {
        var header = new MemoryStream("BKEX\u0001\u0000"u8.ToArray());
        byte[] magic = new byte[4];
        header.ReadExactly(magic);
        Console.WriteLine($"ReadExactly: {Encoding.ASCII.GetString(magic)}");

        int lines = 0;
        await foreach (string line in File.ReadLinesAsync(Demo.NdjsonPath)) lines++;
        Console.WriteLine($"File.ReadLinesAsync: {lines:N0} lines");

        PaymentMethod[] payments = [new CardPayment("4242"), new BankTransfer("BS-0000042")];
        Console.WriteLine($"Polymorphism: {JsonSerializer.Serialize(payments)}");
        Console.WriteLine($"Polymorphism: {JsonSerializer.Deserialize<PaymentMethod[]>("""[{"type":"card","Last4":"1881"}]""")![0]}");

        Demo.Try("required", () => JsonSerializer.Deserialize<BookRequest>("""{"Quantity":2}"""));
    }
}

public static class Net8
{
    public static void Run()
    {
        var book = new BookDto("9780441172719", "Dune", null);
        var snake = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        Console.WriteLine($"SnakeCaseLower: {JsonSerializer.Serialize(new { OrderNumber = "BS-0000042", CreatedAtUtc = DateTime.UnixEpoch }, snake)}");

        var disallow = new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        Demo.Try("Disallow", () => JsonSerializer.Deserialize<BookDto>("""{"Isbn":"9780441172719","Title":"Dune","Price":349.90}""", disallow));

        const string json = """{"Name":"Ada","Tags":["institutional"]}""";
        var replace = JsonSerializer.Deserialize<CustomerProfile>(json)!;
        var populate = JsonSerializer.Deserialize<CustomerProfile>(json,
            new JsonSerializerOptions { PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate })!;
        Console.WriteLine($"Read-only Tags: default {replace.Tags.Count}, Populate {populate.Tags.Count}");
    }
}

public static class Net9
{
    public static async Task RunAsync()
    {
        var book = new BookDto("9780441172719", "Dune", null);
        Console.WriteLine($"Web: {JsonSerializer.Serialize(book, JsonSerializerOptions.Web)}");
        Demo.Try("Web.Converters.Add", () => JsonSerializerOptions.Web.Converters.Add(new JsonStringEnumConverter()));

        var nullable = new JsonSerializerOptions { RespectNullableAnnotations = true };
        Demo.Try("RespectNullableAnnotations", () => JsonSerializer.Deserialize<BookDto>("""{"Isbn":"9780441172719","Title":null}""", nullable));

        var indented = new JsonSerializerOptions { WriteIndented = true, IndentCharacter = ' ', IndentSize = 2 };
        Console.WriteLine($"IndentSize 2:\n{JsonSerializer.Serialize(book, indented)}");

        await using (var stream = File.OpenRead(Demo.NdjsonPath))
        {
            int count = 0;
            await foreach (Order? order in JsonSerializer.DeserializeAsyncEnumerable<Order>(stream, topLevelValues: true)) count++;
            Console.WriteLine($"topLevelValues: {count:N0} orders");
        }
        await using (var stream = File.OpenRead(Demo.NdjsonPath))
        {
            try
            {
                await foreach (Order? order in JsonSerializer.DeserializeAsyncEnumerable<Order>(stream)) { }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Without topLevelValues: {ex.GetType().Name}: {ex.Message}");
            }
        }

        JsonNode schema = JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(BookDto));
        Console.WriteLine($"JsonSchemaExporter: {schema.ToJsonString()}");
    }
}

public static class Net10
{
    public static async Task RunAsync()
    {
        const string duplicate = """{"Isbn":"9780441172719","Title":"Dune","Title":"Emma","Subtitle":null}""";
        Console.WriteLine($"Default: {JsonSerializer.Deserialize<BookDto>(duplicate)}");
        Demo.Try("Strict", () => JsonSerializer.Deserialize<BookDto>(duplicate, JsonSerializerOptions.Strict));

        var fiction = new Category { Name = "Fiction" };
        fiction.Children.Add(new Category { Name = "Science Fiction", Parent = fiction });
        Console.WriteLine($"ReferenceHandler: {JsonSerializer.Serialize(fiction, CategoryContext.Default.Category)}");

        var pipe = new Pipe();
        Order sample = SampleOrders.Generate(1).Single();
        await JsonSerializer.SerializeAsync(pipe.Writer, sample, OrderContext.Default.Order);
        await pipe.Writer.CompleteAsync();
        Order? read = await JsonSerializer.DeserializeAsync(pipe.Reader, OrderContext.Default.Order);
        await pipe.Reader.CompleteAsync();
        Console.WriteLine($"PipeReader: {read!.OrderNumber}, {read.Items.Count} items, {read.Total:N2} {read.Currency}");
    }
}
