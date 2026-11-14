using static Bookstore.Ordering.AwesomeAssertions.Tests.Records;

namespace Bookstore.Ordering.AwesomeAssertions.Tests;

public sealed class OrderMappingTests
{
    private readonly OrderRecord saved = SavedOrder();

    [Fact]
    public void Reloaded_order_is_equivalent_to_the_saved_one()
    {
        OrderRecord reloaded = SavedOrder();

        reloaded.Should().BeEquivalentTo(saved);
    }

    [Fact]
    public void Dto_carries_everything_but_ids_and_audit_fields()
    {
        OrderDto dto = OrderMapper.ToDto(saved);

        dto.Should().BeEquivalentTo(saved, options => options
            .Excluding(o => o.Id)
            .Excluding(o => o.CreatedAtUtc)
            .Excluding(o => o.CreatedBy)
            .Excluding(o => o.UpdatedAtUtc)
            .For(o => o.Lines).Exclude(l => l.Id)
            .Using<decimal>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.005m))
            .WhenTypeIs<decimal>());
    }

    [Fact]
    public void Dto_lines_are_listed_by_title()
    {
        OrderDto dto = OrderMapper.ToDto(saved);

        dto.Lines.Should().BeEquivalentTo(
            [
                new { Title = "Dune", Quantity = 2, UnitPrice = 12.50m },
                new { Title = "Emma", Quantity = 1, UnitPrice = 8.75m },
                new { Title = "The Hobbit", Quantity = 1, UnitPrice = 9.90m },
            ],
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void Dto_total_and_status()
    {
        OrderDto dto = OrderMapper.ToDto(saved);

        dto.Total.Should().Be(43.65m);
        dto.Status.Should().Be(OrderStatus.Placed);
        dto.CustomerEmail.Should().EndWith("@example.com").And.NotContainAny(" ", "\t");
    }
}
