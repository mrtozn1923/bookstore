using System.Net;
using Bookstore.Api.Carts;
using Bookstore.Api.Catalog;
using Bookstore.Api.Institutional;
using Bookstore.Api.Middleware;
using Bookstore.Api.Shipping;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BookCatalog>();
builder.Services.AddSingleton<CartStore>();
builder.Services.AddSingleton<InstitutionDirectory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<OrderAudit>();
builder.Services.AddKeyedSingleton<IShippingCalculator, StandardShipping>("standard");
builder.Services.AddKeyedSingleton<IShippingCalculator, ExpressShipping>("express");
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddAntiforgery();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Loopback varsayılan olarak güvenilir; gerçek proxy'nin adresini ekle
    options.KnownProxies.Add(IPAddress.Parse("10.0.0.5"));
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<ResponseTimeMiddleware>();
app.UseMiddleware<RequestBodyLoggingMiddleware>();
app.UseMiddleware<TenantMiddleware>();
app.UseAntiforgery();

app.MapGet("/api/debug/request", (HttpRequest request) =>
{
    RequestHeaders headers = request.GetTypedHeaders();
    return new
    {
        request.Method,
        request.Scheme,
        Host = request.Host.Value,
        PathBase = request.PathBase.Value,
        Path = request.Path.Value,
        QueryString = request.QueryString.Value,
        Search = request.Query["q"].ToString(),
        UserAgent = request.Headers.UserAgent.ToString(),
        AcceptLanguage = headers.AcceptLanguage.Select(l => $"{l.Value}:{l.Quality ?? 1}"),
        RemoteIp = request.HttpContext.Connection.RemoteIpAddress?.ToString(),
        TraceId = request.HttpContext.TraceIdentifier,
    };
});

app.MapGet("/api/debug/features", (HttpContext context) =>
{
    IHttpConnectionFeature connection = context.Features.GetRequiredFeature<IHttpConnectionFeature>();
    IHttpMaxRequestBodySizeFeature? bodySize = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    return new
    {
        connection.ConnectionId,
        connection.LocalPort,
        MaxRequestBodySize = bodySize?.MaxRequestBodySize,
        BodySizeIsReadOnly = bodySize?.IsReadOnly,
        HasStarted = context.Response.HasStarted,
        Authenticated = context.User.Identity?.IsAuthenticated,
    };
});

const string CartCookie = "bookstore_cart";

app.MapPost("/api/cart/items", (CartItem item, HttpContext context, CartStore carts) =>
{
    if (!Guid.TryParse(context.Request.Cookies[CartCookie], out Guid cartId))
    {
        cartId = Guid.NewGuid();
        context.Response.Cookies.Append(CartCookie, cartId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(7),
            IsEssential = true,
        });
    }

    return carts.Add(cartId, item);
});

app.MapGet("/api/cart", (HttpRequest request, CartStore carts) =>
    Guid.TryParse(request.Cookies[CartCookie], out Guid cartId) ? carts.Get(cartId) : []);

app.MapGet("/api/institution/profile", (HttpContext context) =>
    TenantMiddleware.GetUnit(context));

app.MapPost("/api/institution/orders", (InstitutionOrderRequest order, HttpContext context, OrderAudit audit) =>
{
    InstitutionUnit unit = TenantMiddleware.GetUnit(context);
    string orderNumber = $"INS-{unit.Id}-{Random.Shared.Next(1000, 9999)}";
    audit.Record(orderNumber);

    context.Response.OnCompleted(() =>
    {
        app.Logger.LogInformation("Response for {OrderNumber} finished with {StatusCode}", orderNumber, context.Response.StatusCode);
        return Task.CompletedTask;
    });

    return TypedResults.Created($"/api/institution/orders/{orderNumber}", new { orderNumber, unit = unit.Name, order.Items });
});

app.MapGet("/api/books", ([AsParameters] BookQuery query) => query.Apply());

app.MapGet("/api/books/{isbn}", Results<Ok<Book>, NotFound> (Isbn isbn, BookCatalog catalog) =>
    catalog.Find(isbn) is { } book ? TypedResults.Ok(book) : TypedResults.NotFound());

app.MapGet("/api/books/lookup", (Isbn[] isbn, BookCatalog catalog) =>
    isbn.Select(i => catalog.Find(i)?.Title ?? $"{i}: not found"));

app.MapGet("/api/shipping/quote", (
    InstitutionContext institution,
    int quantity,
    [FromKeyedServices("express")] IShippingCalculator express,
    [FromKeyedServices("standard")] IShippingCalculator standard) =>
    new
    {
        Unit = institution.Unit.Name,
        Standard = standard.Calculate(quantity),
        Express = express.Calculate(quantity),
    });

app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
{
    AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
    return new { tokens.FormFieldName, tokens.RequestToken };
});

app.MapPost("/api/books/{isbn}/cover", (Isbn isbn, IFormFile file, [FromForm] string? altText) =>
    new { Isbn = isbn.Value, file.FileName, file.ContentType, file.Length, AltText = altText });

app.Run();

public record InstitutionOrderRequest(List<CartItem> Items);
