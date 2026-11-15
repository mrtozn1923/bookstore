using System.Text.Json;
using System.Text.Json.Nodes;
using Bookstore.Api.Ai;
using Bookstore.Api.Catalog;
using Bookstore.Api.Ordering;
using Bookstore.Api.Returns;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Tests;

public sealed class StructuredOutputTests
{
    private static string Reply(string stopReason, string text) => $$$"""
        {"id":"msg_01","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":{{{JsonSerializer.Serialize(text)}}}}],
         "stop_reason":"{{{stopReason}}}","stop_sequence":null,"usage":{"input_tokens":610,"output_tokens":85}}
        """;

    private const string DraftJson = """
        {"order_id":"1042","reason":"damaged","items":[{"title":"Dune","quantity":1}],"urgency":"normal","summary":"Dune arrived with a torn cover."}
        """;

    private static ReturnRequestExtractor CreateExtractor(FakeClaudeHandler handler) =>
        new(handler.CreateClient(), Options.Create(new ClaudeOptions()));

    private static readonly ReturnPolicy Policy = new(new InMemoryOrderRepository(), new InMemoryBookRepository());

    [Fact]
    public void Schema_IsClosedOnEveryObject()
    {
        JsonObject schema = ClaudeJsonSchema.For<ReturnRequestDraft>();

        Assert.False(schema["additionalProperties"]!.GetValue<bool>());
        Assert.False(schema["properties"]!["items"]!["items"]!["additionalProperties"]!.GetValue<bool>());
        Assert.Equal(5, schema["required"]!.AsArray().Count);
    }

    [Fact]
    public async Task ExtractAsync_SendsTheSchemaAndParsesTheText()
    {
        FakeClaudeHandler handler = new(Reply("end_turn", DraftJson));

        ExtractionResult result = await CreateExtractor(handler).ExtractAsync(
            "Order 1042: Dune came with a torn cover, I want to send it back.", CancellationToken.None);

        Assert.Equal(new ReturnItem("Dune", 1), Assert.Single(result.Draft!.Items));
        Assert.Equal(ReturnReason.Damaged, result.Draft.Reason);

        JsonElement format = JsonDocument.Parse(handler.RequestBodies[0]).RootElement
            .GetProperty("output_config").GetProperty("format");
        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        Assert.False(format.GetProperty("schema").GetProperty("additionalProperties").GetBoolean());
    }

    [Theory]
    [InlineData("refusal")]
    [InlineData("max_tokens")]
    public async Task ExtractAsync_DoesNotParseIncompleteOutput(string stopReason)
    {
        FakeClaudeHandler handler = new(Reply(stopReason, """{"order_id":"1042","reason":"dam"""));

        ExtractionResult result = await CreateExtractor(handler).ExtractAsync("...", CancellationToken.None);

        Assert.Null(result.Draft);
        Assert.NotNull(result.Problem);
    }

    [Fact]
    public void Deserialize_EnumCasingDifferences_StillParse()
    {
        // The documentation says the casing of enum values is not guaranteed.
        ReturnRequestDraft? draft = JsonSerializer.Deserialize<ReturnRequestDraft>(
            DraftJson.Replace("\"damaged\"", "\"Damaged\""), ClaudeJsonSchema.SerializerOptions);

        Assert.Equal(ReturnReason.Damaged, draft!.Reason);
    }

    [Fact]
    public void Policy_SchemaValidDraftForSomeoneElsesOrder_IsRejected()
    {
        ReturnRequestDraft draft = JsonSerializer.Deserialize<ReturnRequestDraft>(DraftJson, ClaudeJsonSchema.SerializerOptions)!;

        Assert.Empty(Policy.Check(draft, new CurrentCustomer(InMemoryOrderRepository.Ada)));
        Assert.Equal(["Order 1042 was not found for this customer."], Policy.Check(draft, new CurrentCustomer(InMemoryOrderRepository.Alan)));
    }

    [Fact]
    public void Policy_BookNotInTheOrder_IsReported()
    {
        ReturnRequestDraft draft = new("1042", ReturnReason.WrongItem, [new ReturnItem("Emma", 1)], Urgency.Normal, "Wrong book.");

        Assert.Equal(["'Emma' is not in order 1042."], Policy.Check(draft, new CurrentCustomer(InMemoryOrderRepository.Ada)));
    }
}
