using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Microsoft.Extensions.AI;

namespace Bookstore.Api.Tests;

public sealed class ProductionTests
{
    // The spend-cap body is the documented example; the others use the documented error shape.
    private const string SpendCap = """
        {"type":"error","error":{"type":"rate_limit_error","message":"You have reached your API usage limits.",
         "details":{"error_code":"enforced_spend_limit_reached"}},"request_id":"req_018EeWyXxfu5pfWkrYcMdjWG"}
        """;

    private const string Overloaded = """{"type":"error","error":{"type":"overloaded_error","message":"Overloaded"},"request_id":"req_test_529"}""";

    private const string BadRequest = """{"type":"error","error":{"type":"invalid_request_error","message":"max_tokens: Field required"},"request_id":"req_test_400"}""";

    private const string Ok = """
        {"id":"msg_test_ok","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":"Dune ships tomorrow."}],"stop_reason":"end_turn","usage":{"input_tokens":12,"output_tokens":5}}
        """;

    private static FakeResponse Error(HttpStatusCode status, string body, string requestId, string? retryAfter = null)
    {
        Dictionary<string, string> headers = new() { ["request-id"] = requestId };
        if (retryAfter is not null)
        {
            headers["retry-after"] = retryAfter;
        }

        return new FakeResponse(body, Status: status, Headers: headers);
    }

    private static Task<Message> Send(AnthropicClient client) => client.Messages.Create(new MessageCreateParams
    {
        Model = "claude-haiku-5-5",
        MaxTokens = 50,
        Messages = [new UserMessageParam { Content = "Where is order 1042?" }],
    });

    [Fact]
    public async Task SpendCap_IsItsOwnExceptionAndIsNotWorthRetrying()
    {
        FakeClaudeHandler handler = new(Error(HttpStatusCode.TooManyRequests, SpendCap, "req_018EeWyXxfu5pfWkrYcMdjWG"));

        Exception error = await Assert.ThrowsAnyAsync<AnthropicRateLimitException>(() => Send(handler.CreateClient()));
        ClaudeFailure failure = ClaudeFailureClassifier.Classify(error);

        Assert.IsType<Anthropic.Models.AnthropicEnforcedSpendLimitReachedException>(error);
        Assert.Equal(new ClaudeFailure(ClaudeFailureKind.SpendCapReached, false, 429, "req_018EeWyXxfu5pfWkrYcMdjWG",
            "enforced_spend_limit_reached"), failure);
    }

    [Fact]
    public async Task SpendCap_IsStillRetriedBySdkWithoutRetryAfter()
    {
        FakeClaudeHandler handler = new(
            Error(HttpStatusCode.TooManyRequests, SpendCap, "req_018EeWyXxfu5pfWkrYcMdjWG"),
            Error(HttpStatusCode.TooManyRequests, SpendCap, "req_018EeWyXxfu5pfWkrYcMdjWG"),
            Error(HttpStatusCode.TooManyRequests, SpendCap, "req_018EeWyXxfu5pfWkrYcMdjWG"));
        AnthropicClient client = new() { ApiKey = "fake-api-key-for-tests", HttpClient = new HttpClient(handler), MaxRetries = 2 };

        await Assert.ThrowsAsync<Anthropic.Models.AnthropicEnforcedSpendLimitReachedException>(() => Send(client));

        // Every 429 is retried; this one keeps failing until the next month.
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task BadRequest_IsNotRetriedBySdk()
    {
        FakeClaudeHandler handler = new(Error(HttpStatusCode.BadRequest, BadRequest, "req_test_400"));
        AnthropicClient client = new() { ApiKey = "fake-api-key-for-tests", HttpClient = new HttpClient(handler), MaxRetries = 2 };

        Exception error = await Assert.ThrowsAsync<AnthropicBadRequestException>(() => Send(client));

        Assert.Single(handler.Requests);
        Assert.Equal(ClaudeFailureKind.InvalidRequest, ClaudeFailureClassifier.Classify(error).Kind);
    }

    [Fact]
    public async Task Overloaded_IsRetriedBySdkUntilItSucceeds()
    {
        FakeClaudeHandler handler = new(
            Error((HttpStatusCode)529, Overloaded, "req_test_529"),
            Error((HttpStatusCode)529, Overloaded, "req_test_529"),
            new FakeResponse(Ok));
        AnthropicClient client = new() { ApiKey = "fake-api-key-for-tests", HttpClient = new HttpClient(handler), MaxRetries = 2 };

        Message message = await Send(client);

        Assert.Equal("msg_test_ok", message.ID);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(["0", "1", "2"], handler.Requests.Select(r => r.Headers.GetValues("x-stainless-retry-count").Single()));
    }

    [Fact]
    public async Task SdkSpan_CarriesGenAiAttributesAndTheRequestId()
    {
        ConcurrentBag<Activity> spans = [];
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name.StartsWith("com.anthropic.sdk.csharp", StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Add,
        };
        ActivitySource.AddActivityListener(listener);
        FakeClaudeHandler handler = new(new FakeResponse(Ok, Headers: new Dictionary<string, string> { ["request-id"] = "req_test_span" }));

        await Send(handler.CreateClient());

        Activity span = Assert.Single(spans, s => (string?)s.GetTagItem("anthropic.request.id") == "req_test_span");
        Assert.Equal("anthropic.messages.create", span.DisplayName);
        Assert.Equal("claude-haiku-5-5", span.GetTagItem("gen_ai.request.model"));
        Assert.Equal(12L, Convert.ToInt64(span.GetTagItem("gen_ai.usage.input_tokens")));
        // The SDK also propagates the trace context to the API.
        Assert.StartsWith("00-" + span.TraceId, handler.Requests[0].Headers.GetValues("traceparent").Single());
    }

    [Fact]
    public async Task ChatClient_IgnoresTemperatureForThisApiVersion()
    {
        FakeClaudeHandler handler = new(Ok);
        IChatClient chat = handler.CreateClient().AsIChatClient("claude-haiku-5-5");

        ChatResponse response = await chat.GetResponseAsync("Where is order 1042?", new ChatOptions { Temperature = 0.2f, MaxOutputTokens = 50 });

        Assert.Equal("Dune ships tomorrow.", response.Text);
        JsonElement body = JsonDocument.Parse(handler.RequestBodies[0]).RootElement;
        Assert.False(body.TryGetProperty("temperature", out _));
        Assert.Equal(50, body.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public void UsageMeter_RecordsCostPerFeature()
    {
        List<(string Instrument, double Value, string? Feature)> measurements = [];
        using MeterListener listener = new();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == ClaudeUsageMeter.MeterName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((i, v, tags, _) => measurements.Add((i.Name, v, Feature(tags))));
        listener.Start();
        using ClaudeUsageMeter meter = new(TestMeters.Factory);

        decimal usd = meter.Record("support", "claude-sonnet-5-5", new TokenUsage(1_000_000, 100_000, 0, 0, 0));

        // Sonnet 5.5: $2 input + 0.1 x $10 output.
        Assert.Equal(3.00m, usd);
        Assert.Contains(("bookstore.claude.cost", 3.0, "support"), measurements);

        static string? Feature(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "feature")
                {
                    return tag.Value as string;
                }
            }

            return null;
        }
    }
}
