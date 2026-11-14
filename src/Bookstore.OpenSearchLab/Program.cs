using System.Globalization;
using OpenSearch.Client;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// Post 13: the same search against OpenSearch with OpenSearch.Client (a fork of NEST 7.10).
string url = Environment.GetEnvironmentVariable("BOOKSTORE_OS_URL") ?? "http://localhost:9200";
ConnectionSettings settings = new ConnectionSettings(new Uri(url)).DefaultIndex("books");
OpenSearchClient client = new(settings);

ISearchResponse<BookHit> response = await client.SearchAsync<BookHit>(s => s
    .Size(5)
    .Query(q => q.Bool(b => b
        .Must(m => m.Match(mt => mt.Field(f => f.Title).Query("dune")))
        .Filter(f => f.Term(t => t.Field(x => x.Language).Value("tr"))))));

// NEST naming: IsValid instead of IsValidResponse, ApiCall instead of ApiCallDetails.
Console.WriteLine($"{response.ApiCall.HttpMethod} {response.ApiCall.Uri.PathAndQuery} -> HTTP {response.ApiCall.HttpStatusCode}, IsValid {response.IsValid}, {response.Total} hit(s)");
foreach (BookHit book in response.Documents)
{
    Console.WriteLine($"  {book.Isbn} {book.Title} ({book.Language}) {book.Price}");
}

public sealed class BookHit
{
    public string Isbn { get; set; } = "";

    public string Title { get; set; } = "";

    public string Language { get; set; } = "";

    public decimal Price { get; set; }
}
