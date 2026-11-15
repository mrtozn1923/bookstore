namespace Bookstore.Privacy;

public sealed class ErasureRequest
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
