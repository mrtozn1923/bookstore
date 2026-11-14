using System.Text.Json.Serialization;
using Bookstore.Api.Catalog;
using Bookstore.Api.Common;
using Bookstore.Api.Identity;
using Bookstore.Api.Institutional;
using Bookstore.Api.Legacy;
using Bookstore.Api.Ordering;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;

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
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemCatalog.Normalize);
builder.Services.AddRequestLocalization(options =>
{
    options.SetDefaultCulture("en").AddSupportedCultures("en", "tr").AddSupportedUICultures("en", "tr");
    options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
    options.ApplyCurrentCultureToResponseHeaders = true;
});
builder.Services.AddValidation();
// Page tokens are encrypted with Data Protection. The ephemeral key ring keeps the sample self-contained
// (tokens die on restart); several instances need a shared, persisted key ring.
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddSingleton<Paging>();
builder.Services.AddSingleton<CatalogData>();
builder.Services.AddSingleton<OrderData>();
builder.Services.AddSingleton<CartData>();
builder.Services.AddSingleton<InstitutionData>();

WebApplication app = builder.Build();

app.UseRequestLocalization();
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    // Unreadable JSON is the client's fault in every environment: 400, not 500.
    StatusCodeSelector = exception => exception is BadHttpRequestException bad ? bad.StatusCode : 500,
});
app.UseStatusCodePages();
app.UseAuthentication();
// Resolves ?api-version= (ADR-0004) and writes Deprecation/Sunset for old versions.
app.UseApiVersions();
app.UseAuthorization();

app.MapBookEndpoints();
app.MapOrderEndpoints();
app.MapCartEndpoints();
app.MapInstitutionEndpoints();
app.MapProblemEndpoints();
app.MapLegacyEndpoints();

app.Run();
