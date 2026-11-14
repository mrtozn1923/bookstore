using System.Net;
using Bookstore.Api;
using Bookstore.Api.Carts;
using Bookstore.Api.Catalog;
using Bookstore.Api.Formatters;
using Bookstore.Api.Institutional;
using Bookstore.Api.Inventory;
using Bookstore.Api.Json;
using Bookstore.Api.Middleware;
using Bookstore.Api.OpenApi;
using Bookstore.Api.Ordering;
using Bookstore.Api.Shipping;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BookCatalog>();
builder.Services.AddSingleton<CartStore>();
builder.Services.AddSingleton<InstitutionDirectory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<OrderAudit>();
builder.Services.AddKeyedSingleton<IShippingCalculator, StandardShipping>("standard");
builder.Services.AddKeyedSingleton<IShippingCalculator, ExpressShipping>("express");
builder.Services.AddValidation();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddRequestLocalization(options =>
{
    string[] cultures = ["en-US", "tr-TR"];
    options.SetDefaultCulture(cultures[0])
        .AddSupportedCultures(cultures)
        .AddSupportedUICultures(cultures);
    options.ApplyCurrentCultureToResponseHeaders = true;
});
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // Kendi code'u olan problem'ler zaten yerelleştirilmiş geliyor
    if (context.ProblemDetails.Extensions.ContainsKey("code") || context.ProblemDetails.Status is not { } status)
    {
        return;
    }

    IStringLocalizer<SharedMessages> localizer =
        context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedMessages>>();
    LocalizedString title = localizer[$"Status{status}"];
    if (!title.ResourceNotFound)
    {
        context.ProblemDetails.Title = title;
    }
});
builder.Services.AddAntiforgery();
builder.Services.AddSingleton<OrderStore>();

builder.Services.ConfigureHttpJsonOptions(options => JsonDefaults.Apply(options.SerializerOptions));
builder.Services.AddControllers(options =>
    {
        options.ReturnHttpNotAcceptable = true;
        options.OutputFormatters.Add(new BookCsvOutputFormatter());
        options.FormatterMappings.SetMediaTypeMappingForFormat("csv", "text/csv");
    })
    .AddJsonOptions(options => JsonDefaults.Apply(options.JsonSerializerOptions))
    .AddXmlSerializerFormatters();

builder.Services.Configure<ExceptionHandlerOptions>(options =>
    // Development'ta binding hataları exception olarak gelir; 400'ü 500'e çevirme
    options.StatusCodeSelector = exception =>
        exception is BadHttpRequestException badRequest ? badRequest.StatusCode : StatusCodes.Status500InternalServerError);
builder.Services.AddResponseCompression(options =>
{
    // BREACH riskini değerlendirdik: yanıtlarda secret ile kullanıcı girdisi bir arada yok
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.AddRequestDecompression();
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(10) };
    options.AddPolicy("reports", TimeSpan.FromSeconds(2));
});
builder.Services.AddHeaderPropagation(options => options.Headers.Add("X-Correlation-Id"));
builder.Services.AddHttpClient("inventory", client =>
        client.BaseAddress = new Uri(builder.Configuration["Inventory:BaseUrl"]!))
    .AddHeaderPropagation();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<OrderOperationTransformer>();
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Loopback varsayılan olarak güvenilir; gerçek proxy'nin adresini ekle
    options.KnownProxies.Add(IPAddress.Parse("10.0.0.5"));
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRequestLocalization();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRewriter(new RewriteOptions()
    .AddRedirect("^api/v1/books/(.*)$", "api/books/$1", StatusCodes.Status301MovedPermanently)
    .AddRewrite("^catalog/(.*)$", "api/books/$1", skipRemainingRules: true));
app.UseResponseCompression();
app.UseRequestDecompression();
app.UseMiddleware<ResponseTimeMiddleware>();
app.UseMiddleware<RequestBodyLoggingMiddleware>();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();
app.UseAntiforgery();
app.UseRequestTimeouts();
app.UseHeaderPropagation();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

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
}).ExcludeFromDescription();

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
}).ExcludeFromDescription();

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

app.MapPost("/api/institution/orders", Results<Created<InstitutionOrderResponse>, ProblemHttpResult> (
    InstitutionOrderRequest order, HttpContext context, OrderAudit audit, BookCatalog catalog, IStringLocalizer<SharedMessages> localizer) =>
{
    if (order.Items.FirstOrDefault(i => !Isbn.TryParse(i.Isbn, null, out Isbn isbn) || catalog.Find(isbn) is null) is { } missing)
    {
        // type ve code makine için: hiçbir dilde değişmez
        return TypedResults.Problem(
            type: "https://bookstore.example.com/problems/isbn-not-found",
            title: localizer["IsbnNotFound.Title"],
            detail: localizer["IsbnNotFound.Detail", missing.Isbn],
            statusCode: StatusCodes.Status422UnprocessableEntity,
            extensions: new Dictionary<string, object?> { ["code"] = "catalog.isbn_not_found", ["isbn"] = missing.Isbn });
    }

    InstitutionUnit unit = TenantMiddleware.GetUnit(context);
    string orderNumber = $"INS-{unit.Id}-{Random.Shared.Next(1000, 9999)}";
    audit.Record(orderNumber);

    context.Response.OnCompleted(() =>
    {
        app.Logger.LogInformation("Response for {OrderNumber} finished with {StatusCode}", orderNumber, context.Response.StatusCode);
        return Task.CompletedTask;
    });

    return TypedResults.Created($"/api/institution/orders/{orderNumber}", new InstitutionOrderResponse(orderNumber, unit.Name, order.Items));
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
}).ExcludeFromDescription();

app.MapPost("/api/books/{isbn}/cover", (Isbn isbn, IFormFile file, [FromForm] string? altText) =>
    new { Isbn = isbn.Value, file.FileName, file.ContentType, file.Length, AltText = altText });

app.MapPatch("/api/books/{isbn}", BookPatchEndpoint.Patch)
    .RequireAuthorization(policy => policy.RequireRole("StoreStaff"));

app.MapOrderEndpoints();
app.MapInventoryEndpoints();
app.MapControllers();

app.Run();

public record InstitutionOrderResponse(string OrderNumber, string Unit, List<InstitutionOrderLine> Items);

