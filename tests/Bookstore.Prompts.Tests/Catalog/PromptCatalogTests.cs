using System.Text.Json;
using Bookstore.Prompts.Catalog;

namespace Bookstore.Prompts.Tests.Catalog;

public class PromptCatalogTests
{
    static Dictionary<string, string> Lock() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "prompts.lock.json")))!;

    // A released version never changes; a changed prompt is a new version, reviewed together with its eval result
    [Fact]
    public void Every_prompt_matches_its_locked_fingerprint()
    {
        Dictionary<string, string> locked = Lock();

        Assert.All(PromptCatalog.Default.All, template =>
        {
            Assert.True(locked.TryGetValue(template.Key, out string? fingerprint), $"{template.Key} is not in prompts.lock.json");
            Assert.True(fingerprint == template.Fingerprint,
                $"{template.Key} changed ({fingerprint} -> {template.Fingerprint}). Create a new version instead of editing a released one.");
        });
        Assert.Equal(locked.Keys.Order(), PromptCatalog.Default.All.Select(t => t.Key).Order());
    }

    [Fact]
    public void Every_prompt_pins_a_model_digest()
    {
        Assert.All(PromptCatalog.Default.All, t => Assert.Matches("^[0-9a-f]{12}$", t.Metadata.ModelDigest));
    }

    [Fact]
    public void Rollout_is_stable_per_customer_and_close_to_the_share()
    {
        PromptRollout rollout = new("return-check", "v3", "v4", candidatePercent: 10);

        List<string> versions = [.. Enumerable.Range(1, 10_000).Select(i => rollout.VersionFor($"c-{i}"))];

        Assert.Equal(rollout.VersionFor("c-42"), rollout.VersionFor("c-42"));
        Assert.InRange(versions.Count(v => v == "v4"), 900, 1100);
    }
}
