using System.Net;
using System.Runtime.CompilerServices;
using Bookstore.Bff.Books;
using PactNet;
using PactNet.Matchers;

namespace Bookstore.Bff.ContractTests;

// Consumer side: what the BFF's book page needs from Catalog, written down as a pact.
// The test runs the real CatalogApi against Pact's mock server; the pact file is the result.
public sealed class CatalogApiPactTests
{
    // Pact's native library reports anonymous usage statistics unless this is set
    static CatalogApiPactTests() => Environment.SetEnvironmentVariable("PACT_DO_NOT_TRACK", "true");

    private readonly IPactBuilderV4 pact = Pact.V4("Bookstore.Bff", "Bookstore.Catalog", new PactConfig { PactDir = PactFolder() })
        .WithHttpInteractions();

    [Fact]
    public async Task The_book_page_reads_a_book_from_Catalog()
    {
        pact.UponReceiving("a request for an existing book")
            .Given("the book 9780141439587 exists")
            .WithRequest(HttpMethod.Get, "/api/v1/books/9780141439587")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            // Only the fields the BFF reads, with types instead of exact values: Catalog may add fields freely
            .WithJsonBody(new
            {
                isbn = "9780141439587",
                title = Match.Type("Emma"),
                author = Match.Type("Jane Austen"),
                category = Match.Type("Classics"),
                price = Match.Decimal(249.90m),
                currency = Match.Regex("TRY", "^[A-Z]{3}$"),
                publishedYear = Match.Integer(1815),
                description = Match.Type("A young woman in a small English village keeps arranging other people's marriages.")
            });

        await pact.VerifyAsync(async context =>
        {
            CatalogApi catalog = new(new HttpClient { BaseAddress = context.MockServerUri });

            CatalogBook? book = await catalog.GetBookAsync("9780141439587", TestContext.Current.CancellationToken);

            Assert.Equal("Emma", book?.Title);
            Assert.Equal(249.90m, book?.Price);
        });
    }

    [Fact]
    public async Task An_unknown_book_is_a_404_not_an_error()
    {
        pact.UponReceiving("a request for a book that does not exist")
            .Given("the book 9780000000000 does not exist")
            .WithRequest(HttpMethod.Get, "/api/v1/books/9780000000000")
            .WillRespond()
            .WithStatus(HttpStatusCode.NotFound);

        await pact.VerifyAsync(async context =>
        {
            CatalogApi catalog = new(new HttpClient { BaseAddress = context.MockServerUri });

            Assert.Null(await catalog.GetBookAsync("9780000000000", TestContext.Current.CancellationToken));
        });
    }

    // tests/pacts: the provider's test reads the file from there (in CI it would go to a Pact Broker)
    private static string PactFolder([CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "pacts"));
}
