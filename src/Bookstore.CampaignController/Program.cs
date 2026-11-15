using System.Text.Json;
using System.Threading.Channels;
using Bookstore.CampaignController;
using k8s;
using k8s.Autorest;

// Out-of-cluster for the demo: reads KUBECONFIG; inside a pod BuildDefaultConfig uses the ServiceAccount token.
string ns = Environment.GetEnvironmentVariable("WATCH_NAMESPACE") ?? "bookstore";
using Kubernetes client = new(KubernetesClientConfiguration.BuildDefaultConfig());
CampaignReconciler reconciler = new(client, new StorefrontCache(Environment.GetEnvironmentVariable("STOREFRONT_FILE") ?? "storefront-banners.json"), TimeProvider.System);

using CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
AppDomain.CurrentDomain.ProcessExit += (_, _) => cts.Cancel();

// Work queue: the watch only says "look at this name again"; the reconciler decides what to do.
Channel<string> queue = Channel.CreateUnbounded<string>();

Task worker = Task.Run(async () =>
{
    await foreach (string name in queue.Reader.ReadAllAsync(cts.Token))
    {
        try
        {
            await reconciler.ReconcileAsync(ns, name, cts.Token);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            CampaignReconciler.Log($"{ns}/{name}: reconcile failed ({e.GetType().Name}), retrying in 5s");
            _ = Task.Delay(TimeSpan.FromSeconds(5), cts.Token).ContinueWith(_ => queue.Writer.TryWrite(name), TaskScheduler.Default);
        }
    }
});

CampaignReconciler.Log($"watching campaigns in namespace {ns}");
try
{
    while (!cts.IsCancellationRequested)
    {
        // A watch ends after a server-side timeout; re-listing on every loop also covers missed events.
        try
        {
            await foreach ((WatchEventType type, object item) in client.CustomObjects.WatchListNamespacedCustomObjectAsync(
                Campaign.Group, Campaign.Version, ns, Campaign.Plural, timeoutSeconds: 300, cancellationToken: cts.Token))
            {
                Campaign campaign = KubernetesJson.Deserialize<Campaign>(((JsonElement)item).GetRawText());
                CampaignReconciler.Log($"event {type} {campaign.Metadata.Name} (generation {campaign.Metadata.Generation})");
                queue.Writer.TryWrite(campaign.Metadata.Name);
            }
        }
        catch (HttpOperationException e)
        {
            // 404 here usually means the CRD is not installed (yet or any more): wait instead of crashing.
            CampaignReconciler.Log($"watch failed ({(int)e.Response.StatusCode}), retrying in 10s");
            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
        }
    }
}
catch (OperationCanceledException)
{
}

queue.Writer.TryComplete();
CampaignReconciler.Log("controller stopped");
