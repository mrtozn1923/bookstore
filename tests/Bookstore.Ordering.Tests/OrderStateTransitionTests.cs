using Microsoft.Extensions.Time.Testing;
using static Bookstore.Ordering.Tests.OrderStateModel;

namespace Bookstore.Ordering.Tests;

public sealed class OrderStateTransitionTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero));

    private Order OrderIn(OrderState state)
    {
        Order order = new(_clock);
        foreach (OrderEvent e in ShortestPath[state])
        {
            order.Fire(e);
        }

        return order;
    }

    // 0-switch (valid transitions) coverage: each valid transition once
    public static TheoryData<OrderState, OrderEvent, OrderState, bool> ValidTransitions =>
        new(Valid.Select(t => (t.From, t.Event, t.To, t.Refunds)));

    [Theory]
    [MemberData(nameof(ValidTransitions))]
    public void Valid_transition_moves_the_order_to_the_target_state(
        OrderState from, OrderEvent e, OrderState to, bool refunds)
    {
        Order order = OrderIn(from);
        int refundsBefore = order.Actions.Count(a => a == "Refund");

        order.Fire(e);

        Assert.Equal(to, order.State);
        Assert.Equal(refunds ? 1 : 0, order.Actions.Count(a => a == "Refund") - refundsBefore);
    }

    // Empty cells of the state table: 7 states x 7 events - 9 valid transitions = 40 invalid transitions
    public static TheoryData<OrderState, OrderEvent> InvalidTransitions =>
        new(from state in Enum.GetValues<OrderState>()
            from e in Enum.GetValues<OrderEvent>()
            where !Valid.Any(t => t.From == state && t.Event == e)
            select (state, e));

    [Theory]
    [MemberData(nameof(InvalidTransitions))]
    public void Invalid_transition_is_rejected_and_the_state_does_not_change(OrderState from, OrderEvent e)
    {
        Order order = OrderIn(from);

        Assert.Throws<InvalidOperationException>(() => order.Fire(e));
        Assert.Equal(from, order.State);
    }

    // 1-switch coverage: every pair of two consecutive valid transitions
    public static IEnumerable<TheoryDataRow<OrderEvent[], OrderState>> TransitionPairs =>
        from first in Valid
        from second in Valid
        where second.From == first.To
        select new TheoryDataRow<OrderEvent[], OrderState>(
            [.. ShortestPath[first.From], first.Event, second.Event], second.To)
        {
            Label = $"{first.Event} -> {second.Event}",
        };

    [Theory]
    [MemberData(nameof(TransitionPairs))]
    public void Pair_of_transitions_ends_in_the_expected_state(OrderEvent[] events, OrderState expected)
    {
        Order order = new(_clock);

        foreach (OrderEvent e in events)
        {
            order.Fire(e);
        }

        Assert.Equal(expected, order.State);
    }

    // Guard: Return [at most 14 days after delivery]; the boundary and just past it
    [Fact]
    public void Return_is_allowed_exactly_14_days_after_delivery()
    {
        Order order = OrderIn(OrderState.Delivered);
        _clock.Advance(Order.ReturnWindow);

        order.Fire(OrderEvent.Return);

        Assert.Equal(OrderState.Returned, order.State);
    }

    [Fact]
    public void Return_is_rejected_one_tick_after_the_window()
    {
        Order order = OrderIn(OrderState.Delivered);
        _clock.Advance(Order.ReturnWindow + TimeSpan.FromTicks(1));

        Assert.Throws<InvalidOperationException>(() => order.Fire(OrderEvent.Return));
        Assert.Equal(OrderState.Delivered, order.State);
    }
}
