using System.ServiceModel;
using System.ServiceModel.Security;
using Bookstore.Invoicing.Integrator;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Polly.Timeout;

namespace Bookstore.Invoicing;

// Adapter: the generated SOAP proxy stays behind this class; its types and exceptions never leave it.
public sealed class SoapInvoiceIntegrator(
    EInvoiceChannelFactory channels,
    ResiliencePipelineProvider<string> pipelines,
    IOptions<EInvoiceIntegratorOptions> options,
    ILogger<SoapInvoiceIntegrator> logger) : IInvoiceIntegrator
{
    public const string PipelineName = "einvoice";

    public async Task<InvoiceSubmission> SubmitAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var document = ToDocument(invoice, options.Value.SenderTaxNumber);
        var pipeline = pipelines.GetPipeline(PipelineName);
        try
        {
            try
            {
                // The UUID makes the call idempotent: a retry after a timeout cannot create a second invoice.
                var receipt = await pipeline.ExecuteAsync(token => CallAsync(c => c.SendInvoiceAsync(document), token), cancellationToken);
                return new InvoiceSubmission(invoice.Uuid, receipt.EnvelopeId, receipt.Status, AlreadyReceived: false);
            }
            catch (FaultException<IntegratorFault> fault) when (fault.Detail.Code == "DUPLICATE_UUID")
            {
                // An earlier attempt reached the integrator; ask what it has instead of failing.
                var status = await pipeline.ExecuteAsync(token => CallAsync(c => c.GetInvoiceStatusAsync(document.Uuid), token), cancellationToken);
                logger.LogInformation("Invoice {Uuid} was already received as {EnvelopeId}", invoice.Uuid, status.EnvelopeId);
                return new InvoiceSubmission(invoice.Uuid, status.EnvelopeId, status.Status, AlreadyReceived: true);
            }
        }
        catch (Exception ex) when (Translate(ex) is { } translated)
        {
            throw translated;
        }
    }

    // Retry only what can succeed a moment later. FaultException derives from CommunicationException, so order matters.
    public static bool IsTransient(Exception exception) => exception switch
    {
        FaultException => false,
        // Wrong password, untrusted or expired certificate: the next attempt fails the same way.
        MessageSecurityException or SecurityAccessDeniedException or SecurityNegotiationException => false,
        ServerTooBusyException => false,
        TimeoutException or CommunicationException => true,
        _ => false
    };

    private async ValueTask<T> CallAsync<T>(Func<IEInvoiceServiceChannel, Task<T>> operation, CancellationToken token)
    {
        var channel = channels.CreateChannel();
        // WCF operations take no CancellationToken: aborting the channel is the only way to stop a call.
        using var registration = token.Register(channel.Abort);
        try
        {
            var result = await operation(channel);
            await Task.Factory.FromAsync(channel.BeginClose, channel.EndClose, null);
            return result;
        }
        catch (CommunicationException) when (token.IsCancellationRequested)
        {
            channel.Abort();
            throw new OperationCanceledException(token);
        }
        catch
        {
            // A faulted channel cannot be closed or reused, only aborted.
            channel.Abort();
            throw;
        }
    }

    private static InvoiceIntegratorException? Translate(Exception exception) => exception switch
    {
        FaultException<IntegratorFault> fault =>
            new(InvoiceFailureKind.Rejected, fault.Detail.Message, fault.Detail.Code, fault),
        // An untyped fault: the integrator failed on its side; the background job tries again later.
        FaultException =>
            new(InvoiceFailureKind.Transient, "The integrator returned an unexpected SOAP fault.", innerException: exception),
        MessageSecurityException or SecurityAccessDeniedException or SecurityNegotiationException =>
            new(InvoiceFailureKind.Unauthorized, "Credentials or certificates were not accepted.", innerException: exception),
        ServerTooBusyException =>
            new(InvoiceFailureKind.Unavailable, "The integrator is unavailable.", innerException: exception),
        TimeoutRejectedException or TimeoutException or CommunicationException =>
            new(InvoiceFailureKind.Transient, "The integrator did not answer in time.", innerException: exception),
        _ => null
    };

    private static InvoiceDocument ToDocument(Invoice invoice, string senderTaxNumber) => new()
    {
        Uuid = invoice.Uuid.ToString(),
        InvoiceNumber = invoice.Number,
        IssueDate = invoice.IssueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        SenderTaxNumber = senderTaxNumber,
        ReceiverTaxNumber = invoice.ReceiverTaxNumber,
        ReceiverName = invoice.ReceiverName,
        PayableAmount = invoice.Total,
        CurrencyCode = invoice.Currency,
        Lines = [.. invoice.Items.Select(i => new InvoiceLine { ItemName = i.Title, Quantity = i.Quantity, UnitPrice = i.UnitPrice })]
    };
}
