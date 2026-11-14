using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;

namespace Bookstore.Search.Tests;

// Integration tests against a real Elasticsearch in a container (Testcontainers).
public sealed class BooksIndexTests(ElasticsearchFixture fixture) : IClassFixture<ElasticsearchFixture>
{
    private readonly ElasticsearchClient _client = fixture.Client;

    [Fact]
    public async Task Autocomplete_analyzer_folds_before_splitting_prefixes()
    {
        AnalyzeIndexResponse response = await _client.Indices.AnalyzeAsync(a => a
            .Index(SearchClientFactory.BooksIndex)
            .Analyzer("tr_autocomplete")
            .Text("Kürk Mantolu"), TestContext.Current.CancellationToken);

        List<string> tokens = response.Tokens!.Select(t => t.Token).ToList();
        Assert.Contains("kur", tokens);
        Assert.Contains("mantolu", tokens);
        Assert.DoesNotContain("kür", tokens);
    }

    [Fact]
    public async Task Search_without_turkish_characters_finds_the_book_first()
    {
        SearchRequest<Book> request = BookSearchRequestBuilder.Build(new BookSearchInput(new BookSearchFilter("kurk mantolu")), SearchTuning.Tuned);

        SearchResponse<Book> response = await _client.SearchAsync<Book>(request, TestContext.Current.CancellationToken);

        Assert.True(response.IsValidResponse, response.DebugInformation);
        Assert.Equal("9781000000030", response.Hits.First().Source!.Isbn);
    }

    [Fact]
    public async Task Selected_category_keeps_the_other_options_in_its_facet()
    {
        SearchRequest<Book> request = BookSearchRequestBuilder.Build(
            new BookSearchInput(new BookSearchFilter("roman", Category: "Historical Fiction")), SearchTuning.Tuned);

        SearchResponse<Book> response = await _client.SearchAsync<Book>(request, TestContext.Current.CancellationToken);
        Facet categories = BookFacets.Read(response.Aggregations).Single(f => f.Name == BookFacets.Category);

        Assert.All(response.Hits, h => Assert.Contains("Historical Fiction", h.Source!.Categories));
        Assert.Contains(categories.Values, v => v.Key == "Classics" && v.Count > 0);
    }

    [Fact]
    public async Task Strict_mapping_rejects_an_unknown_field()
    {
        IndexResponse response = await _client.IndexAsync(new { isbn = "9789999999991", title = "Draft", pageCount = 412 },
            i => i.Index(SearchClientFactory.BooksIndex).Id("9789999999991"), TestContext.Current.CancellationToken);

        Assert.False(response.IsValidResponse);
        Assert.Equal("strict_dynamic_mapping_exception", response.ElasticsearchServerError?.Error?.Type);
    }
}
