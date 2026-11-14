using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.Billing.Numbering;

public enum InvoiceStatus { Draft = 1, Approved = 2, Cancelled = 3 }

public class Invoice
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? InvoiceNumber { get; set; }
}

public class InvoiceNumberCounter
{
    public string Series { get; set; } = "";
    public short Year { get; set; }
    public int LastNumber { get; set; }
}

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceNumberCounter> InvoiceNumberCounters => Set<InvoiceNumberCounter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(b =>
        {
            b.Property(i => i.TotalAmount).HasPrecision(19, 4);
            b.Property(i => i.InvoiceNumber).HasMaxLength(16).IsUnicode(false).IsFixedLength();
            b.HasIndex(i => i.InvoiceNumber).IsUnique();
        });
        modelBuilder.Entity<InvoiceNumberCounter>(b =>
        {
            b.HasKey(c => new { c.Series, c.Year });
            b.Property(c => c.Series).HasMaxLength(3).IsUnicode(false).IsFixedLength();
        });
    }
}

public sealed class InvoiceNumberAllocator(BookstoreContext db)
{
    // Must run inside the caller's transaction; the row lock is held until commit
    public async Task<int> NextAsync(string series, short year, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Invoice numbers must be allocated inside a transaction.");

        List<int> result = await db.Database.SqlQuery<int>($"""
            IF NOT EXISTS (SELECT 1 FROM dbo.InvoiceNumberCounters WITH (UPDLOCK, HOLDLOCK)
                           WHERE Series = {series} AND [Year] = {year})
                INSERT dbo.InvoiceNumberCounters (Series, [Year], LastNumber) VALUES ({series}, {year}, 0);

            UPDATE dbo.InvoiceNumberCounters
            SET LastNumber = LastNumber + 1
            OUTPUT inserted.LastNumber AS Value
            WHERE Series = {series} AND [Year] = {year};
            """).ToListAsync(ct);
        return result.Single();
    }
}

public static partial class InvoiceNumber
{
    public static string Format(string series, int year, int sequence)
    {
        if (!SeriesPattern().IsMatch(series))
            throw new ArgumentException($"Series must be 3 characters [A-Z0-9]: '{series}'.", nameof(series));
        if (sequence is < 1 or > 999_999_999)
            throw new ArgumentOutOfRangeException(nameof(sequence));
        return $"{series}{year:D4}{sequence:D9}";
    }

    [GeneratedRegex("^[A-Z0-9]{3}$")]
    private static partial Regex SeriesPattern();
}

public sealed class InvoiceApprovalService(BookstoreContext db, InvoiceNumberAllocator numbers)
{
    public const string Series = "BKS";

    public async Task<string> ApproveAsync(int invoiceId, Func<Task>? beforeCommit = null, Func<Task>? beforeNumber = null, CancellationToken ct = default)
    {
        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);

        Invoice invoice = await db.Invoices.SingleAsync(i => i.Id == invoiceId, ct);
        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException($"Invoice {invoiceId} is {invoice.Status}.");

        if (beforeNumber is not null) await beforeNumber();

        // The year comes from the invoice date, not from the approval time
        int seq = await numbers.NextAsync(Series, (short)invoice.InvoiceDate.Year, ct);
        invoice.InvoiceNumber = InvoiceNumber.Format(Series, invoice.InvoiceDate.Year, seq);
        invoice.Status = InvoiceStatus.Approved;
        await db.SaveChangesAsync(ct);

        if (beforeCommit is not null) await beforeCommit();
        await tx.CommitAsync(ct);
        return invoice.InvoiceNumber;
    }
}
