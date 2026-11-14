using Bookstore.Api.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ShippingOptions>(builder.Configuration.GetSection(ShippingOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<PaymentWebhookOptions>(builder.Configuration.GetSection(PaymentWebhookOptions.SectionName));

var app = builder.Build();

// Reports which settings are present, never their values.
app.MapGet("/api/info", (IHostEnvironment env, IConfiguration config, IOptions<ShippingOptions> shipping) => new
{
    Environment = env.EnvironmentName,
    shipping.Value.FreeShippingThreshold,
    Secrets = new Dictionary<string, bool>
    {
        ["ConnectionStrings:Bookstore"] = !string.IsNullOrEmpty(config.GetConnectionString("Bookstore")),
        ["Jwt:SigningKey"] = !string.IsNullOrEmpty(config["Jwt:SigningKey"]),
        ["Email:ApiKey"] = !string.IsNullOrEmpty(config["Email:ApiKey"]),
        ["Payments:Webhook:Secret"] = !string.IsNullOrEmpty(config["Payments:Webhook:Secret"])
    }
});

app.Run();
