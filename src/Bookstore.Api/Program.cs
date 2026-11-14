using Bookstore.Api;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ElasticsearchOptions elasticsearch = builder.Configuration.GetSection(ElasticsearchOptions.SectionName).Get<ElasticsearchOptions>()
    ?? new ElasticsearchOptions();

// Singleton: the client keeps the connection pool and node state; never create one per request.
builder.Services.AddSingleton<ElasticsearchClient>(_ => SearchClientFactory.Create(elasticsearch));

// Autocomplete answers are the same for everyone: cache them for a short time (post 08).
builder.Services.AddOutputCache(options => options.AddPolicy(BookSuggestEndpoints.CachePolicy, policy => policy
    .Expire(TimeSpan.FromSeconds(30))
    .SetVaryByQuery("q", "category")));

WebApplication app = builder.Build();

app.UseOutputCache();

app.MapBookEndpoints();
app.MapBookSearchEndpoints();
app.MapBookSuggestEndpoints();

app.Run();
