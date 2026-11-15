using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Bookstore.FileDrops;
using Bookstore.Ordering;

namespace Bookstore.Payments.BankStatements;

public enum MatchOutcome
{
    Matched,
    AmountMismatch,
    UnknownReference,
    AlreadySeen
}

public sealed record StatementLine(string TransactionId, DateOnly ValueDate, decimal Amount, string Currency, string Description);

public sealed record ReconciliationLine(string TransactionId, decimal Amount, string Description, MatchOutcome Outcome, int? OrderId, string? Note);

// Institutional customers pay by bank transfer; the bank drops one collections file per day on its FTPS server.
public sealed partial class BankStatementReconciler(
    [FromKeyedServices(BankStatementReconciler.DropName)] IFileDrop drop,
    FileDropLedger ledger,
    OrderStore orders,
    FileDropFolders folders,
    TimeProvider time,
    ILogger<BankStatementReconciler> logger)
{
    public const string DropName = "bank";
    private const string Scope = "bank:transaction";

    public async Task<IReadOnlyList<ReconciliationLine>> RunAsync(CancellationToken cancellationToken)
    {
        await using var session = await drop.OpenAsync(cancellationToken);
        var report = new List<ReconciliationLine>();
        foreach (var file in (await session.ListAsync("statements", cancellationToken))
                     .Where(f => f.Name.EndsWith(".csv", StringComparison.Ordinal)).OrderBy(f => f.Name))
        {
            var local = folders.Path("bank-statements", "incoming", file.Name);
            await using (var stream = File.Create(local))
            {
                await session.DownloadAsync($"statements/{file.Name}", stream, cancellationToken);
            }

            foreach (var line in Parse(await File.ReadAllLinesAsync(local, Encoding.UTF8, cancellationToken)))
            {
                report.Add(Match(line));
            }

            File.Move(local, folders.Path("bank-statements", "processed", file.Name), overwrite: true);
            await session.MoveAsync($"statements/{file.Name}", $"statements/processed/{file.Name}", cancellationToken);
        }

        return report;
    }

    // Idempotency per bank transaction, not per file: banks resend statements, and a day's file may repeat earlier lines.
    private ReconciliationLine Match(StatementLine line)
    {
        if (ledger.Find(Scope, line.TransactionId) is { } seen)
        {
            return new(line.TransactionId, line.Amount, line.Description, MatchOutcome.AlreadySeen, null, $"seen at {seen.At:O}");
        }

        var result = OrderReference().Match(line.Description) is { Success: true } reference
            && orders.Find(int.Parse(reference.Groups[1].Value, CultureInfo.InvariantCulture)) is { InstitutionId: not null } order
                ? Compare(line, order)
                : new(line.TransactionId, line.Amount, line.Description, MatchOutcome.UnknownReference, null, "no order reference found");
        ledger.Add(Scope, line.TransactionId, $"{result.Outcome} {result.OrderId}", time.GetUtcNow());
        if (result.Outcome != MatchOutcome.Matched)
        {
            // Not an error to retry: a person in the back office looks at it.
            logger.LogWarning("Bank transaction {TransactionId} needs a manual check: {Outcome}", line.TransactionId, result.Outcome);
        }

        return result;
    }

    private static ReconciliationLine Compare(StatementLine line, Order order)
    {
        if (line.Currency != order.Currency || line.Amount != order.ItemsTotal)
        {
            return new(line.TransactionId, line.Amount, line.Description, MatchOutcome.AmountMismatch, order.Id,
                $"expected {order.ItemsTotal.ToString("0.00", CultureInfo.InvariantCulture)} {order.Currency}");
        }

        order.Status = OrderStatus.Paid;
        return new(line.TransactionId, line.Amount, line.Description, MatchOutcome.Matched, order.Id, null);
    }

    // transaction_id;value_date;amount;currency;description — the bank's format: invariant decimals, ISO dates.
    public static IEnumerable<StatementLine> Parse(string[] lines) =>
        lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Split(';', 5)).Select(f => new StatementLine(
            f[0], DateOnly.ParseExact(f[1], "yyyy-MM-dd", CultureInfo.InvariantCulture),
            decimal.Parse(f[2], CultureInfo.InvariantCulture), f[3], f[4]));

    // Customers type the reference by hand: "BKS-1003", "bks 1003", "BKS1003 school payment".
    [GeneratedRegex(@"\bBKS[-\s]?(\d{4,})\b", RegexOptions.IgnoreCase)]
    private static partial Regex OrderReference();
}
