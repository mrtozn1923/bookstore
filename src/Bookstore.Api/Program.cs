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
// The document generated from the code; CI compares its operations with contract/ (post 09).
builder.Services.AddOpenApi();
// Page tokens are encrypted with Data Protection. The ephemeral key ring keeps the sample self-contained
// (tokens die on restart); several instances need a shared, persisted key ring.
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddSingleton<Paging>();
builder.Services.AddSingleton<CatalogData>();
builder.Services.AddSingleton<OrderData>();
builder.Services.AddSingleton<CartData>();
builder.Services.AddSingleton<InstitutionData>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IdempotencyStore>();
builder.Services.AddSingleton<OperationStore>();
builder.Services.AddHostedService<OperationWorker>();
builder.Services.AddSingleton<InvoiceData>();
builder.Services.AddSingleton<OrderBatchData>();

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
app.MapInvoiceEndpoints();
app.MapOperationEndpoints();
app.MapCartEndpoints();
app.MapInstitutionEndpoints();
// draft-ietf-httpapi-ratelimit-headers-11: 5 batches per minute per institution.
app.MapOrderBatchEndpoints(new QuotaPolicy("batches", 5, TimeSpan.FromMinutes(1),
    http => http.GetRouteValue("institutionId") as string ?? "", TimeProvider.System));
app.MapProblemEndpoints();
app.MapLegacyEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
