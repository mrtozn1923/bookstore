using Bookstore.Reporting.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Reporting;

public partial class ReportingContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // DBA team stores SOLD_AT in UTC; mark the values as UTC when reading.
        modelBuilder.Entity<BookSale>()
            .Property(e => e.SoldAt)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => throw new InvalidOperationException("ReportingContext is read-only.");

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("ReportingContext is read-only.");
}
