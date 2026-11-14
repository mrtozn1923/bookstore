using Microsoft.EntityFrameworkCore;

namespace Bookstore.ScaleLab.ReadRouting;

public sealed class Order
{
    public long OrderId { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime CreatedAt { get; set; }
}

public static class OrderMapping
{
    public static void Configure(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("orders");
            order.HasKey(o => o.OrderId);
            order.Property(o => o.OrderId).HasColumnName("order_id");
            order.Property(o => o.CustomerId).HasColumnName("customer_id");
            order.Property(o => o.Status).HasColumnName("status");
            order.Property(o => o.TotalAmount).HasColumnName("total_amount");
            order.Property(o => o.Currency).HasColumnName("currency");
            order.Property(o => o.CreatedAt).HasColumnName("created_at");
        });
}
