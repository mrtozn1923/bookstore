using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bookstore.Institutional.Settings;

public enum SettingScope : byte { Default = 0, Global = 1, Institution = 2, User = 3 }

public enum SettingType : byte { Int = 1, Bool = 2, String = 3 }

public class SettingDefinition
{
    public string Key { get; set; } = "";
    public SettingType Type { get; set; }
    public string DefaultValue { get; set; } = "";
    public SettingScope LowestScope { get; set; }
    public int? Min { get; set; }
    public int? Max { get; set; }
}

public class SettingValue
{
    public long Id { get; set; }
    public string Key { get; set; } = "";
    public SettingScope Scope { get; set; }
    public int? InstitutionId { get; set; }
    public int? UserId { get; set; }
    public string Value { get; set; } = "";
}

public class SettingChange
{
    public long Id { get; set; }
    public string Key { get; set; } = "";
    public SettingScope Scope { get; set; }
    public int? InstitutionId { get; set; }
    public int? UserId { get; set; }
    public string? OldValue { get; set; }
    public string NewValue { get; set; } = "";
    public int ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string Reason { get; set; } = "";
}

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<SettingDefinition> SettingDefinitions => Set<SettingDefinition>();
    public DbSet<SettingValue> SettingValues => Set<SettingValue>();
    public DbSet<SettingChange> SettingChanges => Set<SettingChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SettingDefinition>(b =>
        {
            b.HasKey(d => d.Key);
            b.Property(d => d.Key).HasMaxLength(100).IsUnicode(false);
            b.Property(d => d.DefaultValue).HasMaxLength(400);
        });
        modelBuilder.Entity<SettingValue>(b =>
        {
            b.Property(v => v.Key).HasMaxLength(100).IsUnicode(false);
            b.Property(v => v.Value).HasMaxLength(400);
            b.HasOne<SettingDefinition>().WithMany().HasForeignKey(v => v.Key);
            b.HasIndex(v => new { v.Key, v.InstitutionId, v.UserId }).IsUnique().HasFilter(null);
            b.HasIndex(v => new { v.InstitutionId, v.UserId });
            b.ToTable(t => t.HasCheckConstraint("CK_SettingValues_Scope",
                "([Scope] = 1 AND [InstitutionId] IS NULL AND [UserId] IS NULL) OR " +
                "([Scope] = 2 AND [InstitutionId] IS NOT NULL AND [UserId] IS NULL) OR " +
                "([Scope] = 3 AND [InstitutionId] IS NOT NULL AND [UserId] IS NOT NULL)"));
        });
        modelBuilder.Entity<SettingChange>(b =>
        {
            b.Property(c => c.Key).HasMaxLength(100).IsUnicode(false);
            b.Property(c => c.OldValue).HasMaxLength(400);
            b.Property(c => c.NewValue).HasMaxLength(400);
            b.Property(c => c.Reason).HasMaxLength(200);
        });
    }
}

// The opening: the same unique index without HasFilter(null)
public class NaiveSettingsContext(DbContextOptions<NaiveSettingsContext> options) : DbContext(options)
{
    public DbSet<SettingValue> SettingValues => Set<SettingValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SettingValue>(b =>
        {
            b.Property(v => v.Key).HasMaxLength(100).IsUnicode(false);
            b.Property(v => v.Value).HasMaxLength(400);
            b.HasIndex(v => new { v.Key, v.InstitutionId, v.UserId }).IsUnique();
        });
    }
}

// Counts the commands that reach the database
public sealed class QueryCounter : DbCommandInterceptor
{
    public int Count;

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref Count);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
