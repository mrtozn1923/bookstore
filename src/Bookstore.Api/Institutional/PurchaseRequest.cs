using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Institutional;

public enum PurchaseRequestStatus
{
    PendingApproval,
    Approved,
    Ordered
}

public sealed class PurchaseRequest
{
    public const int MinimumCopies = 20;
    public const decimal InstitutionalDiscount = 0.10m;

    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int UnitId { get; set; }
    public required string RequestedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public PurchaseRequestStatus Status { get; set; }
    public decimal Total { get; set; }
    public List<PurchaseRequestLine> Lines { get; } = [];

    // Who approved which content: the hash pins the lines that were approved.
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedLinesHash { get; set; }
    public int? OrderId { get; set; }

    public string ComputeLinesHash()
    {
        string canonical = string.Join(";", Lines
            .OrderBy(l => l.BookId)
            .Select(l => $"{l.BookId}:{l.Quantity}:{l.UnitPrice:0.00}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static decimal DiscountedPrice(decimal listPrice) =>
        Math.Round(listPrice * (1 - InstitutionalDiscount), 2, MidpointRounding.AwayFromZero);
}

public sealed class PurchaseRequestLine
{
    public int Id { get; set; }
    public int PurchaseRequestId { get; set; }
    public int BookId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
