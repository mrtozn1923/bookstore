using Bookstore.Api;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ElasticsearchOptions elasticsearch = builder.Configuration.GetSection(ElasticsearchOptions.SectionName).Get<ElasticsearchOptions>()
    ?? new ElasticsearchOptions();

// Singleton: the client keeps the connection pool and node state; never create one per request.
builder.Services.AddSingleton<ElasticsearchClient>(_ => SearchClientFactory.Create(elasticsearch));

WebApplication app = builder.Build();

app.MapBookEndpoints();

app.Run();
