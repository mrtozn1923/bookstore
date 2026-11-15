using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Bookstore.FakeEInvoice;
using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Description;
using CoreWCF.IdentityModel.Tokens;
using CoreWCF.Security;
using Microsoft.AspNetCore.Server.Kestrel.Https;

// Usage: dotnet run --project src/Bookstore.FakeEInvoice -- [--mode Normal|Slow|Maintenance]
// Writes .local/fake-einvoice.cer (server certificate to trust) and .local/bookstore-client.pfx (test client certificate).
var settings = new FakeEInvoiceSettings
{
    Mode = Array.IndexOf(args, "--mode") is var i and >= 0 ? Enum.Parse<FakeEInvoiceMode>(args[i + 1], true) : FakeEInvoiceMode.Normal
};

var serverCertificate = CreateCertificate("CN=fake-einvoice.example.com", serverAuth: true);
var clientCertificate = CreateCertificate("CN=bookstore-test-client", serverAuth: false);
Directory.CreateDirectory(".local");
File.WriteAllBytes(".local/fake-einvoice.cer", serverCertificate.Export(X509ContentType.Cert));
File.WriteAllBytes(".local/bookstore-client.pfx", clientCertificate.Export(X509ContentType.Pkcs12));

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Listen(IPAddress.Loopback, 52410);
    kestrel.Listen(IPAddress.Loopback, 52411, listen => listen.UseHttps(https =>
    {
        https.ServerCertificate = serverCertificate;
        // The TLS layer asks for a client certificate; the /mtls endpoint decides whether it is the right one.
        https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
        https.AllowAnyClientCertificate();
    }));
});
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Services.AddSingleton(settings);
builder.Services.AddTransient<EInvoiceService>();
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

var app = builder.Build();

// Maintenance: a gateway in front of the service answers 503 before any SOAP processing.
app.Use(async (context, next) =>
{
    if (settings.Mode == FakeEInvoiceMode.Maintenance && HttpMethods.IsPost(context.Request.Method))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = "600";
        return;
    }

    await next();
});

// --show-wire: print the SOAP header as it arrived (password masked), to see what the client's security layer added.
if (args.Contains("--show-wire"))
{
    app.Use(async (context, next) =>
    {
        if (HttpMethods.IsPost(context.Request.Method))
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body, leaveOpen: true).ReadToEndAsync();
            context.Request.Body.Position = 0;
            var header = System.Text.RegularExpressions.Regex.Match(body, "<s:Header>.*</s:Header>", System.Text.RegularExpressions.RegexOptions.Singleline).Value;
            Console.WriteLine(System.Text.RegularExpressions.Regex.Replace(header, "(<o:Password[^>]*>)[^<]*", "$1***"));
        }

        await next();
    });
}

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<EInvoiceService>();
    // 1. No security: only for local tests.
    serviceBuilder.AddServiceEndpoint<EInvoiceService, IEInvoiceService>(new BasicHttpBinding(), "/EInvoiceService.svc");

    // 2. HTTPS + WS-Security UsernameToken in the SOAP header.
    var userName = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential);
    userName.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
    serviceBuilder.AddServiceEndpoint<EInvoiceService, IEInvoiceService>(userName, "/secure/EInvoiceService.svc");

    // 3. HTTPS + client certificate (mutual TLS).
    var certificate = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
    certificate.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
    serviceBuilder.AddServiceEndpoint<EInvoiceService, IEInvoiceService>(certificate, "/mtls/EInvoiceService.svc");

    serviceBuilder.ConfigureServiceHostBase<EInvoiceService>(host =>
    {
        host.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
        host.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new FakeUserValidator();
        host.Credentials.ClientCertificate.Authentication.CertificateValidationMode = CoreWCF.Security.X509CertificateValidationMode.Custom;
        host.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new ThumbprintValidator(clientCertificate.Thumbprint);
    });
});
var metadata = app.Services.GetRequiredService<ServiceMetadataBehavior>();
metadata.HttpGetEnabled = true;

Console.WriteLine($"Fake e-invoice integrator: http://127.0.0.1:52410/EInvoiceService.svc ({settings.Mode})");
Console.WriteLine("  https://127.0.0.1:52411/secure/EInvoiceService.svc (UsernameToken)");
Console.WriteLine("  https://127.0.0.1:52411/mtls/EInvoiceService.svc (client certificate)");
Console.WriteLine("  WSDL: http://127.0.0.1:52410/EInvoiceService.svc?wsdl");
app.Run();

static X509Certificate2 CreateCertificate(string subject, bool serverAuth)
{
    using var key = RSA.Create(2048);
    var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
        [serverAuth ? new Oid("1.3.6.1.5.5.7.3.1") : new Oid("1.3.6.1.5.5.7.3.2")], critical: false));
    if (serverAuth)
    {
        var names = new SubjectAlternativeNameBuilder();
        names.AddIpAddress(IPAddress.Loopback);
        names.AddDnsName("localhost");
        request.CertificateExtensions.Add(names.Build());
    }

    using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(30));
    return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pkcs12), null);
}

// Accepts exactly one client certificate (the test one written to .local/).
sealed class ThumbprintValidator(string thumbprint) : CoreWCF.IdentityModel.Selectors.X509CertificateValidator
{
    public override void Validate(X509Certificate2 certificate)
    {
        if (!string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenValidationException("Unknown client certificate.");
        }
    }
}
