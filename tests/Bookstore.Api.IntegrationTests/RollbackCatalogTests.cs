using System.Net.Http.Json;
using System.Transactions;
using Bookstore.Api.Endpoints;
using Bookstore.Api.IntegrationTests.Infrastructure;

namespace Bookstore.Api.IntegrationTests;

public sealed class RollbackCatalogTests(BookstoreApiFactory factory) : IClassFixture<BookstoreApiFactory>
{
    [Theory]
    [InlineData("978-0201633610")]
    [InlineData("978-0201485677")]
    [InlineData("978-0321127426")]
    public async Task Each_test_sees_only_its_own_book(string isbn)
    {
        // TestServer, isteği testin ExecutionContext'iyle çalıştırsın; ambient transaction API'ye ulaşsın
        factory.Server.PreserveExecutionContext = true;
        using TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled);

        HttpClient admin = factory.CreateClientFor(TestUser.Admin);
        HttpResponseMessage created = await admin.PostAsJsonAsync("/api/books",
            new CreateBookRequest(isbn, "Rollback", 10m, 1), TestContext.Current.CancellationToken);
        created.EnsureSuccessStatusCode();

        List<BookDto>? books = await admin.GetFromJsonAsync<List<BookDto>>("/api/books", TestContext.Current.CancellationToken);
        Assert.Equal(isbn, Assert.Single(books!).Isbn);
        // scope.Complete() çağrılmadı: Dispose, transaction'ı rollback eder
    }
}
