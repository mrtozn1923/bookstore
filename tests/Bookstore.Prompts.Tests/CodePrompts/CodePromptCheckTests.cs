using Bookstore.Prompts.Tasks.CodePrompts;

namespace Bookstore.Prompts.Tests.CodePrompts;

public class CodePromptCheckTests
{
    [Theory]
    [InlineData(0, "throws")]
    [InlineData(1, "0")]
    [InlineData(19, "0")]
    [InlineData(20, "0.10")]
    [InlineData(5000, "0.10")]
    [InlineData(5001, "throws")]
    public void Discount_follows_the_specification(int quantity, string expected) =>
        Assert.Equal(expected, TestGenChecker.Actual(quantity));

    [Theory]
    [InlineData("10%", "0.10")]
    [InlineData("0.1", "0.10")]
    [InlineData("ArgumentOutOfRangeException", "throws")]
    [InlineData("0.00", "0")]
    public void Expected_values_are_normalized(string expected, string normalized) =>
        Assert.Equal(normalized, TestGenChecker.Normalize(expected));

    [Fact]
    public void Wrong_oracles_and_missing_boundaries_are_reported()
    {
        GeneratedTests tests = new(null, [new(20, "0", null), new(1, "0", null)]);

        TestGenReport report = TestGenChecker.Check(tests);

        Assert.Equal(["20: expected 0, method returns 0.10"], report.WrongExpected);
        Assert.Equal([0, 19, 5000, 5001], report.MissingBoundaries);
    }

    [Fact]
    public void A_finding_counts_only_when_its_line_is_in_the_diff()
    {
        string diff = "--- a/x.cs\n+++ b/x.cs\n-    return quantity >= MinimumQuantity ? Rate : 0m;\n+    static bool IsEligible(int quantity) => quantity > MinimumQuantity;\n";
        ReviewResult review = new(
        [
            new("static bool IsEligible(int quantity) => quantity > MinimumQuantity;", "off by one", "high", true),
            new("if (quantity == null) return 0;", "null check", "low", false),
        ]);

        ReviewScore score = ReviewChecker.Score(review, diff, "quantity > MinimumQuantity");

        Assert.Equal(new ReviewScore(2, 1, true), score);
    }
}
