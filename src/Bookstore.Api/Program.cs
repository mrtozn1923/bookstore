using Bookstore.Api;
using Bookstore.Search;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Options, validation and the singleton client (post 10); debug mode only in Development.
builder.Services.AddBookstoreSearch(builder.Configuration, builder.Environment.IsDevelopment());

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
