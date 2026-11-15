using System.Collections.Concurrent;
using CoreWCF;

namespace Bookstore.FakeEInvoice;

public enum FakeEInvoiceMode
{
    Normal,
    // Stores the invoice at once but answers the first request for it after 5 s.
    Slow,
    // HTTP 503 before the SOAP layer, like a gateway in front of the real service.
    Maintenance
}

public sealed class FakeEInvoiceSettings
{
    public FakeEInvoiceMode Mode { get; set; }
}

// A fake e-invoice integrator. Only rules a client must react to are imitated.
[ServiceBehavior(Namespace = IntegratorNamespace.Value)]
public sealed class EInvoiceService(FakeEInvoiceSettings settings, ILogger<EInvoiceService> logger) : IEInvoiceService
{
    private static readonly ConcurrentDictionary<string, InvoiceReceipt> Received = new();
    private static int _envelopeCounter = 4000;

    public async Task<InvoiceReceipt> SendInvoice(InvoiceDocument request)
    {
        if (request.ReceiverTaxNumber.Length is not (10 or 11) || !request.ReceiverTaxNumber.All(char.IsAsciiDigit))
        {
            throw Fault("INVALID_TAX_NUMBER", "Receiver tax number must be 10 or 11 digits.");
        }

        var response = new InvoiceReceipt
        {
            Uuid = request.Uuid,
            EnvelopeId = $"ENV-{Interlocked.Increment(ref _envelopeCounter)}",
            Status = "QUEUED",
            ReceivedAt = DateTime.UtcNow
        };
        // The same UUID twice is rejected; the client must ask for the status instead.
        if (!Received.TryAdd(request.Uuid, response))
        {
            throw Fault("DUPLICATE_UUID", $"Invoice {request.Uuid} was already received.");
        }

        logger.LogInformation("Received invoice {InvoiceNumber} ({Uuid}) -> {EnvelopeId}", request.InvoiceNumber, request.Uuid, response.EnvelopeId);
        // Slow: the invoice is already stored, only the answer is late.
        if (settings.Mode == FakeEInvoiceMode.Slow)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        return response;
    }

    public Task<InvoiceStatusInfo> GetInvoiceStatus(string uuid) =>
        Received.TryGetValue(uuid, out var invoice)
            ? Task.FromResult(new InvoiceStatusInfo { Uuid = uuid, EnvelopeId = invoice.EnvelopeId, Status = invoice.Status })
            : throw Fault("NOT_FOUND", $"Invoice {uuid} not found.");

    private static FaultException<IntegratorFault> Fault(string code, string message) =>
        new(new IntegratorFault { Code = code, Message = message }, new FaultReason(message), new FaultCode(code), null);
}

// Accepts one fake user; a real integrator checks its own user store.
public sealed class FakeUserValidator : CoreWCF.IdentityModel.Selectors.UserNamePasswordValidator
{
    public const string UserName = "bookstore-test";
    public const string Password = "example-password-change-me";

    public override ValueTask ValidateAsync(string userName, string password) =>
        userName == UserName && password == Password
            ? ValueTask.CompletedTask
            : throw new FaultException("Unknown user name or bad password.");
}
