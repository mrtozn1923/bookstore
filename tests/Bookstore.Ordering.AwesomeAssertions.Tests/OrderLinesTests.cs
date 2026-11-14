using static Bookstore.Ordering.AwesomeAssertions.Tests.Records;

namespace Bookstore.Ordering.AwesomeAssertions.Tests;

public sealed class OrderLinesTests
{
    private readonly OrderDto dto = OrderMapper.ToDto(SavedOrder());

    [Fact]
    public void Lines_collection_assertions()
    {
        dto.Lines.Should().HaveCount(3)
            .And.OnlyHaveUniqueItems(l => l.Isbn)
            .And.BeInAscendingOrder(l => l.Title, StringComparer.Ordinal)
            .And.AllSatisfy(l => l.Quantity.Should().BePositive());

        dto.Lines.Should().ContainSingle(l => l.Isbn == Dune)
            .Which.Quantity.Should().Be(2);

        dto.Lines.Should().ContainEquivalentOf(new { Isbn = Emma, UnitPrice = 8.75m })
            .And.NotContain(l => l.UnitPrice <= 0m);
    }
}
