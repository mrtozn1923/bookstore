using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Bookstore.IO.Json.Payments;
using Bookstore.Ordering;

namespace Bookstore.IO.Json;

public static class JsonDemos
{
    private static readonly OrderItem Item = new("9780441172719", "Dune", 2, 349.90m);

    private static void Try(string label, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            Console.WriteLine($"{label}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void Defaults()
    {
        var order = new Order("BS-0000042", "Ada", new DateTime(2026, 11, 14, 9, 30, 0, DateTimeKind.Utc),
            OrderStatus.Paid, "TRY", [Item]);

        Console.WriteLine($"Default: {JsonSerializer.Serialize(order)}");
        Console.WriteLine($"Web:     {JsonSerializer.Serialize(order, JsonSerializerOptions.Web)}");

        var snake = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };
        Console.WriteLine($"Snake:   {JsonSerializer.Serialize(order, snake)}");

        var web = JsonSerializerOptions.Web;
        Console.WriteLine($"Web options: case-insensitive={web.PropertyNameCaseInsensitive}, " +
                          $"numbers={web.NumberHandling}, naming={web.PropertyNamingPolicy?.GetType().Name}");
    }

    public static void CaseAndNumbers()
    {
        const string json = """{"orderNumber":"BS-0000042","customerName":"Ada","status":1,"currency":"TRY","items":[]}""";
        var byDefault = JsonSerializer.Deserialize<Order>(json)!;
        var byWeb = JsonSerializer.Deserialize<Order>(json, JsonSerializerOptions.Web)!;
        Console.WriteLine($"Default: OrderNumber={byDefault.OrderNumber ?? "null"}, Items={byDefault.Items?.Count.ToString() ?? "null"}");
        Console.WriteLine($"Web:     OrderNumber={byWeb.OrderNumber}, Items={byWeb.Items.Count}");

        const string priceAsString = """{"isbn":"9780441172719","title":"Dune","quantity":"2","unitPrice":"349.90"}""";
        Try("Default", () => JsonSerializer.Deserialize<OrderItem>(priceAsString, new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            PropertyNameCaseInsensitive = true
        }));
        var fromStrings = JsonSerializer.Deserialize<OrderItem>(priceAsString, JsonSerializerOptions.Web)!;
        Console.WriteLine($"Web:     {fromStrings.Title} x{fromStrings.Quantity} @ {fromStrings.UnitPrice}");
    }

    public static void ModelRules()
    {
        Try("required", () => JsonSerializer.Deserialize<PaymentRequest>(
            """{"OrderNumber":"BS-0000042","Method":{"type":"card","Brand":"Visa","Last4":"4242"}}"""));

        var disallow = new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        Try("unmapped", () => JsonSerializer.Deserialize<OrderItem>(
            """{"Isbn":"9780441172719","Title":"Dune","Quantity":2,"UnitPrice":349.90,"Discount":100}""", disallow));

        const string nullTitle = """{"Isbn":"9780441172719","Title":null,"Quantity":2,"UnitPrice":349.90}""";
        var lenient = JsonSerializer.Deserialize<OrderItem>(nullTitle)!;
        Console.WriteLine($"default: Title is {(lenient.Title is null ? "null" : lenient.Title)}");
        Try("nullable", () => JsonSerializer.Deserialize<OrderItem>(nullTitle,
            new JsonSerializerOptions { RespectNullableAnnotations = true }));

        const string missingCtorArg = """{"Isbn":"9780441172719","Quantity":2,"UnitPrice":349.90}""";
        var missing = JsonSerializer.Deserialize<OrderItem>(missingCtorArg)!;
        Console.WriteLine($"default: missing Title becomes {(missing.Title is null ? "null" : missing.Title)}");
        Try("ctor", () => JsonSerializer.Deserialize<OrderItem>(missingCtorArg,
            new JsonSerializerOptions { RespectRequiredConstructorParameters = true }));
    }

    public static void Duplicates()
    {
        const string json = """{"OrderNumber":"BS-0000042","Amount":"10.00 TRY","Amount":"9999.00 TRY","Method":{"type":"card","Brand":"Visa","Last4":"4242"}}""";

        var payment = JsonSerializer.Deserialize<PaymentRequest>(json)!;
        Console.WriteLine($"Default: Amount = {payment.Amount}");

        using (var doc = JsonDocument.Parse(json))
        {
            JsonElement first = doc.RootElement.EnumerateObject().First(p => p.Name == "Amount").Value;
            Console.WriteLine($"JsonDocument, first Amount property: {first}");
        }

        Try("AllowDuplicateProperties=false", () => JsonSerializer.Deserialize<PaymentRequest>(json,
            new JsonSerializerOptions { AllowDuplicateProperties = false }));
        Try("Strict", () => JsonSerializer.Deserialize<PaymentRequest>(json, JsonSerializerOptions.Strict));
    }

    public static void Strict()
    {
        var strict = JsonSerializerOptions.Strict;
        Console.WriteLine($"Strict: unmapped={strict.UnmappedMemberHandling}, duplicates={strict.AllowDuplicateProperties}, " +
                          $"case-insensitive={strict.PropertyNameCaseInsensitive}, nullable={strict.RespectNullableAnnotations}, " +
                          $"ctor={strict.RespectRequiredConstructorParameters}");

        const string good = """{"Isbn":"9780441172719","Title":"Dune","Quantity":2,"UnitPrice":349.90}""";
        Console.WriteLine($"Strict reads default output: {JsonSerializer.Deserialize<OrderItem>(good, strict)!.Title}");
        Try("camelCase", () => JsonSerializer.Deserialize<OrderItem>(
            """{"isbn":"9780441172719","title":"Dune","quantity":2,"unitPrice":349.90}""", strict));

        var strictWeb = new JsonSerializerOptions(JsonSerializerOptions.Strict)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var item = JsonSerializer.Deserialize<OrderItem>(
            """{"isbn":"9780441172719","title":"Dune","quantity":2,"unitPrice":349.90}""", strictWeb)!;
        Console.WriteLine($"Strict + camelCase: {item.Title} x{item.Quantity}");
    }

    public static void Polymorphism()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Web)
        {
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };
        var request = new PaymentRequest
        {
            OrderNumber = "BS-0000042",
            Amount = new Money(699.80m, "TRY"),
            Method = new BankTransferPayment("TR000000000000000000000000", "BS-0000042"),
            Status = PaymentStatus.RefundPending,
            DueDate = new DateOnly(2026, 11, 21)
        };
        string json = JsonSerializer.Serialize(request, options);
        Console.WriteLine(json);

        var back = JsonSerializer.Deserialize<PaymentRequest>(json, options)!;
        Console.WriteLine($"Round trip: {back.Method.GetType().Name}, {back.Amount}, {back.Status}");

        PaymentMethod[] methods = [new CardPayment("Visa", "4242"), new BankTransferPayment("TR000000000000000000000000", "BS-0000043")];
        Console.WriteLine(JsonSerializer.Serialize(methods, options));

        const string late = """{"brand":"Visa","last4":"4242","type":"card"}""";
        Try("discriminator last", () => JsonSerializer.Deserialize<PaymentMethod>(late, options));
        var outOfOrder = new JsonSerializerOptions(options) { AllowOutOfOrderMetadataProperties = true };
        Console.WriteLine($"AllowOutOfOrderMetadataProperties: {JsonSerializer.Deserialize<PaymentMethod>(late, outOfOrder)}");

        Try("unknown type", () => JsonSerializer.Deserialize<PaymentMethod>("""{"type":"crypto","wallet":"x"}""", options));
        Try("no type", () => JsonSerializer.Deserialize<PaymentMethod>("""{"brand":"Visa","last4":"4242"}""", options));
    }

    public static void Converters()
    {
        Try("money", () => JsonSerializer.Deserialize<Money>("249.90"));
        Try("money", () => JsonSerializer.Deserialize<Money>("\"249,90 TL\""));
        Console.WriteLine($"Money: {JsonSerializer.Deserialize<Money>("\"249.90 TRY\"")}");

        Console.WriteLine($"DateOnly default: {JsonSerializer.Serialize(new DateOnly(2026, 11, 14))}");
        var partner = new JsonSerializerOptions { Converters = { new DayMonthYearDateOnlyConverter() } };
        Console.WriteLine($"DateOnly partner: {JsonSerializer.Serialize(new DateOnly(2026, 11, 14), partner)}");
        Console.WriteLine($"Parsed: {JsonSerializer.Deserialize<DateOnly>("\"21.11.2026\"", partner):yyyy-MM-dd}");
    }

    public static void Modifiers()
    {
        var customer = new Customer("Ada", "ada@example.com") { Phone = "+90 555 000 00 00" };
        Console.WriteLine($"API:  {JsonSerializer.Serialize(customer)}");

        var forLogs = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { HideSensitiveData }
            }
        };
        Console.WriteLine($"Logs: {JsonSerializer.Serialize(customer, forLogs)}");
    }

    // Contract customization: drop every property marked [SensitiveData].
    private static void HideSensitiveData(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
        foreach (JsonPropertyInfo property in typeInfo.Properties)
        {
            if (property.AttributeProvider?.IsDefined(typeof(SensitiveDataAttribute), inherit: true) == true)
            {
                property.ShouldSerialize = static (_, _) => false;
            }
        }
    }

    public static void Populate()
    {
        const string json = """{"Tags":["new"]}""";
        var replace = JsonSerializer.Deserialize<BookFilter>(json)!;
        var populate = JsonSerializer.Deserialize<BookFilter>(json,
            new JsonSerializerOptions { PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate })!;
        Console.WriteLine($"Replace:  [{string.Join(", ", replace.Tags)}]");
        Console.WriteLine($"Populate: [{string.Join(", ", populate.Tags)}]");
    }
}

public sealed class BookFilter
{
    public List<string> Tags { get; set; } = ["in-stock"];
}
