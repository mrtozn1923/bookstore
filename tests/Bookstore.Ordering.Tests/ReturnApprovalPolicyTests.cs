namespace Bookstore.Ordering.Tests;

public sealed class ReturnApprovalPolicyTests
{
    // Extended-entry table: day-count partitions represented by their boundary values
    public static IEnumerable<TheoryDataRow<BookFormat, int, bool, bool, ReturnDecision>> DecisionTable =>
    [
        new(BookFormat.EBook, 14, false, false, ReturnDecision.Approve) { Label = "E1" },
        new(BookFormat.EBook, 15, false, false, ReturnDecision.Reject) { Label = "E2" },
        new(BookFormat.EBook, 3, false, true, ReturnDecision.Reject) { Label = "E3" },
        new(BookFormat.Printed, 30, true, false, ReturnDecision.Approve) { Label = "P1" },
        new(BookFormat.Printed, 31, true, false, ReturnDecision.ManualReview) { Label = "P2" },
        new(BookFormat.Printed, 14, false, false, ReturnDecision.Approve) { Label = "P3" },
        new(BookFormat.Printed, 15, false, false, ReturnDecision.ManualReview) { Label = "P4 lower" },
        new(BookFormat.Printed, 30, false, false, ReturnDecision.ManualReview) { Label = "P4 upper" },
        new(BookFormat.Printed, 31, false, false, ReturnDecision.Reject) { Label = "P5" },
    ];

    [Theory]
    [MemberData(nameof(DecisionTable))]
    public void Return_decision_follows_the_decision_table(
        BookFormat format, int daysSinceDelivery, bool damagedInShipping, bool downloaded, ReturnDecision expected)
    {
        ReturnDecision decision = ReturnApprovalPolicy.Decide(
            new ReturnRequest(format, daysSinceDelivery, damagedInShipping, downloaded));

        Assert.Equal(expected, decision);
    }
}
