using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace Bookstore.CampaignController;

// C# shape of the Campaign custom resource (deploy/k8s/operator/campaign-crd.yaml).
public sealed class Campaign : IKubernetesObject<V1ObjectMeta>
{
    public const string Group = "bookstore.example.com";
    public const string Version = "v1alpha1";
    public const string Plural = "campaigns";

    [JsonPropertyName("apiVersion")] public string ApiVersion { get; set; } = $"{Group}/{Version}";
    [JsonPropertyName("kind")] public string Kind { get; set; } = "Campaign";
    [JsonPropertyName("metadata")] public V1ObjectMeta Metadata { get; set; } = new();
    [JsonPropertyName("spec")] public CampaignSpec Spec { get; set; } = new();
    [JsonPropertyName("status")] public CampaignStatus? Status { get; set; }
}

public sealed class CampaignSpec
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("discountPercent")] public int DiscountPercent { get; set; }
    [JsonPropertyName("startsAt")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("endsAt")] public DateTimeOffset EndsAt { get; set; }
    [JsonPropertyName("isbns")] public List<string> Isbns { get; set; } = [];
}

public sealed record CampaignStatus(
    [property: JsonPropertyName("phase")] string Phase,
    [property: JsonPropertyName("observedGeneration")] long ObservedGeneration,
    [property: JsonPropertyName("configMapName")] string ConfigMapName);
