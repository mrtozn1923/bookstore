using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Payments;

public enum PaymentAttemptStatus
{
    // Sent (or about to be sent); the provider's answer is not known yet.
    Open,
    Succeeded,
    Declined,
    // Reconciliation found no charge at the provider: safe to start a new attempt.
    Abandoned
}

// One payment intent: the same order, amount and card. Every HTTP request for it carries the same key.
public sealed class PaymentAttempt
{
    public required Guid Id { get; init; }
    public required int OrderId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string CardTokenHash { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public PaymentAttemptStatus Status { get; set; } = PaymentAttemptStatus.Open;
    public string? ProviderPaymentId { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }

    // What the provider sees as merchant_reference; reconciliation searches by it.
    public string Reference => $"bookstore-{OrderId}-{Id:N}";
}

// In memory like OrderStore; in the real Bookstore this is a PaymentAttempts table in BookstoreDb,
// written in the same transaction as the order status (Message Queue series, Transactional Outbox post).
public sealed class PaymentAttemptStore(TimeProvider time)
{
    private readonly List<PaymentAttempt> _attempts = [];

    public PaymentAttempt GetOrStart(int orderId, decimal amount, string currency, string cardToken, out bool reused)
    {
        var tokenHash = Hash(cardToken);
        lock (_attempts)
        {
            var open = _attempts.SingleOrDefault(a => a.OrderId == orderId && a.Status == PaymentAttemptStatus.Open);
            if (open is not null && open.Amount == amount && open.CardTokenHash == tokenHash)
            {
                reused = true;
                return open;
            }

            if (open is not null)
            {
                // A different card or amount while the old attempt may still be charged: refuse.
                throw new PaymentAttemptConflictException(orderId);
            }

            var id = Guid.CreateVersion7(time.GetUtcNow());
            var attempt = new PaymentAttempt
            {
                Id = id,
                OrderId = orderId,
                IdempotencyKey = id.ToString(),
                Amount = amount,
                Currency = currency,
                CardTokenHash = tokenHash,
                CreatedAt = time.GetUtcNow()
            };
            _attempts.Add(attempt);
            reused = false;
            return attempt;
        }
    }

    public IReadOnlyList<PaymentAttempt> OpenOlderThan(DateTimeOffset cutoff)
    {
        lock (_attempts)
        {
            return _attempts.Where(a => a.Status == PaymentAttemptStatus.Open && a.CreatedAt <= cutoff).ToList();
        }
    }

    public IReadOnlyList<PaymentAttempt> ForOrder(int orderId)
    {
        lock (_attempts)
        {
            return _attempts.Where(a => a.OrderId == orderId).ToList();
        }
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public sealed class PaymentAttemptConflictException(int orderId)
    : Exception($"Order {orderId} has a payment that is still being verified.");
