using System.Text.Json;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Extensions;

namespace Bookstore.Search.Tests;

// Unit tests: the request is built and serialized, nothing is sent.
public sealed class BookSearchRequestBuilderTests
{
    private static readonly ElasticsearchClient Client = new(new ElasticsearchClientSettings(new Uri("http://localhost:9200")));

    private static JsonElement Build(BookSearchFilter filter, string? sort = null)
    {
        SearchRequest<Book> request = BookSearchRequestBuilder.Build(new BookSearchInput(filter, sort), SearchTuning.Tuned);
        return JsonDocument.Parse(Client.RequestResponseSerializer.SerializeToString(request)).RootElement;
    }

    [Fact]
    public void Post_filter_is_omitted_when_no_filter_is_selected()
    {
        JsonElement json = Build(new BookSearchFilter("roman"));

        // Post 09: a null BoolQuery once became "post_filter": {} and every search failed with HTTP 400.
        Assert.False(json.TryGetProperty("post_filter", out _));
    }

    [Fact]
    public void Each_facet_ignores_its_own_filter()
    {
        JsonElement json = Build(new BookSearchFilter("roman", Category: "Classics", Language: "tr"));
        string categoryFacet = json.GetProperty("aggregations").GetProperty("category").GetProperty("filter").GetRawText();
        string languageFacet = json.GetProperty("aggregations").GetProperty("language").GetProperty("filter").GetRawText();

        Assert.Contains("\"language\"", categoryFacet);
        Assert.DoesNotContain("\"categories\"", categoryFacet);
        Assert.Contains("\"categories\"", languageFacet);
        Assert.DoesNotContain("\"language\"", languageFacet);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("price_asc")]
    [InlineData("newest")]
    public void Every_sort_ends_with_the_isbn_tie_breaker(string? sort)
    {
        JsonElement last = Build(new BookSearchFilter("dune"), sort).GetProperty("sort").EnumerateArray().Last();

        Assert.True(last.TryGetProperty("isbn", out _));
    }
}
