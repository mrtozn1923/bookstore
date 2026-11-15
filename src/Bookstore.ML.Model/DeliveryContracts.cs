namespace Bookstore.ML.Model;

/// <summary>The HTTP request: raw order fields only. Every feature is computed inside the saved model pipeline.</summary>
public sealed record DeliveryRequest(
    DateTime OrderedAtUtc, string City, string District, string Warehouse, float DistanceKm, string Carrier,
    string ShippingMethod, int ItemCount, float? TotalWeightKg, float OrderTotal, bool IsCampaign,
    string PaymentMethod, bool HasBackorderItem, string CustomerNote)
{
    public DeliveryInput ToInput() => new()
    {
        OrderedAtUtc = OrderedAtUtc, City = City, District = District, Warehouse = Warehouse, DistanceKm = DistanceKm,
        Carrier = Carrier, ShippingMethod = ShippingMethod, ItemCount = ItemCount,
        TotalWeightKg = TotalWeightKg ?? float.NaN, OrderTotal = OrderTotal, IsCampaign = IsCampaign,
        PaymentMethod = PaymentMethod, HasBackorderItem = HasBackorderItem, CustomerNote = CustomerNote,
    };

    public static DeliveryRequest From(DeliveryInput o) => new(
        o.OrderedAtUtc, o.City, o.District, o.Warehouse, o.DistanceKm, o.Carrier, o.ShippingMethod, (int)o.ItemCount,
        float.IsNaN(o.TotalWeightKg) ? null : o.TotalWeightKg, o.OrderTotal, o.IsCampaign, o.PaymentMethod,
        o.HasBackorderItem, o.CustomerNote);
}

public sealed record DeliveryResponse(float PredictedDays, string ModelVersion, string? Warning);
