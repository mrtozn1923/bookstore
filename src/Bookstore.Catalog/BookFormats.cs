namespace Bookstore.Catalog;

public interface IBookFormat
{
    decimal VatRate { get; }
    string Delivery(DateOnly? releaseDate, DateOnly today);
}

public sealed record PrintedFormat(int WeightGrams) : IBookFormat
{
    public string? SignedBy { get; init; }

    public decimal VatRate => 0.10m;

    public decimal ShippingWeightKg() => WeightGrams / 1000m;

    public string Delivery(DateOnly? releaseDate, DateOnly today) =>
        releaseDate > today ? $"Ships on {releaseDate:yyyy-MM-dd}"
        : WeightGrams > 1000 ? "Cargo (heavy), 3 days"
        : "Cargo, 2 days";
}

public sealed record DigitalFormat(string FileFormat) : IBookFormat
{
    public decimal VatRate => 0.20m;

    public string Delivery(DateOnly? releaseDate, DateOnly today) =>
        releaseDate > today ? $"Available on {releaseDate:yyyy-MM-dd}"
        : $"Download ({FileFormat})";
}
