using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.Institutional.Audit;

public sealed class AuditInterceptor(AuditContext auditContext, TimeProvider clock) : SaveChangesInterceptor
{
    private sealed record Pending(EntityEntry Entry, string Action, List<PropertyChange> Changes);

    private readonly List<Pending> _pending = [];
    private IDbContextTransaction? _ownTransaction;
    private bool _writingAudit;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        DbContext db = eventData.Context!;
        if (_writingAudit)
        {
            return result;
        }

        _pending.Clear();
        foreach (EntityEntry entry in db.ChangeTracker.Entries().Where(e => e.Entity is IAudited))
        {
            List<PropertyChange> changes = entry.State switch
            {
                EntityState.Added => entry.Properties
                    .Where(p => !p.Metadata.IsPrimaryKey())
                    .Select(p => Change(p, null, p.CurrentValue)).ToList(),
                EntityState.Modified => entry.Properties
                    .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                    .Select(p => Change(p, p.OriginalValue, p.CurrentValue)).ToList(),
                EntityState.Deleted => entry.Properties
                    .Select(p => Change(p, p.OriginalValue, null)).ToList(),
                _ => []
            };
            if (changes.Count > 0)
            {
                _pending.Add(new Pending(entry, entry.State.ToString(), changes));
            }
        }

        // audit rows must be written in the same transaction as the business data
        if (_pending.Count > 0 && db.Database.CurrentTransaction is null)
        {
            _ownTransaction = await db.Database.BeginTransactionAsync(ct);
        }
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        DbContext db = eventData.Context!;
        if (_writingAudit || _pending.Count == 0)
        {
            return result;
        }

        DateTimeOffset now = clock.GetUtcNow();
        foreach (Pending p in _pending)
        {
            // identity values are real now: read the key after saving
            string key = string.Join(",", p.Entry.Metadata.FindPrimaryKey()!.Properties
                .Select(k => p.Entry.Property(k.Name).CurrentValue));
            db.Add(new AuditEntry
            {
                EntityType = p.Entry.Metadata.ClrType.Name,
                EntityId = key,
                Action = p.Action,
                Changes = JsonSerializer.Serialize(p.Changes),
                UserName = auditContext.UserName,
                CorrelationId = auditContext.CorrelationId,
                Reason = auditContext.Reason,
                ChangedAt = now
            });
        }
        _pending.Clear();

        _writingAudit = true;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            _writingAudit = false;
        }

        if (_ownTransaction is not null)
        {
            await _ownTransaction.CommitAsync(ct);
            await _ownTransaction.DisposeAsync();
            _ownTransaction = null;
        }
        return result;
    }

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken ct = default)
    {
        _pending.Clear();
        if (_ownTransaction is not null)
        {
            await _ownTransaction.RollbackAsync(ct);
            await _ownTransaction.DisposeAsync();
            _ownTransaction = null;
        }
    }

    private static PropertyChange Change(PropertyEntry p, object? oldValue, object? newValue)
    {
        bool personal = p.Metadata.PropertyInfo?.GetCustomAttribute<PersonalDataAttribute>() is not null;
        return personal
            ? new PropertyChange(p.Metadata.Name, oldValue is null ? null : "***", newValue is null ? null : "***")
            : new PropertyChange(p.Metadata.Name, oldValue, newValue);
    }
}
