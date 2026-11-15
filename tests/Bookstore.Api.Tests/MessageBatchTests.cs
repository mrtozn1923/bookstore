using System.Text.Json;
using System.Text.RegularExpressions;
using Anthropic.Models.Messages.Batches;
using Bookstore.Api.Ai;
using Bookstore.Api.Catalog;
using Bookstore.Api.Catalog.Seo;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bookstore.Api.Tests;

public sealed class MessageBatchTests
{
    // The batch object and the succeeded lines follow the documented examples, the errored line the documented error shape.
    // Ids and texts are adapted to Bookstore.
    private const string CreatedBatch = """
        {"id":"msgbatch_01HkcTjaV5uDC8jWR4ZsDV8d","type":"message_batch","processing_status":"in_progress",
         "request_counts":{"processing":4,"succeeded":0,"errored":0,"canceled":0,"expired":0},
         "ended_at":null,"created_at":"2026-11-15T18:37:24.100435Z","expires_at":"2026-11-16T18:37:24.100435Z",
         "cancel_initiated_at":null,"results_url":null}
        """;

    private const string EndedBatch = """
        {"id":"msgbatch_01HkcTjaV5uDC8jWR4ZsDV8d","type":"message_batch","processing_status":"ended",
         "request_counts":{"processing":0,"succeeded":2,"errored":1,"canceled":0,"expired":1},
         "ended_at":"2026-11-15T19:02:11.000000Z","created_at":"2026-11-15T18:37:24.100435Z","expires_at":"2026-11-16T18:37:24.100435Z",
         "cancel_initiated_at":null,"results_url":"https://api.anthropic.com/v1/messages/batches/msgbatch_01HkcTjaV5uDC8jWR4ZsDV8d/results"}
        """;

    // Results arrive in any order: the second book comes first.
    private const string Results = """
        {"custom_id":"seo-9780141439587","result":{"type":"succeeded","message":{"id":"msg_014VwiXbi91y3JMjcpyGBHX5","type":"message","role":"assistant","model":"claude-haiku-5-5","content":[{"type":"text","text":"Emma by Jane Austen: a clever, rich young woman plays matchmaker in her village and misreads almost everyone. A sharp, funny classic."}],"stop_reason":"end_turn","stop_sequence":null,"usage":{"input_tokens":96,"output_tokens":34}}}}
        {"custom_id":"seo-9780441172719","result":{"type":"succeeded","message":{"id":"msg_01FqfsLoHwgeFbguDgpz48m7","type":"message","role":"assistant","model":"claude-haiku-5-5","content":[{"type":"text","text":"Dune by Frank Herbert: on the desert planet Arrakis, young Paul Atreides is pulled into a war over the spice that rules the galaxy."}],"stop_reason":"end_turn","stop_sequence":null,"usage":{"input_tokens":98,"output_tokens":35}}}}
        {"custom_id":"seo-9780547928227","result":{"type":"errored","error":{"type":"error","error":{"type":"invalid_request_error","message":"max_tokens: Field required"}}}}
        {"custom_id":"seo-9780141439549","result":{"type":"expired"}}
        """;

    private static (SeoBatchProcessor Processor, FakeClaudeHandler Handler, InMemoryBookSeoStore Seo, InMemorySeoBatchJobStore Jobs) Create(
        params FakeResponse[] responses)
    {
        FakeClaudeHandler handler = new(responses);
        InMemoryBookSeoStore seo = new();
        InMemorySeoBatchJobStore jobs = new();
        SeoBatchProcessor processor = new(handler.CreateClient(), new InMemoryBookRepository(), seo, jobs,
            NullLogger<SeoBatchProcessor>.Instance);
        return (processor, handler, seo, jobs);
    }

    [Fact]
    public void CustomIds_MatchTheDocumentedPattern()
    {
        Regex allowed = new("^[a-zA-Z0-9_-]{1,64}$");

        foreach (Book book in new InMemoryBookRepository().All())
        {
            Request request = BookSeoBatch.BuildRequest(book);
            Assert.Matches(allowed, request.CustomID);
            Assert.Equal(book.Isbn, BookSeoBatch.IsbnFrom(request.CustomID));
        }
    }

    [Fact]
    public async Task Submit_StoresTheBatchIdAndDoesNotSendTwice()
    {
        (SeoBatchProcessor processor, FakeClaudeHandler handler, _, InMemorySeoBatchJobStore jobs) = Create(new FakeResponse(CreatedBatch));

        IReadOnlyList<string> first = await processor.SubmitMissingAsync(CancellationToken.None);
        IReadOnlyList<string> second = await processor.SubmitMissingAsync(CancellationToken.None);

        Assert.Equal(["msgbatch_01HkcTjaV5uDC8jWR4ZsDV8d"], first);
        Assert.Empty(second);
        Assert.Single(handler.Requests);
        Assert.Equal("/v1/messages/batches", handler.Requests[0].RequestUri!.AbsolutePath);

        JsonElement body = JsonDocument.Parse(handler.RequestBodies[0]).RootElement;
        Assert.Equal(4, body.GetProperty("requests").GetArrayLength());
        Assert.Equal("""{"type":"disabled"}""", body.GetProperty("requests")[0].GetProperty("params").GetProperty("thinking").GetRawText());
        Assert.Equal(["msgbatch_01HkcTjaV5uDC8jWR4ZsDV8d"], jobs.Pending());
    }

    [Fact]
    public async Task Collect_MatchesResultsByCustomIdAndSortsOutRetries()
    {
        (SeoBatchProcessor processor, FakeClaudeHandler handler, InMemoryBookSeoStore seo, InMemorySeoBatchJobStore jobs) = Create(
            new FakeResponse(EndedBatch),
            new FakeResponse(Results, "application/x-jsonl"));
        jobs.Add("msgbatch_01HkcTjaV5uDC8jWR4ZsDV8d");

        SeoBatchReport? report = await processor.CollectAsync("msgbatch_01HkcTjaV5uDC8jWR4ZsDV8d", CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal(2, report.Saved);
        Assert.StartsWith("Dune by Frank Herbert", seo.Find("9780441172719"));
        Assert.StartsWith("Emma by Jane Austen", seo.Find("9780141439587"));
        Assert.Equal(["9780547928227"], report.Rejected);
        Assert.Equal(["9780141439549"], report.Retry);
        Assert.Empty(jobs.Pending());
        Assert.Equal(["/v1/messages/batches/msgbatch_01HkcTjaV5uDC8jWR4ZsDV8d",
                      "/v1/messages/batches/msgbatch_01HkcTjaV5uDC8jWR4ZsDV8d/results"],
            handler.Requests.Select(r => r.RequestUri!.AbsolutePath));
    }

    [Fact]
    public async Task Collect_WhileInProgress_ReturnsNullAndKeepsTheJob()
    {
        (SeoBatchProcessor processor, _, _, InMemorySeoBatchJobStore jobs) = Create(new FakeResponse(CreatedBatch));
        jobs.Add("msgbatch_01HkcTjaV5uDC8jWR4ZsDV8d");

        Assert.Null(await processor.CollectAsync("msgbatch_01HkcTjaV5uDC8jWR4ZsDV8d", CancellationToken.None));
        Assert.Single(jobs.Pending());
    }

    [Fact]
    public void BatchPrice_IsHalfOfTheStandardPrice()
    {
        TokenUsage usage = new(1_000_000, 1_000_000, 0, 0, 0);

        // Sonnet 5.5: $2 + $10 standard, $1 + $5 in a batch.
        Assert.Equal(12.00m, ClaudePricing.EstimateUsd("claude-sonnet-5-5", usage));
        Assert.Equal(6.00m, ClaudePricing.EstimateUsd("claude-sonnet-5-5", usage, batch: true));
    }
}
