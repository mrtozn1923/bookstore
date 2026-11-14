using System.Text.Json.Serialization;
using Bookstore.Api.Catalog;
using Bookstore.Api.Identity;
using Bookstore.Api.Institutional;
using Bookstore.Api.Legacy;
using Bookstore.Api.Ordering;
using Microsoft.AspNetCore.Authentication;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Absent values are omitted, never sent as null (ADR-0001).
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddAuthentication(DemoBearerHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DemoBearerHandler>(DemoBearerHandler.SchemeName, null);
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<CatalogData>();
builder.Services.AddSingleton<OrderData>();
builder.Services.AddSingleton<CartData>();
builder.Services.AddSingleton<InstitutionData>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapBookEndpoints();
app.MapOrderEndpoints();
app.MapCartEndpoints();
app.MapInstitutionEndpoints();
app.MapLegacyEndpoints();

app.Run();
