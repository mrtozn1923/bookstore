using Microsoft.ML.Data;

namespace Bookstore.ML.Lab;

/// <summary>The columns of data/orders_clean.csv that the delivery-time models use.</summary>
public class DeliveryInput
{
    [LoadColumn(0)] public string OrderId { get; set; } = "";
    [LoadColumn(1)] public string CustomerId { get; set; } = "";
    [LoadColumn(2)] public DateTime OrderedAtUtc { get; set; }
    [LoadColumn(3)] public string City { get; set; } = "";
    [LoadColumn(4)] public string District { get; set; } = "";
    [LoadColumn(5)] public string Warehouse { get; set; } = "";
    [LoadColumn(6)] public float DistanceKm { get; set; }
    [LoadColumn(7)] public string Carrier { get; set; } = "";
    [LoadColumn(8)] public string ShippingMethod { get; set; } = "";
    [LoadColumn(9)] public float ItemCount { get; set; }
    [LoadColumn(10)] public float TotalWeightKg { get; set; }
    [LoadColumn(11)] public float OrderTotal { get; set; }
    [LoadColumn(13)] public bool IsCampaign { get; set; }
    [LoadColumn(14)] public string PaymentMethod { get; set; } = "";
    [LoadColumn(15)] public bool HasBackorderItem { get; set; }
    [LoadColumn(16)] public string CustomerNote { get; set; } = "";
    [LoadColumn(17)] public bool IsCancelled { get; set; }
    [LoadColumn(18)] public float DeliveryDays { get; set; }
    [LoadColumn(19)] public float TrackingEventCount { get; set; }
}

/// <summary>Post 03 leakage experiments: columns that are not in the file, filled in code.</summary>
public sealed class DeliveryExtra : DeliveryInput
{
    /// <summary>Days since 2024-11-01: the order date as a number.</summary>
    public float DaySerial { get; set; }

    public float CustomerAvgDays { get; set; }
}
