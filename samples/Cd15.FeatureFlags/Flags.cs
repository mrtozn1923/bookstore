using System.Reflection;
using Microsoft.FeatureManagement;

namespace Bookstore.Institutional.Features;

public interface ITenantContext
{
    int TenantId { get; }
}

// Post 13's TenantContext, reduced to what this sample needs
public sealed class TenantContext : ITenantContext
{
    private int? _tenantId;
    public int TenantId => _tenantId ?? throw new InvalidOperationException("Tenant is not resolved for this scope.");
    public void Set(int tenantId) => _tenantId = tenantId;
}

public sealed class TenantFilterSettings
{
    public int[] Tenants { get; set; } = [];
}

[FilterAlias("Bookstore.Tenant")]
public sealed class TenantFilter(ITenantContext tenant) : IFeatureFilter
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        TenantFilterSettings settings = context.Parameters.Get<TenantFilterSettings>() ?? new();
        return Task.FromResult(settings.Tenants.Contains(tenant.TenantId));
    }
}

public enum ToggleKind { Release, Experiment, Ops, Permission }

[AttributeUsage(AttributeTargets.Field)]
public sealed class FlagInfoAttribute(ToggleKind kind, string owner, string expires) : Attribute
{
    public ToggleKind Kind { get; } = kind;
    public string Owner { get; } = owner;
    public DateOnly Expires { get; } = DateOnly.Parse(expires);
}

public static class FeatureFlags
{
    [FlagInfo(ToggleKind.Release, "team-institutional", "2026-12-31")]
    public const string PurchaseApprovalV2 = "PurchaseApprovalV2";

    [FlagInfo(ToggleKind.Ops, "team-billing", "9999-12-31")]
    public const string InvoicePdfExport = "InvoicePdfExport";

    [FlagInfo(ToggleKind.Experiment, "team-web", "2026-12-15")]
    public const string DashboardLayoutB = "DashboardLayoutB";

    [FlagInfo(ToggleKind.Release, "team-mobile", "2026-09-30")]
    public const string IsbnScannerBeta = "IsbnScannerBeta";

    public static IEnumerable<(string Name, FlagInfoAttribute Info)> All() =>
        typeof(FeatureFlags).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => ((string)f.GetRawConstantValue()!, f.GetCustomAttribute<FlagInfoAttribute>()!));
}

public sealed record ApprovalRequest(int OrderId, decimal TotalAmount);

public interface IPurchaseApprovalPolicy
{
    string Name { get; }
    IReadOnlyList<string> StepsFor(ApprovalRequest request);
}

public sealed class SingleStepApproval : IPurchaseApprovalPolicy
{
    public string Name => "single-step";
    public IReadOnlyList<string> StepsFor(ApprovalRequest request) => ["unit-approver"];
}

public sealed class TwoLevelApproval : IPurchaseApprovalPolicy
{
    public string Name => "two-level";
    public IReadOnlyList<string> StepsFor(ApprovalRequest request) =>
        request.TotalAmount > 50_000m ? ["unit-approver", "purchasing-manager"] : ["unit-approver"];
}

public sealed class PurchaseApprovalPolicyRouter(
    IVariantFeatureManagerSnapshot features,
    SingleStepApproval current,
    TwoLevelApproval next)
{
    public async Task<IPurchaseApprovalPolicy> ResolveAsync(CancellationToken ct) =>
        await features.IsEnabledAsync(FeatureFlags.PurchaseApprovalV2, ct) ? next : current;
}
