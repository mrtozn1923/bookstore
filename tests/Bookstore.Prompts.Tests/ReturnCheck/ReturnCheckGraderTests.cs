using Bookstore.Prompts.Evaluation;
using Bookstore.Prompts.Tasks.ReturnCheck;

namespace Bookstore.Prompts.Tests.ReturnCheck;

public class ReturnCheckGraderTests
{
    static readonly ReturnCheckResult Expected = new(IsReturnRequest: true, OrderNumber: "10428");

    [Fact]
    public void Prose_instead_of_json_fails()
    {
        Verdict verdict = ReturnCheckGrader.Grade("Okay, let's see. The customer wants...", Expected, guardMessage: null);

        Assert.False(verdict.Passed);
        Assert.StartsWith("not JSON", verdict.Detail);
    }

    [Fact]
    public void Hash_prefix_in_order_number_is_ignored()
    {
        Verdict verdict = ReturnCheckGrader.Grade("""{"isReturnRequest": true, "orderNumber": "#10428"}""", Expected, guardMessage: null);

        Assert.True(verdict.Passed);
    }

    [Fact]
    public void Wrong_flag_and_wrong_number_are_both_reported()
    {
        Verdict verdict = ReturnCheckGrader.Grade("""{"isReturnRequest": false, "orderNumber": "10429"}""", Expected, guardMessage: null);

        Assert.Equal("isReturnRequest: expected true, got false; orderNumber: expected 10428, got 10429", verdict.Detail);
    }
}
