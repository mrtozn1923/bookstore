using System.Net;
using k8s;
using k8s.Autorest;
using k8s.Models;

namespace Bookstore.CampaignController;

public sealed class CampaignReconciler(IKubernetes client, StorefrontCache storefront, TimeProvider clock)
{
    public const string Finalizer = "bookstore.example.com/storefront-cleanup";

    // Level-triggered: always read the current object and move the world towards it.
    public async Task ReconcileAsync(string ns, string name, CancellationToken ct)
    {
        Campaign campaign;
        try
        {
            campaign = await client.CustomObjects.GetNamespacedCustomObjectAsync<Campaign>(
                Campaign.Group, Campaign.Version, ns, Campaign.Plural, name, ct);
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return; // already gone; the ConfigMap is removed by garbage collection
        }

        string key = $"{ns}/{name}";
        List<string> finalizers = campaign.Metadata.Finalizers?.ToList() ?? [];

        if (campaign.Metadata.DeletionTimestamp is not null)
        {
            if (finalizers.Remove(Finalizer))
            {
                await storefront.RemoveAsync(key, ct);
                Log($"{key}: banner removed from storefront, releasing finalizer");
                await PatchFinalizersAsync(ns, name, finalizers, ct);
            }
            return;
        }

        if (!finalizers.Contains(Finalizer))
        {
            finalizers.Add(Finalizer);
            await PatchFinalizersAsync(ns, name, finalizers, ct);
            Log($"{key}: finalizer added");
            return; // the update triggers another reconcile
        }

        string configMapName = $"campaign-{name}";
        await EnsureConfigMapAsync(campaign, configMapName, ct);
        await storefront.PublishAsync(key, campaign.Spec.Title, ct);

        DateTimeOffset now = clock.GetUtcNow();
        string phase = now < campaign.Spec.StartsAt ? "Scheduled" : now >= campaign.Spec.EndsAt ? "Expired" : "Active";
        long generation = campaign.Metadata.Generation ?? 0;
        if (campaign.Status is not { } status || status.Phase != phase || status.ObservedGeneration != generation)
        {
            var patch = new { status = new CampaignStatus(phase, generation, configMapName) };
            await client.CustomObjects.PatchNamespacedCustomObjectStatusAsync(
                new V1Patch(patch, V1Patch.PatchType.MergePatch), Campaign.Group, Campaign.Version, ns, Campaign.Plural, name,
                cancellationToken: ct);
            Log($"{key}: generation {generation} reconciled, phase {phase}");
        }
    }

    private async Task EnsureConfigMapAsync(Campaign campaign, string configMapName, CancellationToken ct)
    {
        string ns = campaign.Metadata.NamespaceProperty;
        V1ConfigMap desired = new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = configMapName,
                NamespaceProperty = ns,
                Labels = new Dictionary<string, string> { ["app.kubernetes.io/managed-by"] = "bookstore-campaign-controller" },
                OwnerReferences =
                [
                    new V1OwnerReference
                    {
                        ApiVersion = campaign.ApiVersion,
                        Kind = campaign.Kind,
                        Name = campaign.Metadata.Name,
                        Uid = campaign.Metadata.Uid,
                        Controller = true,
                        BlockOwnerDeletion = true
                    }
                ]
            },
            Data = new Dictionary<string, string>
            {
                ["title"] = campaign.Spec.Title,
                ["discountPercent"] = campaign.Spec.DiscountPercent.ToString(),
                ["startsAt"] = campaign.Spec.StartsAt.ToString("O"),
                ["endsAt"] = campaign.Spec.EndsAt.ToString("O"),
                ["isbns"] = string.Join(',', campaign.Spec.Isbns)
            }
        };

        try
        {
            V1ConfigMap current = await client.CoreV1.ReadNamespacedConfigMapAsync(configMapName, ns, cancellationToken: ct);
            if (!current.Data.OrderBy(kv => kv.Key).SequenceEqual(desired.Data.OrderBy(kv => kv.Key)))
            {
                current.Data = desired.Data;
                await client.CoreV1.ReplaceNamespacedConfigMapAsync(current, configMapName, ns, cancellationToken: ct);
                Log($"{ns}/{campaign.Metadata.Name}: configmap {configMapName} updated");
            }
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
            await client.CoreV1.CreateNamespacedConfigMapAsync(desired, ns, cancellationToken: ct);
            Log($"{ns}/{campaign.Metadata.Name}: configmap {configMapName} created");
        }
    }

    private Task PatchFinalizersAsync(string ns, string name, List<string> finalizers, CancellationToken ct) =>
        client.CustomObjects.PatchNamespacedCustomObjectAsync(
            new V1Patch(new { metadata = new { finalizers } }, V1Patch.PatchType.MergePatch),
            Campaign.Group, Campaign.Version, ns, Campaign.Plural, name, cancellationToken: ct);

    public static void Log(string message) => Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss} {message}");
}
