using System.Globalization;

namespace Bookstore.ML.Lab;

/// <summary>One line of data/orders.csv, typed for LINQ queries.</summary>
public sealed record OrderRow(
    string OrderId, string CustomerId, DateTime OrderedAtUtc, string City, string District, string Warehouse,
    int DistanceKm, string Carrier, string ShippingMethod, int ItemCount, double? TotalWeightKg, decimal OrderTotal,
    decimal DiscountRate, bool IsCampaign, string PaymentMethod, bool HasBackorderItem, string CustomerNote,
    bool IsCancelled, double? DeliveryDays, int? TrackingEventCount, bool IsReturned, DateTime? ReturnRequestedUtc)
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static IEnumerable<OrderRow> Load(string path = "data/orders.csv") => Csv.Read(path).Select(Parse);

    public static OrderRow Parse(string[] f) => new(
        f[0], f[1], DateTime.Parse(f[2], Inv, DateTimeStyles.AdjustToUniversal), f[3], f[4], f[5],
        int.Parse(f[6], Inv), f[7], f[8], int.Parse(f[9], Inv), NullableDouble(f[10]), decimal.Parse(f[11], Inv),
        decimal.Parse(f[12], Inv), bool.Parse(f[13]), f[14], bool.Parse(f[15]), f[16], bool.Parse(f[17]),
        NullableDouble(f[18]), f[19] == "" ? null : int.Parse(f[19], Inv), bool.Parse(f[20]),
        f[21] == "" ? null : DateTime.Parse(f[21], Inv, DateTimeStyles.AdjustToUniversal));

    private static double? NullableDouble(string s) => s == "" ? null : double.Parse(s, Inv);
}
