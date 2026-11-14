using Bookstore.Search;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Testcontainers.Elasticsearch;
using HttpMethod = Elastic.Transport.HttpMethod;

namespace Bookstore.Search.Tests;

// One Elasticsearch container for the whole test class: the lab image (with analysis-icu), our synonyms,
// search/books.index.json and data/books.json. Nothing here depends on the lab running.
public sealed class ElasticsearchFixture : IAsyncLifetime
{
    private static readonly string Root = FindRepositoryRoot();
    private IFutureDockerImage? _image;
    private ElasticsearchContainer? _container;

    public ElasticsearchClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(Path.Combine(Root, "lab", "elasticsearch"))
            .WithBuildArgument("ES_VERSION", "9.5.6")
            .WithName("bookstore-search-es-test:9.5.6")
            .Build();
        await _image.CreateAsync();

        // TEST_CONTAINER_PREFIX lets CI give containers a recognisable name.
        string prefix = Environment.GetEnvironmentVariable("TEST_CONTAINER_PREFIX") ?? "bookstore-search-test";
        _container = new ElasticsearchBuilder(_image)
            .WithName($"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 9)])
            .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
            .Build();
        await _container.StartAsync();

        // The container uses HTTPS with a self-signed certificate and the elastic user: fine for a throwaway test node only.
        Uri uri = new(_container.GetConnectionString());
        string[] credentials = Uri.UnescapeDataString(uri.UserInfo).Split(':', 2);
        ElasticsearchClientSettings settings = new ElasticsearchClientSettings(
                new SingleNodePool(new UriBuilder(uri) { UserName = "", Password = "" }.Uri), SearchClientFactory.CreateSourceSerializer)
            .Authentication(new BasicAuthentication(credentials[0], credentials[1]))
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
            .DefaultMappingFor<Book>(m => m.IndexName(SearchClientFactory.BooksIndex).IdProperty(b => b.Isbn));
        Client = new ElasticsearchClient(settings);

        await PutAsync("/_synonyms/bookstore-synonyms", Path.Combine(Root, "search", "synonyms.json"));
        await PutAsync($"/{SearchClientFactory.BooksIndex}", Path.Combine(Root, "search", "books.index.json"));

        IReadOnlyList<Book> books = await BookCatalog.LoadAsync(Path.Combine(Root, "data", "books.json"));
        BulkResponse bulk = await Client.BulkAsync(b => b.Index(SearchClientFactory.BooksIndex).IndexMany(books).Refresh(Refresh.WaitFor));
        if (!bulk.IsValidResponse)
        {
            throw new InvalidOperationException($"Bulk load failed: {bulk.DebugInformation}");
        }
    }

    private async Task PutAsync(string path, string file)
    {
        StringResponse response = await Client.Transport.RequestAsync<StringResponse>(
            HttpMethod.PUT, path, PostData.String(await File.ReadAllTextAsync(file)));
        if (!response.ApiCallDetails.HasSuccessfulStatusCode)
        {
            throw new InvalidOperationException($"PUT {path}: HTTP {response.ApiCallDetails.HttpStatusCode} {response.Body}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bookstore.slnx")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Bookstore.slnx not found above the test output folder.");
    }
}
