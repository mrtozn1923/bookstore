namespace Aspire.Hosting.ApplicationModel;

// A typed resource instead of AddContainer: other resources can WithReference it and get an SMTP connection string
public sealed class MailpitResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string SmtpEndpointName = "smtp";

    private EndpointReference? smtpEndpoint;

    public EndpointReference SmtpEndpoint => smtpEndpoint ??= new(this, SmtpEndpointName);

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"smtp://{SmtpEndpoint.Property(EndpointProperty.HostAndPort)}");
}
