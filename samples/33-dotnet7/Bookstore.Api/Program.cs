using Microsoft.AspNetCore.RateLimiting;
using Bookstore.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IBookService, InMemoryBookService>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit = 2;
        limiter.Window = TimeSpan.FromSeconds(10);
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddOutputCache();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseRateLimiter();
app.UseOutputCache();

var books = app.MapGroup("/api/books");

books.MapGet("/", (IBookService service) => TypedResults.Ok(service.GetAll()));

books.MapGet("/{id:int}", BookEndpoints.GetById);

books.MapPost("/", (CreateBookRequest request, IBookService service) =>
{
    var book = service.Add(request.Title, request.Price);
    return TypedResults.Created($"/api/books/{book.Id}", book);
})
.AddEndpointFilter<ValidateBookFilter>();

books.MapGet("/limited", () => TypedResults.Ok("ok"))
    .RequireRateLimiting("fixed");

books.MapGet("/featured", () => TypedResults.Ok(new { GeneratedAt = DateTime.UtcNow.ToString("HH:mm:ss.fff") }))
    .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)));

app.MapControllers();

app.Run();
