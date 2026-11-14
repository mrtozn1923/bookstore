using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Scalar.AspNetCore;
using Bookstore.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Bookstore API";
        document.Info.Description = "Books, categories and orders.";
        return Task.CompletedTask;
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<BookRepository>();
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception switch
    {
        KeyNotFoundException => StatusCodes.Status404NotFound,
        ArgumentException => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Bookstore API"));
}

app.MapGet("/legacy/books/{id:int}", async (int id, IMemoryCache cache, BookRepository repository) =>
{
    BookDto? book = await cache.GetOrCreateAsync($"book:{id}", entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return repository.GetAsync(id);
    });
    return book is null ? Results.NotFound() : Results.Ok(book);
});

app.MapGet("/books/{id:int}", async (int id, HybridCache cache, BookRepository repository, CancellationToken cancellationToken) =>
{
    BookDto? book = await cache.GetOrCreateAsync(
        $"book:{id}",
        async token => await repository.GetAsync(id, token),
        cancellationToken: cancellationToken);
    return book is null ? Results.NotFound() : Results.Ok(book);
})
.WithName("GetBookById");

app.MapGet("/stats", (BookRepository repository) => new { DatabaseCalls = repository.Calls });

app.MapGet("/books/{id:int}/stock", Results<Ok<StockDto>, InternalServerError<string>> (int id) =>
    id == 1
        ? TypedResults.Ok(new StockDto(id, 42))
        : TypedResults.InternalServerError("Stock service is unavailable."));

app.MapGet("/orders/{id:int}", (int id) =>
{
    throw new KeyNotFoundException($"Order {id} was not found.");
});

app.MapGet("/audit", () =>
{
    var log = new AuditLog();
    log.Add("book created");
    log.Add("price changed", "stock updated", "published");
    return log.Count;
});

app.Run();
