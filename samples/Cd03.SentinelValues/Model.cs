using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Sentinels;

public enum SpendingLimitStatus
{
    Pending = 1,     // not decided yet
    Set = 2,
    Unlimited = 3    // the user may order without a limit
}

public class InstitutionUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public decimal? SpendingLimit { get; set; }
    public SpendingLimitStatus SpendingLimitStatus { get; set; }
}

public abstract record AgreementEnd
{
    public static AgreementEnd OpenEnded { get; } = new Open();
    public static AgreementEnd On(DateOnly date) => new OnDate(date);

    public abstract bool Covers(DateOnly day);

    private sealed record Open : AgreementEnd
    {
        public override bool Covers(DateOnly day) => true;
        public override string ToString() => "open-ended";
    }

    private sealed record OnDate(DateOnly Date) : AgreementEnd
    {
        public override bool Covers(DateOnly day) => day <= Date;
        public override string ToString() => Date.ToString("yyyy-MM-dd");
    }
}

public class DiscountAgreement
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public decimal DiscountRate { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }          // null = open-ended
    public DateOnly EffectiveEndDate { get; private set; }

    public AgreementEnd End => EndDate is { } date ? AgreementEnd.On(date) : AgreementEnd.OpenEnded;
    public bool IsActiveOn(DateOnly day) => StartDate <= day && End.Covers(day);
}

public class LegacyContract
{
    public int Id { get; set; }
    public DateTime SignedAt { get; set; }
}

public record DiscountAgreementDto(int Id, DateOnly StartDate, DateOnly? EndDate);
public record AgreementPatch(DateOnly? EndDate);

public static class AgreementPatching
{
    // RFC 7396 (JSON Merge Patch): property missing = leave it, null = clear it
    public static void ApplyPatch(DiscountAgreement agreement, JsonElement patch)
    {
        if (patch.TryGetProperty("endDate", out JsonElement end))
            agreement.EndDate = end.ValueKind == JsonValueKind.Null ? null : end.Deserialize<DateOnly>();
    }
}

public class SentinelContext(DbContextOptions<SentinelContext> options) : DbContext(options)
{
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
    public DbSet<DiscountAgreement> DiscountAgreements => Set<DiscountAgreement>();
    public DbSet<LegacyContract> LegacyContracts => Set<LegacyContract>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<InstitutionUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.SpendingLimit).HasPrecision(19, 4);
            e.ToTable(t => t.HasCheckConstraint("CK_InstitutionUsers_SpendingLimit",
                "([SpendingLimitStatus] = 2 AND [SpendingLimit] IS NOT NULL) OR ([SpendingLimitStatus] <> 2 AND [SpendingLimit] IS NULL)"));
        });

        b.Entity<DiscountAgreement>(a =>
        {
            a.Property(x => x.DiscountRate).HasPrecision(5, 4);
            a.Property(x => x.EffectiveEndDate)
                .HasComputedColumnSql("ISNULL([EndDate], DATEFROMPARTS(9999, 12, 31))", stored: true);
            a.HasIndex(x => new { x.StartDate, x.EffectiveEndDate });
            a.ToTable(t => t.HasCheckConstraint("CK_DiscountAgreements_Range",
                "[EndDate] IS NULL OR [EndDate] >= [StartDate]"));
        });

        // An old table: the column is the legacy datetime type (lower bound 1753)
        b.Entity<LegacyContract>().Property(x => x.SignedAt).HasColumnType("datetime");
    }
}
