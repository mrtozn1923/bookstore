namespace Bookstore.Privacy.Retention;

// Every copy outside the main database (search index, cache, e-mail service) gets an eraser.
public interface IPersonalDataCopy
{
    string Name { get; }
    Task EraseCustomerAsync(int customerId, CancellationToken ct);
}
