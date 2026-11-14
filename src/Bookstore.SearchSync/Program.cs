using Bookstore.Search;
using Bookstore.SearchSync;
using Elastic.Clients.Elasticsearch;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

ElasticsearchOptions elasticsearch = builder.Configuration.GetSection(ElasticsearchOptions.SectionName).Get<ElasticsearchOptions>()
    ?? new ElasticsearchOptions();
string connectionString = builder.Configuration.GetConnectionString("Bookstore")
    ?? throw new InvalidOperationException("ConnectionStrings:Bookstore is missing.");

builder.Services.Configure<SearchSyncOptions>(builder.Configuration.GetSection(SearchSyncOptions.SectionName));
builder.Services.AddSingleton<ElasticsearchClient>(_ => SearchClientFactory.Create(elasticsearch));
builder.Services.AddSingleton(new BookChangeReader(connectionString));
builder.Services.AddHostedService<SearchSyncWorker>();

IHost host = builder.Build();
host.Run();
