using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using Bookstore.Invoicing.Integrator;
using Microsoft.Extensions.Options;

namespace Bookstore.Invoicing;

// One ChannelFactory for the application: creating it is expensive, creating channels from it is cheap.
public sealed class EInvoiceChannelFactory : IDisposable
{
    private readonly ChannelFactory<IEInvoiceServiceChannel> _factory;

    public EInvoiceChannelFactory(IOptions<EInvoiceIntegratorOptions> options, ILoggerFactory loggerFactory)
    {
        var settings = options.Value;
        _factory = new ChannelFactory<IEInvoiceServiceChannel>(CreateBinding(settings), new EndpointAddress(settings.Endpoint));

        switch (settings.Security)
        {
            case EInvoiceSecurity.UserName:
                _factory.Credentials.UserName.UserName = settings.UserName;
                _factory.Credentials.UserName.Password = settings.Password;
                break;
            case EInvoiceSecurity.Certificate:
                _factory.Credentials.ClientCertificate.Certificate =
                    X509CertificateLoader.LoadPkcs12FromFile(settings.ClientCertificatePath!, password: null);
                break;
        }

        if (settings.TrustedServerCertificatePath is { Length: > 0 } path)
        {
            // Pin one certificate; never switch validation off with "None".
            _factory.Credentials.ServiceCertificate.SslCertificateAuthentication = new X509ServiceCertificateAuthentication
            {
                CertificateValidationMode = X509CertificateValidationMode.Custom,
                CustomCertificateValidator = new PinnedCertificateValidator(X509CertificateLoader.LoadCertificateFromFile(path))
            };
        }

        if (settings.LogMessages)
        {
            _factory.Endpoint.EndpointBehaviors.Add(new SoapLoggingBehavior(loggerFactory.CreateLogger<SoapLoggingBehavior>()));
        }
    }

    public IEInvoiceServiceChannel CreateChannel() => _factory.CreateChannel();

    public void Dispose() => ((IDisposable)_factory).Dispose();

    // Built from our options instead of the generated GetBindingForEndpoint, so regenerating the proxy changes nothing here.
    public static Binding CreateBinding(EInvoiceIntegratorOptions settings)
    {
        var binding = settings.Security switch
        {
            EInvoiceSecurity.None => new BasicHttpBinding(BasicHttpSecurityMode.None),
            EInvoiceSecurity.UserName => new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential)
            {
                Security = { Message = { ClientCredentialType = BasicHttpMessageCredentialType.UserName } }
            },
            EInvoiceSecurity.Certificate => new BasicHttpBinding(BasicHttpSecurityMode.Transport)
            {
                Security = { Transport = { ClientCredentialType = HttpClientCredentialType.Certificate } }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(settings))
        };
        binding.OpenTimeout = TimeSpan.FromSeconds(5);
        binding.SendTimeout = settings.SendTimeout;
        // The generated code allows int.MaxValue; 1 MB is plenty for a receipt.
        binding.MaxReceivedMessageSize = 1024 * 1024;
        return binding;
    }

    private sealed class PinnedCertificateValidator(X509Certificate2 trusted) : X509CertificateValidator
    {
        public override void Validate(X509Certificate2 certificate)
        {
            if (!certificate.RawDataMemory.Span.SequenceEqual(trusted.RawDataMemory.Span))
            {
                throw new SecurityTokenValidationException($"Unexpected server certificate {certificate.Subject}.");
            }
        }
    }
}
