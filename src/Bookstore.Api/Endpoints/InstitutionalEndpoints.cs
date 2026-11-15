using System.Globalization;
using System.Security.Claims;
using System.Text;
using Bookstore.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Endpoints;

public sealed record BulkOrderLine(string Isbn, int Quantity);

public sealed record PlaceBulkOrderRequest(List<BulkOrderLine> Lines);

public static class InstitutionalEndpoints
{
    public const int MinimumCopies = 20;
    public const int InstitutionalPercentOff = 10;

    public static IEndpointRouteBuilder MapInstitutionalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/institutional/orders", PlaceBulkOrderAsync)
            .WithTags("Institutional")
            .RequireAuthorization("InstitutionBuyer");

        app.MapGet("/api/orders/{id:int}/invoice", GetInvoiceAsync)
            .WithTags("Orders")
            .RequireAuthorization();
        return app;
    }

    // Kurumsal sipariş faturayla ödenir: kart çekimi yok, %10 indirim, en az 20 adet
    private static async Task<Results<Created<OrderDto>, ValidationProblem, ProblemHttpResult>> PlaceBulkOrderAsync(
        PlaceBulkOrderRequest request, ClaimsPrincipal user, BookstoreDbContext db, TimeProvider clock, CancellationToken ct)
    {
        int copies = request.Lines.Sum(l => l.Quantity);
        if (request.Lines.Count == 0 || request.Lines.Any(l => l.Quantity <= 0) || copies < MinimumCopies)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["lines"] = [$"Institutional orders need at least {MinimumCopies} copies."]
            });
        }

        List<string> isbns = request.Lines.Select(l => l.Isbn).ToList();
        Dictionary<string, Book> books = await db.Books.Where(b => isbns.Contains(b.Isbn)).ToDictionaryAsync(b => b.Isbn, ct);
        foreach (BulkOrderLine line in request.Lines)
        {
            if (!books.TryGetValue(line.Isbn, out Book? book) || book.Stock < line.Quantity)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Insufficient stock",
                    detail: $"ISBN {line.Isbn} does not have {line.Quantity} copies in stock.");
            }
        }

        Order order = new()
        {
            Reference = $"INS-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            CustomerId = int.Parse(user.FindFirstValue("customer_id")!),
            CreatedAt = clock.GetUtcNow(),
            Status = OrderStatus.Placed,
            Items = request.Lines.Select(l => new OrderItem
            {
                BookId = books[l.Isbn].Id,
                Quantity = l.Quantity,
                UnitPrice = books[l.Isbn].Price
            }).ToList()
        };
        decimal subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        order.Total = Math.Round(subtotal * (100 - InstitutionalPercentOff) / 100m, 2, MidpointRounding.AwayFromZero);
        foreach (BulkOrderLine line in request.Lines)
        {
            books[line.Isbn].Stock -= line.Quantity;
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/orders/{order.Id}",
            new OrderDto(order.Id, order.Reference, order.Status, order.Total, order.TrackingNumber, order.CreatedAt));
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetInvoiceAsync(
        int id, ClaimsPrincipal user, BookstoreDbContext db, CancellationToken ct)
    {
        int customerId = int.Parse(user.FindFirstValue("customer_id")!);
        Order? order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        byte[] pdf = InvoicePdf.Create($"Invoice {order.Reference} - Total TRY {order.Total.ToString("0.00", CultureInfo.InvariantCulture)}");
        return TypedResults.File(pdf, "application/pdf", $"invoice-{order.Reference}.pdf");
    }
}

// Tek satır metinli, tek sayfalık bir PDF; örnekteki indirilebilir fatura için yeterli
public static class InvoicePdf
{
    public static byte[] Create(string text)
    {
        string content = $"BT /F1 14 Tf 72 760 Td ({text}) Tj ET";
        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        ];

        StringBuilder pdf = new("%PDF-1.4\n");
        List<int> offsets = [];
        for (int i = 0; i < objects.Length; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        int xref = pdf.Length;
        pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (int offset in offsets)
        {
            pdf.Append($"{offset:D10} 00000 n \n");
        }
        pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
