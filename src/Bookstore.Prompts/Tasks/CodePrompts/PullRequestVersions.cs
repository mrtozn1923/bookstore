namespace Bookstore.Prompts.Tasks.CodePrompts;

// The two diffs under review, compiled so their behavior can be compared with the model's claims
public static class PullRequestVersions
{
    // discount-off-by-one.diff
    public static decimal OffByOne(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantity, InstitutionalDiscount.MaximumQuantity);
        return IsEligible(quantity) ? InstitutionalDiscount.Rate : 0m;
    }

    static bool IsEligible(int quantity) => quantity > InstitutionalDiscount.MinimumQuantity;

    // discount-rename.diff
    public static decimal Rename(int copies)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(copies, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(copies, InstitutionalDiscount.MaximumQuantity);
        return copies >= InstitutionalDiscount.MinimumQuantity ? InstitutionalDiscount.Rate : 0m;
    }
}
