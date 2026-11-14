using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Idempotency;

// Institutional order (slice of Bookstore.Ordering for this sample)
public class Order
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int InstitutionUnitId { get; set; }
    public int BookId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public record CreateOrderRequest(int InstitutionUnitId, int BookId, int Quantity, decimal TotalAmount);

public record OrderDto(int Id, decimal TotalAmount);

public enum IdempotencyStatus { Started = 0, Completed = 1 }

public class IdempotencyRecord
{
    public long Id { get; set; }
    public int InstitutionId { get; set; }
    public string UserId { get; set; } = "";
    public string Key { get; set; } = "";
    public byte[] RequestHash { get; set; } = [];
    public IdempotencyStatus Status { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseContentType { get; set; }
    public string? ResponseLocation { get; set; }
    public string? ResponseBody { get; set; }
    public DateTimeOffset LockedUntil { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e => e.Property(o => o.TotalAmount).HasPrecision(19, 4));

        modelBuilder.Entity<IdempotencyRecord>(e =>
        {
            e.HasIndex(r => new { r.InstitutionId, r.UserId, r.Key }).IsUnique();
            e.HasIndex(r => r.ExpiresAt);
            e.Property(r => r.UserId).HasMaxLength(64);
            e.Property(r => r.Key).HasMaxLength(100).IsUnicode(false);
            e.Property(r => r.RequestHash).HasMaxLength(32).IsFixedLength();
            e.Property(r => r.ResponseContentType).HasMaxLength(100);
            e.Property(r => r.ResponseLocation).HasMaxLength(200);
        });
    }
}

// The distributor's stock API (Acme Book Distribution); slow like in real life
public interface IDistributorGateway
{
    Task EnsureInStockAsync(int bookId, int quantity, CancellationToken ct);
}

public sealed class FakeDistributorGateway : IDistributorGateway
{
    public Task EnsureInStockAsync(int bookId, int quantity, CancellationToken ct) => Task.Delay(400, ct);
}

public sealed record CurrentUser(int InstitutionId, string UserId)
{
    // Claims come from authentication; the demo fills them from a header (see Program.cs)
    public static CurrentUser From(HttpContext http) => new(
        int.Parse(http.User.FindFirst("institution_id")!.Value),
        http.User.Identity!.Name!);
}
