using Bookstore.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Ordering;

internal sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> cart)
    {
        cart.ToTable("Carts", "ordering");
        cart.HasMany(c => c.Items).WithOne().HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> item)
    {
        item.ToTable("CartItems", "ordering");
        item.HasIndex(i => new { i.CartId, i.BookId }).IsUnique();
        item.HasOne<Book>().WithMany().HasForeignKey(i => i.BookId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> customer)
    {
        customer.ToTable("Customers", "ordering");
        customer.Property(c => c.Name).HasMaxLength(200);
        customer.Property(c => c.Email).HasMaxLength(256);
        customer.HasIndex(c => c.Email).IsUnique();
    }
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> order)
    {
        order.ToTable("Orders", "ordering");
        order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        order.Property(o => o.Currency).HasMaxLength(3).IsUnicode(false);
        order.Property(o => o.Total).HasPrecision(12, 2);
        order.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId);
        order.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> item)
    {
        item.ToTable("OrderItems", "ordering");
        item.Property(i => i.Title).HasMaxLength(200);
        item.Property(i => i.UnitPrice).HasPrecision(10, 2);
        item.Property(i => i.LineTotal).HasPrecision(12, 2);
        item.HasOne<Book>().WithMany().HasForeignKey(i => i.BookId).OnDelete(DeleteBehavior.Restrict);
    }
}
