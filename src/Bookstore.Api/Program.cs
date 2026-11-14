using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Validation;

var builder = WebApplication.CreateBuilder(args);
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

var app = builder.Build();

app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    app.Logger.LogInformation("{Path} matched {Endpoint}", context.Request.Path, endpoint?.DisplayName ?? "(none)");
    await next();
});

app.MapControllers();

app.MapGet("/api/orders/{orderId:guid}", (Guid orderId) => new { orderId, Status = "Pending" });

app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> sources) =>
    sources.SelectMany(s => s.Endpoints)
           .OfType<RouteEndpoint>()
           .Select(e => e.RoutePattern.RawText));

app.Run();
