using Bookstore.Api;
using Bookstore.Api.Data;
using Bookstore.Api.Integrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.E2E.Host;

// Tip argümanı API assembly'sinden bir tip olmalı; bu projenin kendi Program'ı değil
public sealed class E2EApiFactory(string connectionString, FakeProviders providers, E2ETokenIssuer tokens)
    : WebApplicationFactory<BookstoreDbContext>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("E2E");
        builder.UseSetting("ConnectionStrings:Bookstore", connectionString);
        builder.UseSetting("Payments:BaseUrl", $"{providers.Payments.Url}/");
        builder.UseSetting("Payments:ApiKey", "e2e");
        builder.UseSetting("Shipping:BaseUrl", $"{providers.Shipping.Url}/");

        builder.ConfigureTestServices(services =>
        {
            // Rapor job'ı E2E'nin konusu değil; hata verirse host'u ve Kestrel'i durdurur
            ServiceDescriptor worker = services.Single(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(LowStockReportService));
            services.Remove(worker);

            services.AddScoped<IOrderEmailSender, LoggingOrderEmailSender>();

            // JWT doğrulaması gerçek; yalnız token'ı imzalayan taraf değişiyor
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = E2ETokenIssuer.Issuer,
                    ValidAudience = "bookstore-api",
                    IssuerSigningKey = tokens.SigningKey
                };
            });
            services.AddSingleton(tokens);
            services.AddSingleton<IStartupFilter, E2ETokenEndpoint>();
        });
    }
}
