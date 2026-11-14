using Bookstore.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCloudflareForwarding(Path.Combine(builder.Environment.ContentRootPath, "cloudflare-ips.txt"));
builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);
builder.Services.AddLoginRateLimit();
var app = builder.Build();

app.UseForwardedHeaders();
app.RequireFrontDoorId(app.Configuration["FrontDoor:Id"]);
app.UseHttpsRedirection();
app.UseRateLimiter();

var originRequests = 0;
app.Use(async (context, next) =>
{
    // which responses really reached the origin
    context.Response.Headers["X-Origin-Request"] = Interlocked.Increment(ref originRequests).ToString();
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = CacheHeaders.ForStaticFile });

Book[] catalog =
[
    new("9780441172719", "Dune", 450.00m),
    new("9780141439587", "Emma", 190.00m),
    new("9780547928227", "The Hobbit", 320.00m),
    new("9780141439549", "Middlemarch", 280.00m),
];

var api = app.MapGroup("/api");

api.MapGet("/whoami", (HttpContext http) =>
    new { ClientIp = http.Connection.RemoteIpAddress?.ToString(), http.Request.Scheme });

api.MapGet("/books", (HttpContext http, int page = 1) =>
{
    CacheHeaders.ForCatalog(http.Response);
    return catalog.Skip((page - 1) * 2).Take(2);
});

api.MapGet("/books/featured", (HttpContext http) =>
{
    // browsers revalidate every time, the CDN keeps it for 5 minutes (RFC 9213)
    http.Response.Headers.CacheControl = "no-cache";
    http.Response.Headers["CDN-Cache-Control"] = "max-age=300";
    return catalog[0];
});

api.MapGet("/cart", (HttpContext http) =>
{
    http.Response.Headers.CacheControl = "private, no-store";
    http.Response.Cookies.Append("cart-id", "c-1042", new CookieOptions { HttpOnly = true, Secure = true });
    return new Cart(2, 640.00m);
});

api.MapGet("/orders/mine", (HttpContext http) =>
{
    // lab only: the bearer token is just the customer name
    var customer = http.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    if (customer.Length == 0)
    {
        return Results.Unauthorized();
    }

    http.Response.Headers.CacheControl = CacheHeaders.PerUser;
    return Results.Ok(new OrderSummary(customer, customer == "ada" ? ["BK-1042"] : ["BK-2001", "BK-2002"]));
});

api.MapPost("/auth/login", (LoginRequest login) =>
    // lab only: a real implementation checks the password with ASP.NET Core Identity
    login.Password == "correct horse" ? Results.Ok() : Results.Unauthorized())
    .RequireRateLimiting(EdgeProtection.LoginPolicy);

app.MapFallbackToFile("index.html", new StaticFileOptions { OnPrepareResponse = CacheHeaders.ForStaticFile });

app.Run();

record Book(string Isbn, string Title, decimal Price);
record Cart(int Items, decimal Total);
record OrderSummary(string Customer, string[] Orders);
record LoginRequest(string Email, string Password);
