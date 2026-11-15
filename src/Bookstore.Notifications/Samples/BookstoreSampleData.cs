using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Templates;

namespace Bookstore.Notifications.Samples;

// Deterministic sample data so every run of the posts' examples prints the same numbers
public static class BookstoreSampleData
{
    public static readonly IReadOnlyList<(string Isbn, string Title, decimal Price)> Books =
    [
        ("9780441172719", "Dune", 450.00m),
        ("9780141439587", "Emma", 240.00m),
        ("9780547928227", "The Hobbit", 380.00m),
        ("9780141439549", "Middlemarch", 320.00m),
        ("9780451524935", "Nineteen Eighty-Four", 260.00m),
        ("9780141439518", "Pride and Prejudice", 220.00m),
    ];

    private static readonly string[] Customers = ["ada", "alan", "grace", "linus", "barbara", "edsger"];

    // Same rows as MonthlySales, but generated one by one (unsorted) so nothing is held in memory
    public static IEnumerable<SalesRow> MonthlySalesStream(DateOnly month, int rowCount)
    {
        Random random = new(month.Year * 100 + month.Month);
        int days = DateTime.DaysInMonth(month.Year, month.Month);
        for (int i = 0; i < rowCount; i++)
        {
            (string isbn, string title, decimal price) = Books[random.Next(Books.Count)];
            DateTime placedAt = new DateTime(month.Year, month.Month, random.Next(1, days + 1), random.Next(0, 24), random.Next(0, 60), 0, DateTimeKind.Utc);
            yield return new SalesRow(1000 + i, placedAt, $"{Customers[random.Next(Customers.Length)]}@example.com", isbn, title, random.Next(1, 4), price);
        }
    }

    public static MonthlySalesReport MonthlySales(DateOnly month, int rowCount)
    {
        Random random = new(month.Year * 100 + month.Month);
        int days = DateTime.DaysInMonth(month.Year, month.Month);
        List<SalesRow> rows = new(rowCount);
        for (int i = 0; i < rowCount; i++)
        {
            (string isbn, string title, decimal price) = Books[random.Next(Books.Count)];
            DateTime placedAt = new DateTime(month.Year, month.Month, random.Next(1, days + 1), random.Next(0, 24), random.Next(0, 60), 0, DateTimeKind.Utc);
            rows.Add(new SalesRow(1000 + i, placedAt, $"{Customers[random.Next(Customers.Length)]}@example.com", isbn, title, random.Next(1, 4), price));
        }

        rows.Sort((a, b) => a.PlacedAtUtc.CompareTo(b.PlacedAtUtc));
        return new MonthlySalesReport(month, rows);
    }

    public static ApprovalReminderModel ApprovalReminder(string approverName) => new(
        ApproverName: approverName,
        InstitutionName: "Hilltop Primary School",
        Pending:
        [
            new PendingApproval(2101, "Emre", "Science Department", 9_120.00m, new DateTime(2026, 11, 10, 9, 30, 0, DateTimeKind.Utc), null),
            new PendingApproval(2107, "Ayse", "Library", 4_560.00m, new DateTime(2026, 11, 11, 14, 5, 0, DateTimeKind.Utc),
                "<b>Urgent</b> for the reading week <script>alert('x')</script>"),
        ],
        ApprovalsUrl: "https://bookstore.example.com/institution/approvals");

    public static InvoiceModel Invoice(int orderId) => new(
        InvoiceNumber: $"BKS-2026-{orderId:D6}",
        OrderId: orderId,
        IssueDate: new DateOnly(2026, 11, 14),
        CustomerEmail: "ada@example.com",
        BillingAddress: new PostalAddress("Ada Lovelace", "Example Street 12", "Istanbul", "34000", "Turkiye"),
        Lines:
        [
            new InvoiceLine("9780441172719", "Dune", 2, 450.00m, 0.10m),
            new InvoiceLine("9780547928227", "The Hobbit", 1, 380.00m, 0.10m),
            new InvoiceLine("9780141439587", "Emma", 1, 240.00m, 0.10m),
            new InvoiceLine("GIFT-WRAP", "Gift wrapping", 1, 25.00m, 0.20m),
        ]);
}
