namespace Bookstore.Ordering;

public enum BookFormat
{
    Printed,
    EBook,
}

public enum ReturnDecision
{
    Approve,
    ManualReview,
    Reject,
}

public sealed record ReturnRequest(BookFormat Format, int DaysSinceDelivery, bool DamagedInShipping, bool Downloaded);

public static class ReturnApprovalPolicy
{
    public static ReturnDecision Decide(ReturnRequest request) => request switch
    {
        { Format: BookFormat.EBook, Downloaded: true } => ReturnDecision.Reject,
        { Format: BookFormat.EBook, DaysSinceDelivery: <= 14 } => ReturnDecision.Approve,
        { Format: BookFormat.EBook } => ReturnDecision.Reject,
        { DamagedInShipping: true, DaysSinceDelivery: <= 30 } => ReturnDecision.Approve,
        { DamagedInShipping: true } => ReturnDecision.ManualReview,
        { DaysSinceDelivery: <= 14 } => ReturnDecision.Approve,
        { DaysSinceDelivery: <= 30 } => ReturnDecision.ManualReview,
        _ => ReturnDecision.Reject,
    };
}
