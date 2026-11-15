namespace Bookstore.Ordering.Tests;

public sealed class CheckoutPlannerPairwiseTests
{
    // PICT output: the first line holds the parameter names, every following line is a test case (tab-separated)
    public static IEnumerable<TheoryDataRow<CustomerType, PaymentMethod, DeliveryType, string>> Pairs =>
        File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Pict", "checkout-pairs.txt"))
            .Skip(1)
            .Select(line => line.Split('\t'))
            .Select(v => new TheoryDataRow<CustomerType, PaymentMethod, DeliveryType, string>(
                Enum.Parse<CustomerType>(v[0]), Enum.Parse<PaymentMethod>(v[1]), Enum.Parse<DeliveryType>(v[2]), v[3]));

    [Theory]
    [MemberData(nameof(Pairs))]
    public void Every_valid_pair_gets_a_plan(
        CustomerType customer, PaymentMethod payment, DeliveryType delivery, string language)
    {
        CheckoutPlan plan = CheckoutPlanner.Plan(new CheckoutRequest(customer, payment, delivery, language));

        // Oracle: no separate expected value per row; properties that must hold for every combination
        Assert.EndsWith($".{language}", plan.ConfirmationTemplate);
        Assert.InRange(plan.DispatchInBusinessDays, 0, CheckoutPlanner.MaxDispatchInBusinessDays);
    }
}
