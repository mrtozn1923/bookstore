using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Temporal;

// The institution's pricing terms, effective-dated: [ValidFrom, ValidTo)
public class InstitutionPricing
{
    public static readonly DateOnly OpenEnd = new(9999, 12, 31);

    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string PaymentTerm { get; set; } = "";
    public decimal DiscountRate { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
}

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<InstitutionPricing> InstitutionPricing => Set<InstitutionPricing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstitutionPricing>(e =>
        {
            e.ToTable("InstitutionPricing", t =>
            {
                t.IsTemporal();
                t.HasTrigger("TR_InstitutionPricing_NoOverlap");
                t.HasCheckConstraint("CK_InstitutionPricing_Period", "[ValidFrom] < [ValidTo]");
            });
            e.Property(x => x.PaymentTerm).HasMaxLength(20);
            e.Property(x => x.DiscountRate).HasPrecision(5, 4);
            e.HasIndex(x => new { x.InstitutionId, x.ValidFrom }).IsUnique();
        });
    }

    public const string NoOverlapTrigger = """
        CREATE TRIGGER dbo.TR_InstitutionPricing_NoOverlap
        ON dbo.InstitutionPricing AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            IF EXISTS (
                SELECT 1
                FROM inserted AS i
                JOIN dbo.InstitutionPricing AS p
                  ON p.InstitutionId = i.InstitutionId
                 AND p.Id <> i.Id
                 AND p.ValidFrom < i.ValidTo
                 AND i.ValidFrom < p.ValidTo)
                THROW 50001, 'Pricing periods overlap for the same institution.', 1;
        END;
        """;
}
