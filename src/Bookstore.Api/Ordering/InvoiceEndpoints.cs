using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Bookstore.Api.Common;
using Bookstore.Api.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api.Ordering;

public sealed record Invoice(Guid Id, Guid OrderId, string Number, byte[] Pdf);

public sealed class InvoiceData
{
    private readonly ConcurrentDictionary<Guid, Invoice> _invoices = new();

    public void Add(Invoice invoice) => _invoices[invoice.Id] = invoice;

    public Invoice? Find(Guid orderId, Guid invoiceId) =>
        _invoices.TryGetValue(invoiceId, out Invoice? invoice) && invoice.OrderId == orderId ? invoice : null;
}

public static class InvoiceEndpoints
{
    public const string Kind = "invoice-generation";

    // Institutions without a tax number on file cannot get an invoice (the job finds out half way).
    private static readonly string[] InstitutionsWithTaxNumber = ["lakeside-school"];

    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder invoices = app.MapGroup("/orders/{orderId:guid}/invoices").RequireAuthorization();
        invoices.MapPost("", GenerateInvoice).WithName("generateInvoice");
        invoices.MapGet("/{invoiceId:guid}", GetInvoice).WithName("getInvoice");
        return app;
    }

    // Generating the PDF takes seconds: 202 Accepted and a status monitor instead of a blocked request.
    private static IResult GenerateInvoice(
        Guid orderId, HttpContext http, ClaimsPrincipal principal, OrderData orders, InvoiceData invoices,
        OperationStore operations)
    {
        DemoUser user = principal.Current();
        if (orders.Find(orderId) is not { } order || !OrderData.CanSee(user, order))
        {
            return ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/orders/{orderId}"]);
        }

        if (order.Status is OrderStatus.Pending or OrderStatus.Cancelled)
        {
            return ProblemCatalog.Create(ProblemCatalog.OrderNotInvoiceable, [orderId, order.Status]);
        }

        // One invoice per order: a second request points at the running or finished operation.
        if (operations.FindActive(Kind, orderId.ToString()) is { } existing)
        {
            return ProblemCatalog.Create(ProblemCatalog.InvoiceAlreadyRequested, [orderId],
                new Dictionary<string, object?> { ["operationLocation"] = $"/operations/{existing.Id}" });
        }

        Operation operation = operations.Start(Kind, user.Name, orderId.ToString(), async (progress, cancellationToken) =>
        {
            for (int step = 1; step <= 4; step++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);
                if (step == 2 && order.InstitutionId is { } institution && !InstitutionsWithTaxNumber.Contains(institution))
                {
                    throw new OperationFailedException(ProblemCatalog.InvoiceDetailsMissing, institution);
                }

                progress(step * 25);
            }

            Invoice invoice = new(Guid.CreateVersion7(), order.Id, $"INV-2026-{order.Id.ToString()[..8].ToUpperInvariant()}",
                MinimalPdf.Create($"Invoice for order {order.Id} - total {order.Total.ToString("0.00", CultureInfo.InvariantCulture)} TRY"));
            invoices.Add(invoice);
            return $"/orders/{order.Id}/invoices/{invoice.Id}";
        });
        return OperationStore.Accepted(http, operation);
    }

    private static Results<FileContentHttpResult, ProblemHttpResult> GetInvoice(
        Guid orderId, Guid invoiceId, ClaimsPrincipal principal, OrderData orders, InvoiceData invoices) =>
        orders.Find(orderId) is { } order && OrderData.CanSee(principal.Current(), order) &&
        invoices.Find(orderId, invoiceId) is { } invoice
            ? TypedResults.File(invoice.Pdf, "application/pdf", $"{invoice.Number}.pdf")
            : ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/orders/{orderId}/invoices/{invoiceId}"]);
}

// A one-page PDF with a single line of text; enough for the sample.
public static class MinimalPdf
{
    public static byte[] Create(string text)
    {
        string content = $"BT /F1 12 Tf 72 720 Td ({text.Replace("(", "\\(").Replace(")", "\\)")}) Tj ET";
        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        ];
        StringBuilder pdf = new("%PDF-1.4\n");
        List<int> offsets = [];
        for (int i = 0; i < objects.Length; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        int xref = pdf.Length;
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (int offset in offsets)
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
