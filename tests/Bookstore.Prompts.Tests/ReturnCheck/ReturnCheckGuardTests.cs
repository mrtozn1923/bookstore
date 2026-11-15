using Bookstore.Prompts.Tasks.ReturnCheck;

namespace Bookstore.Prompts.Tests.ReturnCheck;

public class ReturnCheckGuardTests
{
    [Theory]
    [InlineData("Order 10602.", "10602", "10602")]
    [InlineData("Order #10877", "#10877", "10877")]
    [InlineData("I paid 1.240,50 TL, the books arrived wet.", "12405", null)]
    [InlineData("ISBN 9780141439518, three more copies please.", "14395", null)]
    public void Keeps_only_numbers_copied_from_the_message(string message, string modelValue, string? expected)
    {
        ReturnCheckResult result = ReturnCheckGuard.Apply(new ReturnCheckResult(true, modelValue), message);

        Assert.Equal(expected, result.OrderNumber);
    }
}
