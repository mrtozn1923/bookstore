using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Bookstore.Ordering;

namespace Bookstore.Api.Tests;

[Collection(typeof(MetricsCollection))]
public sealed class TracingTests(BookstoreApiFactory factory)
{
    private const string Middlemarch = "9780141439549";

    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public void StartActivity_returns_null_when_nobody_listens()
    {
        using ActivitySource source = new("Demo.NobodyListens");
        Assert.Null(source.StartActivity("check availability"));

        using ActivityListener listener = new()
        {
            ShouldListenTo = s => s.Name == source.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = source.StartActivity("check availability");
        Assert.NotNull(activity);
    }

    [Fact]
    public async Task Checkout_spans_and_the_queue_consumer_are_in_one_trace()
    {
        ConcurrentBag<Activity> stopped = [];
        using ActivityListener listener = new()
        {
            ShouldListenTo = s => s.Name.StartsWith("Bookstore.", StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add
        };
        ActivitySource.AddActivityListener(listener);

        HttpResponseMessage cart = await client.PostAsync("/api/carts", null, ct);
        Guid cartId = (await cart.Content.ReadFromJsonAsync<CartView>(ct))!.Id;
        await client.PutAsJsonAsync($"/api/carts/{cartId}/items/{Middlemarch}", new SetQuantityRequest(1), ct);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest(cartId, "Grace Hopper", "grace@example.com"), ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        int orderId = (await response.Content.ReadFromJsonAsync<OrderView>(ct))!.Id;

        // The consumer runs in the background; wait until its span has ended.
        Activity process = await WaitForAsync(() => stopped.FirstOrDefault(a =>
            a.DisplayName == "order-confirmations process" && Equals(a.GetTagItem("bookstore.order.id"), orderId)));
        Activity publish = stopped.Single(a =>
            a.DisplayName == "order-confirmations publish" && Equals(a.GetTagItem("bookstore.order.id"), orderId));
        Activity payment = stopped.Single(a =>
            a.DisplayName == "authorize payment" && Equals(a.GetTagItem("bookstore.order.id"), orderId));

        Assert.Equal(payment.TraceId, process.TraceId);
        Assert.Equal(publish.SpanId, process.ParentSpanId);
        Assert.Equal(ActivityKind.Consumer, process.Kind);
    }

    [Fact]
    public async Task Availability_of_a_whole_cart_is_checked_with_one_query()
    {
        ConcurrentBag<Activity> stopped = [];
        using ActivityListener listener = new()
        {
            ShouldListenTo = s => s.Name is "Bookstore.Ordering" or "OpenTelemetry.Instrumentation.SqlClient",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add
        };
        ActivitySource.AddActivityListener(listener);

        HttpResponseMessage cart = await client.PostAsync("/api/carts", null, ct);
        Guid cartId = (await cart.Content.ReadFromJsonAsync<CartView>(ct))!.Id;
        foreach (string isbn in (string[])["9780441172719", "9780261102217", "9780141439587", "9780451524935"])
        {
            await client.PutAsJsonAsync($"/api/carts/{cartId}/items/{isbn}", new SetQuantityRequest(1), ct);
        }
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest(cartId, "Ada Lovelace", "ada@example.com"), ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        int orderId = (await response.Content.ReadFromJsonAsync<OrderView>(ct))!.Id;

        ActivityTraceId traceId = stopped.Single(a =>
            a.DisplayName == "authorize payment" && Equals(a.GetTagItem("bookstore.order.id"), orderId)).TraceId;
        Activity check = stopped.Single(a => a.DisplayName == "check availability" && a.TraceId == traceId);
        Assert.Equal(4, check.GetTagItem("bookstore.cart.line_count"));
        // Four cart lines, one SELECT: no N+1.
        Assert.Single(stopped, a => a.ParentSpanId == check.SpanId);
    }

    private async Task<Activity> WaitForAsync(Func<Activity?> find)
    {
        for (int i = 0; i < 50; i++)
        {
            if (find() is { } activity)
            {
                return activity;
            }
            await Task.Delay(100, ct);
        }
        throw new TimeoutException("The span did not arrive.");
    }
}
