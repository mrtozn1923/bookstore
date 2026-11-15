using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Security;

internal sealed class AccessAuditEntryConfiguration : IEntityTypeConfiguration<AccessAuditEntry>
{
    public void Configure(EntityTypeBuilder<AccessAuditEntry> entry)
    {
        entry.ToTable("AccessAuditEntries", "security");
        entry.Property(e => e.UserId).HasMaxLength(100);
        entry.Property(e => e.Action).HasMaxLength(100);
        entry.Property(e => e.Outcome).HasMaxLength(20);
        entry.Property(e => e.TraceId).HasMaxLength(32).IsUnicode(false);
        entry.HasIndex(e => new { e.Action, e.OccurredAtUtc });
    }
}
