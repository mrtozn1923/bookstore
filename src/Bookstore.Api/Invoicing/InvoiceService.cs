using System.Collections.Concurrent;
using Bookstore.Institutional;
using Bookstore.Ordering;

namespace Bookstore.Invoicing;

public sealed class IssuedInvoice
{
    public required int OrderId { get; init; }
    public required Guid Uuid { get; init; }
    public required string Number { get; init; }
    public string? EnvelopeId { get; set; }
    public string? Status { get; set; }
}

// In-memory, like the orders. In Bookstore the UUID and number are written to the Invoices table before the first call.
public sealed class InvoiceStore
{
    private readonly ConcurrentDictionary<int, IssuedInvoice> _invoices = new();
    private int _sequence;

    public IssuedInvoice GetOrCreate(int orderId, int year) => _invoices.GetOrAdd(orderId, id => new IssuedInvoice
    {
        OrderId = id,
        Uuid = Guid.NewGuid(),
        // e-invoice numbers: 3-letter prefix + year + 9-digit sequence
        Number = $"BKS{year}{Interlocked.Increment(ref _sequence):D9}"
    });
}

public sealed class InvoiceService(OrderStore orders, InvoiceStore invoices, InstitutionDirectory institutions,
    IInvoiceIntegrator integrator, TimeProvider time)
{
    public async Task<IssuedInvoice?> IssueAsync(int orderId, CancellationToken cancellationToken)
    {
        if (orders.Find(orderId) is not { InstitutionId: { } institutionId } order
            || institutions.Find(institutionId) is not { } institution)
        {
            return null;
        }

        var now = time.GetUtcNow();
        // Same UUID on every attempt, also when the API call itself is repeated.
        var issued = invoices.GetOrCreate(order.Id, now.Year);
        if (issued.EnvelopeId is not null)
        {
            return issued;
        }

        var submission = await integrator.SubmitAsync(new Invoice(
            issued.Uuid, issued.Number, DateOnly.FromDateTime(now.UtcDateTime),
            institution.TaxNumber, institution.Name, order.ItemsTotal, order.Currency,
            [new InvoiceItem($"Books, order {order.Id}", 1, order.ItemsTotal)]), cancellationToken);
        issued.EnvelopeId = submission.EnvelopeId;
        issued.Status = submission.Status;
        return issued;
    }
}
