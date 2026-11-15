using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Bookstore.Invoicing;

// Logs SOAP envelopes for support cases, with tax numbers and passwords masked.
public sealed partial class SoapLoggingBehavior(ILogger logger) : IEndpointBehavior, IClientMessageInspector
{
    public object? BeforeSendRequest(ref Message request, IClientChannel channel)
    {
        // A Message can be read once: log a buffered copy and send a fresh one.
        var buffer = request.CreateBufferedCopy(int.MaxValue);
        request = buffer.CreateMessage();
        logger.LogInformation("SOAP request {Action}: {Envelope}", request.Headers.Action, Mask(Write(buffer)));
        return null;
    }

    public void AfterReceiveReply(ref Message reply, object? correlationState)
    {
        var buffer = reply.CreateBufferedCopy(int.MaxValue);
        reply = buffer.CreateMessage();
        logger.LogInformation("SOAP reply: {Envelope}", Mask(Write(buffer)));
    }

    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) =>
        clientRuntime.ClientMessageInspectors.Add(this);

    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

    public void Validate(ServiceEndpoint endpoint) { }

    // Message.ToString() prints "... stream ..." for a streamed body, so write the whole envelope.
    private static string Write(MessageBuffer buffer)
    {
        var text = new StringBuilder();
        using (var writer = XmlWriter.Create(text, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
        {
            buffer.CreateMessage().WriteMessage(writer);
        }

        return text.ToString();
    }

    private static string Mask(string envelope) => SensitiveElements().Replace(envelope, "<$1$2>***</$1$2>");

    [GeneratedRegex(@"<(\w+:)?(ReceiverTaxNumber|SenderTaxNumber|Password)\b[^>]*>[^<]*</\1?\2>")]
    private static partial Regex SensitiveElements();
}
