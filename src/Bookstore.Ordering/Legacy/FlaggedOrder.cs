namespace Bookstore.Ordering.Legacy;

public class FlaggedOrder
{
    public bool IsPaid { get; set; }
    public bool IsShipped { get; set; }
    public bool IsCancelled { get; set; }
    public string? PaymentId { get; set; }
    public string? TrackingNo { get; set; }
    public string? CancelReason { get; set; }

    public string Describe()
    {
        if (IsCancelled)
            return $"cancelled: {CancelReason}";
        if (IsShipped)
            return $"shipped with {TrackingNo}";
        if (IsPaid)
            return $"paid with {PaymentId}";
        return "pending";
    }
}
