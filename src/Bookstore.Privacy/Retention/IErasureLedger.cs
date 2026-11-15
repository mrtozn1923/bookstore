namespace Bookstore.Privacy.Retention;

// Completed erasures, stored outside the main database and its backups.
public interface IErasureLedger
{
    Task AppendAsync(int customerId, DateTime erasedAtUtc, CancellationToken ct);
    Task<IReadOnlyList<int>> ErasedCustomerIdsAsync(CancellationToken ct);
}

public sealed class FileErasureLedger(string path) : IErasureLedger
{
    public Task AppendAsync(int customerId, DateTime erasedAtUtc, CancellationToken ct) =>
        File.AppendAllTextAsync(path, $"{customerId};{erasedAtUtc:O}{Environment.NewLine}", ct);

    public async Task<IReadOnlyList<int>> ErasedCustomerIdsAsync(CancellationToken ct) =>
        File.Exists(path)
            ? [.. (await File.ReadAllLinesAsync(path, ct)).Select(line => int.Parse(line.Split(';')[0]))]
            : [];
}
