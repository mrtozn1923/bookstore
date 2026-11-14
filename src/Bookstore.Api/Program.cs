using System.Text.Json.Serialization;
using Bookstore.Api.Catalog;
using Bookstore.Api.Legacy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Absent values are omitted, never sent as null (ADR-0001).
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<CatalogData>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapBookEndpoints();
app.MapLegacyEndpoints();

app.Run();
