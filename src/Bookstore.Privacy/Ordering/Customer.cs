namespace Bookstore.Ordering;

public sealed class Customer
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly? BirthDate { get; set; }
    public required string City { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<Order> Orders { get; } = [];
    public List<CustomerSession> Sessions { get; } = [];
}
