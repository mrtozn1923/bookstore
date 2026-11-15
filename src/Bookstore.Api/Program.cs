using System.Text.Json.Serialization;
using Bookstore.Api;
using Bookstore.Api.Data;
using Bookstore.Api.Endpoints;
using Bookstore.Api.Integrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BookstoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Bookstore")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = "bookstore-api";
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CatalogAdmin", policy => policy.RequireRole("admin"))
    .AddPolicy("InstitutionBuyer", policy => policy.RequireRole("institution-buyer"));

builder.Services.AddOptions<PaymentOptions>().BindConfiguration("Payments");
builder.Services.AddOptions<ShippingOptions>().BindConfiguration("Shipping");
builder.Services.AddOptions<SmtpOptions>().BindConfiguration("Smtp");

builder.Services.AddHttpClient<PaymentClient>((sp, client) =>
    {
        PaymentOptions payments = sp.GetRequiredService<IOptions<PaymentOptions>>().Value;
        client.BaseAddress = payments.BaseUrl;
        client.DefaultRequestHeaders.Add("X-Api-Key", payments.ApiKey);
    })
    .AddStandardResilienceHandler(builder.Configuration.GetSection("Payments:Resilience"));

builder.Services.AddHttpClient<ShippingClient>((sp, client) =>
    client.BaseAddress = sp.GetRequiredService<IOptions<ShippingOptions>>().Value.BaseUrl);

builder.Services.AddScoped<IOrderEmailSender, SmtpOrderEmailSender>();
builder.Services.AddHostedService<LowStockReportService>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapBookEndpoints();
app.MapOrderEndpoints();
app.MapCouponEndpoints();
app.MapInstitutionalEndpoints();

app.Run();
