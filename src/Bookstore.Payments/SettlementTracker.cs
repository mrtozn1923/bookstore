namespace Bookstore.Payments;

// Collects settlements the gateway reports through its webhook-backed event
public sealed class SettlementTracker : IDisposable
{
    private readonly IPaymentGateway gateway;
    private readonly List<string> settled = [];

    public SettlementTracker(IPaymentGateway gateway)
    {
        this.gateway = gateway;
        gateway.Settled += OnSettled;
    }

    public IReadOnlyList<string> SettledTransactions => settled;
    public decimal SettledTotal { get; private set; }

    private void OnSettled(object? sender, PaymentSettledEventArgs e)
    {
        settled.Add(e.TransactionId);
        SettledTotal += e.Amount;
    }

    public void Dispose() => gateway.Settled -= OnSettled;
}
