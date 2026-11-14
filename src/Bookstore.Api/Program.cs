using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookstore.Api.Data;
using Bookstore.Api.Options;
using Bookstore.Api.Services;
using Bookstore.Api.Validation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/bookstore-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.ClientErrorMapping[StatusCodes.Status503ServiceUnavailable] = new ClientErrorData
        {
            Title = "Service Unavailable",
            Link = "https://tools.ietf.org/html/rfc7231#section-6.6.4"
        };
    });
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();
ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("en");
builder.Services.AddDbContext<BookstoreContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Bookstore")));
builder.Services.AddSingleton<SingletonOperation>();
builder.Services.AddScoped<ScopedOperation>();
builder.Services.AddTransient<TransientOperation>();
builder.Services.AddTransient<OperationReporter>();
builder.Services.AddSingleton<INotifier, EmailNotifier>();
builder.Services.AddSingleton<INotifier, SmsNotifier>();
builder.Services.AddOptions<CatalogOptions>()
    .Bind(builder.Configuration.GetSection(CatalogOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => o.Currency.Length == 3, "Currency must be a 3-letter ISO code.")
    .ValidateOnStart();

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.MapControllers();

app.MapGet("/api/orders/{orderId:guid}", (Guid orderId) => new { orderId, Status = "Pending" });

app.MapGet("/api/config/page-size", (IConfiguration config) => config.GetValue<int>("Catalog:PageSize"));

app.MapGet("/api/minimal/reporter", (OperationReporter reporter) => reporter.Report());

app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> sources) =>
    sources.SelectMany(s => s.Endpoints)
           .OfType<RouteEndpoint>()
           .Select(e => e.RoutePattern.RawText));

app.Run();
