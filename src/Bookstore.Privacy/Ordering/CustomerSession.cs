namespace Bookstore.Ordering;

public sealed class CustomerSession
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime SignedInAtUtc { get; set; }
    public required string IpAddress { get; set; }
    public required string DeviceId { get; set; }
    public required string UserAgent { get; set; }
}
