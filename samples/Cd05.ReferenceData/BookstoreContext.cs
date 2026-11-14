using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.ReferenceData;

public class BookstoreContext(DbContextOptions<BookstoreContext> options, bool restrict = true) : DbContext(options)
{
    public DbSet<OrderStatusRow> OrderStatuses => Set<OrderStatusRow>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<Order> Orders => Set<Order>();

    public static readonly Dictionary<OrderStatus, (string Tr, string En)> StatusLabels = new()
    {
        [OrderStatus.Draft] = ("Taslak", "Draft"),
        [OrderStatus.PendingApproval] = ("Onay bekliyor", "Pending approval"),
        [OrderStatus.Approved] = ("Onaylandı", "Approved"),
        [OrderStatus.Rejected] = ("Reddedildi", "Rejected"),
        [OrderStatus.Cancelled] = ("İptal edildi", "Cancelled"),
    };

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
        => c.Properties<string>().HaveMaxLength(100);

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<OrderStatusRow>(s =>
        {
            s.ToTable("OrderStatuses");
            s.Property(x => x.Id).ValueGeneratedNever();
            s.HasIndex(x => x.Code).IsUnique();
            // One row per enum value: if the enum changes, the migration changes too
            s.HasData(Enum.GetValues<OrderStatus>().Select(v => new OrderStatusRow
            {
                Id = v,
                Code = v.ToString(),
                NameTr = StatusLabels[v].Tr,
                NameEn = StatusLabels[v].En
            }));
        });

        b.Entity<BudgetCategory>().HasIndex(x => x.Code).IsUnique();

        b.Entity<Order>(o =>
        {
            o.Property(x => x.TotalAmount).HasPrecision(19, 4);
            o.Property(x => x.Status).HasColumnName("StatusId");
            var status = o.HasOne<OrderStatusRow>().WithMany().HasForeignKey(x => x.Status);
            var category = o.HasOne<BudgetCategory>().WithMany().HasForeignKey(x => x.BudgetCategoryId);
            if (restrict)
            {
                status.OnDelete(DeleteBehavior.Restrict);
                category.OnDelete(DeleteBehavior.Restrict);
            }
        });
    }
}

public class LegacyContext(DbContextOptions<LegacyContext> options) : DbContext(options)
{
    public DbSet<LegacyOrder> LegacyOrders => Set<LegacyOrder>();

    protected override void OnModelCreating(ModelBuilder b)
        => b.Entity<LegacyOrder>().Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
}
