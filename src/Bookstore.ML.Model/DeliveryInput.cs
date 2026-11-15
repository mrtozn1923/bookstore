using Microsoft.ML.Data;

namespace Bookstore.ML.Model;

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

/// <summary>What the delivery-time model returns: the predicted delivery time in days.</summary>
public sealed class DeliveryPrediction
{
    [ColumnName("Score")]
    public float PredictedDays { get; set; }
}
