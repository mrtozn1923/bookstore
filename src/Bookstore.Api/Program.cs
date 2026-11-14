using Bookstore.Api.Books;
using Bookstore.Data;
using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<BookstoreContext>("bookstoredb");

// The cache is optional: without Redis the catalog must still answer, and quickly
builder.AddRedisOutputCache("redis",
    settings => settings.DisableHealthChecks = true,
    options => options.BacklogPolicy = BacklogPolicy.FailFast);

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer(serviceName: "keycloak", realm: "bookstore", options =>
    {
        options.Audience = "bookstore-catalog";
        // Keep claim names as Keycloak writes them; otherwise "roles" is renamed and RequireRole finds nothing
        options.MapInboundClaims = false;
        options.TokenValidationParameters.RoleClaimType = "roles";
        // Locally Keycloak is reached over http; production sets an https Authority
        if (builder.Environment.IsDevelopment())
        {
            options.RequireHttpsMetadata = false;
        }
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("BackOffice", policy => policy.RequireRole("StoreStaff"));

WebApplication app = builder.Build();

app.UseOutputCache();

app.MapDefaultEndpoints();
app.MapBookEndpoints();

app.Run();
