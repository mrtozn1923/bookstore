using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Net.ServerSentEvents;
using Bookstore.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

var app = builder.Build();

// Prints the OpenTelemetry tags of every HTTP server activity (Activity.Stopped)
using var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
    Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
    ActivityStopped = activity => app.Logger.LogInformation("Activity tags: {Tags}",
        string.Join(", ", activity.TagObjects.Select(tag => $"{tag.Key}={tag.Value}")))
};
ActivitySource.AddActivityListener(listener);

// Adds X-Pipeline to every response that passes through the rest of the pipeline
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Pipeline"] = "full";
    await next(context);
});

app.MapOpenApi();

Book[] catalog =
[
    new(1, "Dune", "978-0441013593", 450m),
    new(2, "Emma", "978-0141439587", 280m),
    new(3, "The Hobbit", "978-0547928227", 320m),
];

app.MapPost("/books", (CreateBookRequest request) =>
    TypedResults.Created($"/books/42", new BookDto(42, request.Title, request.Isbn, request.Price)));

app.MapGet("/books", (int page, int pageSize) =>
    TypedResults.Ok(new { page, pageSize }))
    .AddEndpointFilter(async (context, next) =>
    {
        // Binding başarısızsa status code zaten 400
        if (context.HttpContext.Response.StatusCode == StatusCodes.Status400BadRequest)
        {
            app.Logger.LogWarning("Binding failed for {Path}{Query}", context.HttpContext.Request.Path, context.HttpContext.Request.QueryString);
            return TypedResults.Problem(
                title: "Invalid paging parameters.",
                detail: "page and pageSize must be integers.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        return await next(context);
    });

app.MapPost("/books/search", (BookSearch search) =>
    TypedResults.Ok(catalog.Where(b => search.Price switch
    {
        decimal max => b.Price <= max,
        PriceRange range => b.Price >= range.Min && b.Price <= range.Max,
    })));

app.MapMethods("/books/search", ["QUERY"], (BookSearch search) =>
    TypedResults.Ok(catalog.Where(b => search.Price switch
    {
        decimal max => b.Price <= max,
        PriceRange range => b.Price >= range.Min && b.Price <= range.Max,
    })));

#pragma warning disable CS0618
app.MapGet("/v1/books/{id:int}", GetLegacyBook);
#pragma warning restore CS0618

app.MapGet("/orders/{id:int}/status-stream", (int id, CancellationToken cancellationToken) =>
    TypedResults.ServerSentEvents(GetStatusesAsync(id, cancellationToken)));

app.MapGet("/health", [ShortCircuit] () => "Healthy");

app.MapControllers();

app.Run();

#pragma warning disable CS0618
[Obsolete("Use /books/{id}.")]
static BookDto GetLegacyBook(int id) => new(id, "Dune", "978-0441013593", 450m);
#pragma warning restore CS0618

static async IAsyncEnumerable<SseItem<OrderStatus>> GetStatusesAsync(int orderId, [EnumeratorCancellation] CancellationToken cancellationToken)
{
    string[] statuses = ["Received", "Packed", "Shipped"];
    foreach (string status in statuses)
    {
        await Task.Delay(500, cancellationToken);
        yield return new SseItem<OrderStatus>(new OrderStatus(orderId, status), "order-status");
    }
}
