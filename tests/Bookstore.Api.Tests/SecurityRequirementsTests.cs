using System.Reflection;
using System.Text.Json;

namespace Bookstore.Api.Tests;

// Keeps docs/security/asvs-requirements.json and the test suite in step with each other.
public sealed class SecurityRequirementsTests
{
    private sealed record Requirement(string Id, int Level, string Verification, string? Evidence, string? Backlog);
    private sealed record RequirementsFile(string Standard, int TargetLevel, IReadOnlyList<Requirement> Requirements);

    private static readonly RequirementsFile File = Load();

    private static readonly IReadOnlyList<(string Test, string Id)> AsvsTraits =
    [
        .. typeof(SecurityRequirementsTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes<TraitAttribute>()
                .Where(a => a.Name == "asvs")
                .Select(a => ($"{m.DeclaringType!.Name}.{m.Name}", a.Value)))
    ];

    [Fact]
    public void Every_requirement_verified_by_test_has_a_test()
    {
        string[] missing =
        [
            .. File.Requirements
                .Where(r => r.Verification == "test")
                .Select(r => r.Id)
                .Where(id => !AsvsTraits.Any(t => t.Id == id))
        ];

        Assert.True(missing.Length == 0, $"No test with [Trait(\"asvs\", ...)] for: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Every_asvs_trait_points_to_a_listed_requirement()
    {
        string[] unknown = [.. AsvsTraits.Where(t => File.Requirements.All(r => r.Id != t.Id)).Select(t => $"{t.Test} -> {t.Id}")];

        Assert.True(unknown.Length == 0, $"Traits with unknown requirement ids: {string.Join(", ", unknown)}");
    }

    [Fact]
    public void Review_and_planned_requirements_name_their_evidence_or_backlog_item()
    {
        string[] incomplete =
        [
            .. File.Requirements
                .Where(r => (r.Verification == "review" && r.Evidence is null) || (r.Verification == "planned" && r.Backlog is null))
                .Select(r => r.Id)
        ];

        Assert.Empty(incomplete);
    }

    private static RequirementsFile Load()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("Bookstore.slnx").Any())
        {
            dir = dir.Parent;
        }
        string path = Path.Combine(dir?.FullName ?? throw new InvalidOperationException("Repo root not found."),
            "docs", "security", "asvs-requirements.json");
        return JsonSerializer.Deserialize<RequirementsFile>(System.IO.File.ReadAllText(path), JsonSerializerOptions.Web)!;
    }
}
