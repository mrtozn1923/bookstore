using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Bookstore.Crypto.Pki;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Bookstore.Api;

public sealed record StockReservationRequest(Guid OrderId, string Isbn, int Quantity);
public sealed record StockReservation(Guid ReservationId, Guid OrderId, string Isbn, int Quantity, string RequestedBy);

public static class ApiHost
{
    public const string ServiceClaim = "bookstore:service";

    public static WebApplication Build(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        string root = builder.Environment.ContentRootPath;
        X509Certificate2Collection trustAnchors = ServiceCertificates.LoadAll(Path.Combine(root, builder.Configuration["Pki:TrustAnchors"]!));
        X509Certificate2Collection intermediates = ServiceCertificates.LoadAll(Path.Combine(root, builder.Configuration["Pki:Intermediates"]!));

        builder.WebHost.ConfigureKestrel(kestrel => kestrel.ConfigureHttpsDefaults(https =>
        {
            https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            // TLS only asks for a certificate; trust is decided by the certificate handler below, against our own root
            https.AllowAnyClientCertificate();
        }));

        builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate(options =>
            {
                options.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
                options.CustomTrustStore = trustAnchors;
                options.AdditionalChainCertificates = intermediates;
                options.AllowedCertificateTypes = CertificateTypes.Chained;
                options.RevocationMode = X509RevocationMode.NoCheck; // dev PKI publishes no CRL
                options.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        string? service = ServiceNameOf(context.ClientCertificate);
                        if (service is null)
                        {
                            context.Fail("No bookstore service name in the certificate SAN.");
                            return Task.CompletedTask;
                        }

                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
                            [new Claim(ServiceClaim, service), new Claim(ClaimTypes.Name, service)], context.Scheme.Name));
                        context.Success();
                        return Task.CompletedTask;
                    },
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy("OrderingService", policy => policy.RequireClaim(ServiceClaim, "ordering"));

        WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/internal/whoami", (ClaimsPrincipal user) => new { service = user.FindFirstValue(ServiceClaim) });

        app.MapPost("/internal/stock-reservations", (StockReservationRequest request, ClaimsPrincipal user) =>
        {
            StockReservation reservation = new(Guid.CreateVersion7(), request.OrderId, request.Isbn, request.Quantity,
                user.FindFirstValue(ServiceClaim)!);
            return Results.Created($"/internal/stock-reservations/{reservation.ReservationId}", reservation);
        }).RequireAuthorization("OrderingService");

        return app;
    }

    // "<service>.bookstore.test" in the SAN -> "<service>"
    public static string? ServiceNameOf(X509Certificate2 certificate)
    {
        X509SubjectAlternativeNameExtension? san = certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().FirstOrDefault();
        string suffix = "." + DevPki.ServiceDomain;
        return san?.EnumerateDnsNames()
            .Where(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            .Select(name => name[..^suffix.Length])
            .FirstOrDefault();
    }
}
