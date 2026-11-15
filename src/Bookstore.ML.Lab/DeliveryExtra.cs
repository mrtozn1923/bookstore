namespace Bookstore.ML.Lab;

/// <summary>Post 03 leakage experiments: columns that are not in the file, filled in code.</summary>
public sealed class DeliveryExtra : DeliveryInput
{
    /// <summary>Days since 2024-11-01: the order date as a number.</summary>
    public float DaySerial { get; set; }

    public float CustomerAvgDays { get; set; }
}
