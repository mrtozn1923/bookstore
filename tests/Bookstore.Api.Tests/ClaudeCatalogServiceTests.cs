using System.Text.Json;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Bookstore.Api.Catalog;

namespace Bookstore.Api.Tests;

public sealed class ClaudeCatalogServiceTests
{
    private static readonly Book Dune = new InMemoryBookRepository().FindByIsbn("9780441172719")!;

    [Fact]
    public void BuildRequest_UsesConfiguredModelAndLimit()
    {
        MessageCreateParams request = ClaudeCatalogService.BuildRequest(Dune, new ClaudeOptions { Model = "claude-sonnet-5-5", MaxTokens = 300 });

        Assert.Equal("claude-sonnet-5-5", request.Model.Raw());
        Assert.Equal(300, request.MaxTokens);
        Assert.Single(request.Messages);
    }

    [Fact]
    public void BuildRequest_SendsNoSamplingParameters()
    {
        string body = JsonSerializer.Serialize(ClaudeCatalogService.BuildRequest(Dune, new ClaudeOptions()).RawBodyData);

        // API version 2026-10-21 rejects temperature, top_p and top_k with a 400.
        Assert.DoesNotContain("temperature", body);
        Assert.DoesNotContain("top_p", body);
        Assert.DoesNotContain("top_k", body);
    }
}
