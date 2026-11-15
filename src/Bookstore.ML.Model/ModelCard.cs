using System.Text.Json;

namespace Bookstore.ML.Model;

/// <summary>
/// Saved next to every model zip: which data it was trained on, how good it was, and the input range it has seen.
/// </summary>
public sealed record ModelCard(
    string Version,
    string Trainer,
    DateOnly TrainedFrom,
    DateOnly TrainedUntil,
    int TrainingRows,
    string DataSha256,
    double HoldoutMae,
    double HoldoutRmse,
    float MaxDistanceKm,
    float MaxItemCount,
    string[] Warehouses,
    string[] Carriers)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static ModelCard Load(string path) =>
        JsonSerializer.Deserialize<ModelCard>(File.ReadAllText(path)) ?? throw new InvalidDataException(path);

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Json));

    /// <summary>Null when the input looks like the training data, otherwise the first reason it does not.</summary>
    public string? OutOfRange(DeliveryInput input) =>
        !Warehouses.Contains(input.Warehouse) ? $"unknown warehouse {input.Warehouse}"
        : !Carriers.Contains(input.Carrier) ? $"unknown carrier {input.Carrier}"
        : input.DistanceKm > MaxDistanceKm ? $"distance {input.DistanceKm} km > {MaxDistanceKm}"
        : input.ItemCount > MaxItemCount ? $"{input.ItemCount} items > {MaxItemCount}"
        : null;
}
