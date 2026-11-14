using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using RichardSzalay.MockHttp;

namespace Bookstore.Shipping.Tests;

public sealed class ParcelTrackingErrorTests
{
    private const string Parcel = "https://carrier.example.com/api/v2/parcels/BS2026000042TR";
    private readonly MockHttpMessageHandler carrier = new();

    private static ParcelTrackingClient CreateClient(HttpMessageHandler handler, TimeSpan? timeout = null) =>
        new(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://carrier.example.com/api/v2/"),
            Timeout = timeout ?? TimeSpan.FromSeconds(10),
        });

    private Task<ParcelStatus?> GetStatus(HttpMessageHandler handler, TimeSpan? timeout = null) =>
        CreateClient(handler, timeout).GetStatusAsync("BS2026000042TR", TestContext.Current.CancellationToken);

    [Fact]
    public async Task Rate_limit_carries_the_retry_after_delay()
    {
        carrier.When(Parcel).Respond(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30)) },
        });

        CarrierException ex = await Assert.ThrowsAsync<CarrierException>(() => GetStatus(carrier));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), ex.RetryAfter);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Server_errors_surface_as_carrier_exception(HttpStatusCode status)
    {
        carrier.When(Parcel).Respond(status);

        CarrierException ex = await Assert.ThrowsAsync<CarrierException>(() => GetStatus(carrier));

        Assert.Equal(status, ex.StatusCode);
        Assert.Null(ex.RetryAfter);
    }

    [Fact]
    public async Task Slow_carrier_hits_the_client_timeout()
    {
        StubHandler slow = new(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        CarrierException ex = await Assert.ThrowsAsync<CarrierException>(
            () => GetStatus(slow, timeout: TimeSpan.FromMilliseconds(200)));

        Assert.Equal("Carrier did not respond within 0.2s.", ex.Message);
        Assert.IsType<TimeoutException>(ex.InnerException?.InnerException);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_reported_as_a_carrier_error()
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        StubHandler slow = new(async (_, ct) =>
        {
            await cts.CancelAsync();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => CreateClient(slow).GetStatusAsync("BS2026000042TR", cts.Token));
    }

    [Fact]
    public async Task Unreadable_body_keeps_the_json_error_as_inner_exception()
    {
        carrier.When(Parcel).Respond("application/json", """
            { "trackingNumber": "BS2026000042TR", "state": "InTransit", "deliveredAtUtc": "tomorrow", "events": [] }
            """);

        CarrierException ex = await Assert.ThrowsAsync<CarrierException>(() => GetStatus(carrier));

        JsonException json = Assert.IsType<JsonException>(ex.InnerException);
        Assert.Equal("$.deliveredAtUtc", json.Path);
    }
}
