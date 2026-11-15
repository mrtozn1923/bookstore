using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Orders;
using Bookstore.Ordering.Pricing;
using static Bookstore.Ordering.Orders.OrderStatus;

namespace Bookstore.Ordering.Tests;

public class ResultTests
{
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);

    [Fact]
    public void Bind_stops_at_the_first_failure()
    {
        int calls = 0;
        Result<int> failed = new Error("first", "first failure");

        Result<int> next = failed.Bind(x => { calls++; return Result<int>.Success(x + 1); });

        Assert.False(next.IsSuccess);
        Assert.Equal("first", Assert.Single(next.Errors).Code);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Ensure_turns_a_success_into_a_failure()
    {
        Result<int> result = Result<int>.Success(25).Ensure(q => q <= 20, q => new Error("quantity.range", $"{q} > 20"));

        Assert.Equal("quantity.range", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Validate_collects_every_field_error()
    {
        Result<ValidOrderRequest> result = OrderValidation.Validate(new("ada.example.com", "9780441013599", 0, null));

        Assert.Equal(["email.invalid", "isbn.checksum", "quantity.range"], result.Errors.Select(e => e.Code));
    }

    [Fact]
    public void ValidateFirstError_reports_only_one_error() =>
        Assert.Single(OrderValidation.ValidateFirstError(new("ada.example.com", "9780441013599", 0, null)).Errors);

    [Theory]
    [InlineData("9780441013593", true)]
    [InlineData("978-0-14-143958-7", true)]
    [InlineData("9780441013599", false)]
    [InlineData("97804410135", false)]
    [InlineData(null, false)]
    public void Isbn_check_digit_is_verified(string? isbn, bool valid) =>
        Assert.Equal(valid, OrderValidation.ValidateIsbn(isbn).IsSuccess);

    [Fact]
    public void A_failure_needs_at_least_one_error() =>
        Assert.Throws<ArgumentException>(() => Result<int>.Failure([]));

    [Fact]
    public void Shipped_order_cannot_be_cancelled() =>
        Assert.Equal("order.shipped", Assert.Single(OrderCancellation.TryCancel(new Shipped("PAY-1", "TRK-1"), "late").Errors).Code);

    public static TheoryData<PlaceOrderRequest> Requests => new()
    {
        new PlaceOrderRequest("ada@example.com", "9780441013593", 2, "autumn10"),
        new PlaceOrderRequest("alan@example.com", "9780261103344", 1, null),
        new PlaceOrderRequest("grace@example.com", "9780441013593", 5, null),
        new PlaceOrderRequest("ada.example.com", "9780441013599", 0, null)
    };

    [Theory]
    [MemberData(nameof(Requests))]
    public async Task Railway_and_await_versions_agree(PlaceOrderRequest request)
    {
        Result<PlacedOrder> railway = await CreatePlacement().PlaceAsync(request, TestContext.Current.CancellationToken);
        Result<PlacedOrder> awaited = await CreatePlacement().PlaceWithAwaitAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(railway.ToString(), awaited.ToString());
    }

    private static OrderPlacement CreatePlacement() => new(
        new Dictionary<string, CatalogBook> { ["9780441013593"] = new("9780441013593", "Dune", 189.50m, false) },
        new InMemoryStock(new() { ["9780441013593"] = 3 }),
        new Campaign("AUTUMN10", 0.10m, new(2026, 11, 1, 0, 0, 0, Istanbul), new(2026, 12, 1, 0, 0, 0, Istanbul)),
        new FixedClock(new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero)),
        () => "BS-TEST",
        TextWriter.Null);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
