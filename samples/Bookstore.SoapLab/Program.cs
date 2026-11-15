using System.ServiceModel;
using System.ServiceModel.Channels;

// Which bindings does the WCF client for .NET accept? Creating a channel is enough; nothing is sent.
(string Name, Func<Binding> Create)[] bindings =
[
    ("WSHttpBinding(Transport)", () => new WSHttpBinding(SecurityMode.Transport)),
    ("WSHttpBinding(TransportWithMessageCredential)", () => new WSHttpBinding(SecurityMode.TransportWithMessageCredential)),
    ("WSHttpBinding(Message)", () => new WSHttpBinding(SecurityMode.Message)),
    ("BasicHttpBinding(Message)", () => new BasicHttpBinding(BasicHttpSecurityMode.Message))
];

foreach (var (name, create) in bindings)
{
    try
    {
        var factory = new ChannelFactory<IProbe>(create(), new EndpointAddress("https://integrator.example.com/service.svc"));
        factory.CreateChannel();
        Console.WriteLine($"{name}: channel created");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{name}: {ex.GetType().Name}: {ex.Message}");
    }
}

[ServiceContract]
public interface IProbe
{
    [OperationContract]
    string Ping();
}
