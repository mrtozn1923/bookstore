using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

// A partner integration reading institutional orders, written against the September contract.
HttpClient http = new() { BaseAddress = new Uri("http://127.0.0.1:52318") };
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "linus");
JsonSerializerOptions options = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
const string url = "/institutions/lakeside-school/orders?pageSize=3";

try
{
    StrictPage? strict = await http.GetFromJsonAsync<StrictPage>(url, options);
    Console.WriteLine($"strict: {strict!.Items.Count} orders");
}
catch (JsonException e)
{
    Console.WriteLine($"strict: {e.Message}");
}

TolerantPage? page = await http.GetFromJsonAsync<TolerantPage>(url, options);
foreach (TolerantOrder order in page!.Items)
{
    Console.WriteLine($"tolerant: {order.Id} {order.Status} -> {StatusMapping.From(order.Status)}");
}

// The status values the integration knew in September.
enum KnownStatus { Pending, Confirmed, Shipped, Delivered, Cancelled }

enum StatusView { Pending, Confirmed, Shipped, Delivered, Cancelled, Other }

record StrictOrder(Guid Id, KnownStatus Status);
record StrictPage(List<StrictOrder> Items);
record TolerantOrder(Guid Id, string Status);
record TolerantPage(List<TolerantOrder> Items);

static class StatusMapping
{
    // Tolerant reader: an unknown value becomes Other instead of an exception.
    public static StatusView From(string status) =>
        Enum.TryParse(status, ignoreCase: false, out StatusView view) && Enum.IsDefined(view) && view != StatusView.Other
            ? view
            : StatusView.Other;
}
