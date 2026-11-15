using System.Text.Json;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Bookstore.Api.Returns;

namespace Bookstore.Api.Tests;

public sealed class ReturnClassifierTests
{
    [Theory]
    [InlineData("Damaged", ReturnReason.Damaged)]
    [InlineData(" wrongitem.\n", ReturnReason.WrongItem)]
    [InlineData("The book is damaged", null)]
    public void ParseLabel_AcceptsOnlyOneLabel(string text, ReturnReason? expected) =>
        Assert.Equal(expected, ReturnClassifier.ParseLabel(text));

    [Fact]
    public void BuildRequest_SendsEffortInOutputConfig()
    {
        MessageCreateParams request = ReturnClassifier.BuildRequest("It arrived wet.", new ClassifierSettings("claude-haiku-5-5", Effort.Low));
        JsonElement body = JsonSerializer.SerializeToElement(request.RawBodyData);

        Assert.Equal("low", body.GetProperty("output_config").GetProperty("effort").GetString());
        Assert.False(body.TryGetProperty("thinking", out _));
    }

    [Fact]
    public void BuildRequest_HaikuWithThinkingDisabled()
    {
        MessageCreateParams request = ReturnClassifier.BuildRequest("It arrived wet.",
            new ClassifierSettings("claude-haiku-5-5", Effort.Low, new ThinkingConfigDisabled()));
        JsonElement body = JsonSerializer.SerializeToElement(request.RawBodyData);

        Assert.Equal("disabled", body.GetProperty("thinking").GetProperty("type").GetString());
    }
}

public sealed class ClaudePricingTests
{
    [Fact]
    public void EstimateUsd_UsesBaseInputAndOutputPrices()
    {
        // 1M input + 1M output on Sonnet 5.5: $2 + $10.
        decimal cost = ClaudePricing.EstimateUsd("claude-sonnet-5-5", new TokenUsage(1_000_000, 1_000_000, 0, 0, 0));
        Assert.Equal(12.00m, cost);
    }

    [Fact]
    public void EstimateUsd_HaikuPromptOver100kUsesTheHigherPrice()
    {
        decimal shortPrompt = ClaudePricing.EstimateUsd("claude-haiku-5-5", new TokenUsage(100_000, 0, 0, 0, 0));
        decimal longPrompt = ClaudePricing.EstimateUsd("claude-haiku-5-5", new TokenUsage(100_001, 0, 0, 0, 0));

        Assert.Equal(0.01m, shortPrompt);
        Assert.Equal(0.0500005m, longPrompt);
    }
}
