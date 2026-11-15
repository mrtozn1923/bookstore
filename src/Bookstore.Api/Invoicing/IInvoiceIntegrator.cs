namespace Bookstore.Invoicing;

// Port: Bookstore's own language for issuing an e-invoice.
public interface IInvoiceIntegrator
{
    Task<InvoiceSubmission> SubmitAsync(Invoice invoice, CancellationToken cancellationToken);
}

public sealed record Invoice(
    Guid Uuid,
    string Number,
    DateOnly IssueDate,
    string ReceiverTaxNumber,
    string ReceiverName,
    decimal Total,
    string Currency,
    IReadOnlyList<InvoiceItem> Items);

public sealed record InvoiceItem(string Title, int Quantity, decimal UnitPrice);

public sealed record InvoiceSubmission(Guid Uuid, string EnvelopeId, string Status, bool AlreadyReceived);

public enum InvoiceFailureKind
{
    // The integrator refused the document (bad tax number, missing field): fix the data, do not retry.
    Rejected,
    // Our credentials or certificate were not accepted.
    Unauthorized,
    // Network error, timeout or 5xx: the background job tries again later with the same UUID.
    Transient,
    // Maintenance or overload (503).
    Unavailable
}

public sealed class InvoiceIntegratorException(InvoiceFailureKind kind, string message, string? code = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public InvoiceFailureKind Kind { get; } = kind;

    // The integrator's own error code, kept for support tickets.
    public string? Code { get; } = code;
}
