using Microsoft.ML.Transforms;

namespace Bookstore.ML.Lab;

public sealed class OrderTimeInput
{
    public DateTime OrderedAtUtc { get; set; }
}

public sealed class OrderTimeFeatures
{
    public string LocalDay { get; set; } = "";
    public float LocalHour { get; set; }

    /// <summary>Hours until the warehouse can start on the order (15:00 cut-off, no weekend shifts).</summary>
    public float HoursUntilProcessing { get; set; }
}

/// <summary>
/// Date features computed inside the pipeline, so the saved model computes them the same way at prediction time.
/// The contract name lets ML.NET find this mapping again when the model is loaded.
/// </summary>
[CustomMappingFactoryAttribute("OrderTime")]
public sealed class OrderTimeMappingFactory : CustomMappingFactory<OrderTimeInput, OrderTimeFeatures>
{
    private static readonly TimeSpan TurkeyOffset = TimeSpan.FromHours(3);

    public static void Map(OrderTimeInput input, OrderTimeFeatures output)
    {
        DateTime local = input.OrderedAtUtc + TurkeyOffset;
        output.LocalDay = local.DayOfWeek.ToString()[..3];
        output.LocalHour = local.Hour + local.Minute / 60f;

        DateTime start = local.Hour >= 15 ? local.Date.AddDays(1).AddHours(9) : local;
        while (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            start = start.Date.AddDays(1).AddHours(9);
        }
        output.HoursUntilProcessing = (float)(start - local).TotalHours;
    }

    public override Action<OrderTimeInput, OrderTimeFeatures> GetMapping() => Map;
}
