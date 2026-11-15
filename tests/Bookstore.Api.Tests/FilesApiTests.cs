using System.Text;
using System.Text.Json;
using Bookstore.Api.Ai;
using Bookstore.Api.Catalog.Publishers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Tests;

public sealed class FilesApiTests
{
    // The documented upload response, with an expiry because we send expires_in_seconds.
    private const string Uploaded = """
        {"id":"file_011CNha8iCJcU1wXNR6q4V8w","type":"file","filename":"spring-catalog.pdf","mime_type":"application/pdf",
         "size_bytes":1024000,"created_at":"2026-11-15T09:00:00Z","downloadable":false,"expires_at":"2026-12-15T09:00:00Z"}
        """;

    private const string Answer = """
        {"id":"msg_catalog1","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":"The catalog lists three new fantasy titles for March."}],
         "stop_reason":"end_turn","usage":{"input_tokens":18,"cache_creation_input_tokens":5200,"cache_read_input_tokens":0,"output_tokens":14}}
        """;

    private static (PublisherCatalogService Service, FakeClaudeHandler Handler, InMemoryPublisherCatalogStore Store) Create(params string[] responses)
    {
        FakeClaudeHandler handler = new(responses);
        InMemoryPublisherCatalogStore store = new();
        PublisherCatalogService service = new(handler.CreateClient(), store, Options.Create(new ClaudeOptions()),
            NullLogger<PublisherCatalogService>.Instance);
        return (service, handler, store);
    }

    [Fact]
    public async Task Upload_SendsMultipartWithExpiryAndKeepsTheFileIdOnTheServer()
    {
        (PublisherCatalogService service, FakeClaudeHandler handler, InMemoryPublisherCatalogStore store) = Create(Uploaded);
        using MemoryStream pdf = new(Encoding.ASCII.GetBytes("%PDF-1.7 sample"));

        PublisherCatalogFile file = await service.UploadAsync("pub-42", pdf, "spring-catalog.pdf", CancellationToken.None);

        HttpRequestMessage request = handler.Requests[0];
        Assert.Equal("/v1/files", request.RequestUri!.AbsolutePath);
        Assert.StartsWith("multipart/form-data", handler.Requests[0].Content!.Headers.ContentType!.ToString());
        Assert.Contains("name=expires_in_seconds", handler.RequestBodies[0]);
        Assert.Contains("2592000", handler.RequestBodies[0]);
        Assert.Contains("filename=\"spring-catalog.pdf\"", handler.RequestBodies[0]);
        Assert.Equal("file_011CNha8iCJcU1wXNR6q4V8w", store.Find("pub-42")!.FileId);
        Assert.Equal(new DateTimeOffset(2026, 12, 15, 9, 0, 0, TimeSpan.Zero), file.ExpiresAt);
    }

    [Fact]
    public void BuildRequest_ReferencesTheFileAndCachesTheDocument()
    {
        string body = JsonSerializer.Serialize(
            PublisherCatalogService.BuildRequest("file_011CNha8iCJcU1wXNR6q4V8w", "Which titles are new?", new ClaudeOptions()).RawBodyData);
        JsonElement document = JsonDocument.Parse(body).RootElement.GetProperty("messages")[0].GetProperty("content")[0];

        Assert.Equal("document", document.GetProperty("type").GetString());
        Assert.Equal("""{"type":"file","file_id":"file_011CNha8iCJcU1wXNR6q4V8w"}""", document.GetProperty("source").GetRawText());
        Assert.Equal("""{"type":"ephemeral"}""", document.GetProperty("cache_control").GetRawText());
    }

    [Fact]
    public async Task Ask_WithAnExpiredFile_DoesNotCallTheApi()
    {
        (PublisherCatalogService service, FakeClaudeHandler handler, InMemoryPublisherCatalogStore store) = Create(Answer);
        store.Save("pub-42", new PublisherCatalogFile("file_011CNha8iCJcU1wXNR6q4V8w", "old.pdf", DateTimeOffset.UtcNow.AddMinutes(-1)));

        Assert.Null(await service.AskAsync("pub-42", "Which titles are new?", CancellationToken.None));
        Assert.Empty(handler.Requests);
    }
}
