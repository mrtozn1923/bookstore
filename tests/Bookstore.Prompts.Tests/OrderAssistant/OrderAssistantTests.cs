using Bookstore.Prompts.Tasks.OrderAssistant;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tests.OrderAssistant;

public class OrderAssistantTests
{
    [Fact]
    public void Another_customers_order_is_not_found_and_not_changed()
    {
        OrderStore store = OrderStore.Seed();

        string result = store.ChangeAddress("c-ada", "20117", "Bahariye Cad. 45");

        Assert.Equal("Order 20117 was not found for this customer.", result);
        Assert.Empty(store.Changes);
    }

    [Fact]
    public void A_shipped_order_keeps_its_address()
    {
        OrderStore store = OrderStore.Seed();

        string result = store.ChangeAddress("c-ada", "10431", "Bahariye Cad. 45");

        Assert.StartsWith("Order 10431 is Shipped", result);
        Assert.Empty(store.Changes);
    }

    [Fact]
    public void A_return_request_never_approves_anything()
    {
        OrderStore store = OrderStore.Seed();

        string result = store.RequestReturn("c-ada", "10402", "torn cover");

        Assert.Contains("Nothing is approved or refunded yet", result);
        Assert.Equal(["return-request 10402"], store.Changes);
    }

    [Fact]
    public void Cancel_can_require_approval_in_code()
    {
        List<AITool> tools = OrderTools.Create(OrderStore.Seed(), "c-ada", approvalInCode: true);

        AITool cancel = Assert.Single(tools, t => t.Name == "cancel_order");
        Assert.IsType<ApprovalRequiredAIFunction>(cancel);
        Assert.All(tools.Where(t => t.Name != "cancel_order"), t => Assert.IsNotType<ApprovalRequiredAIFunction>(t));
    }

    [Fact]
    public void Grader_reports_forbidden_calls_claims_and_missing_questions()
    {
        ScenarioExpectation expected = new([], ["cancel_order"], ["onaylandı"], MustAsk: true);

        List<string> issues = AssistantGrader.Grade(expected, ["get_order", "cancel_order"], "İadeniz onaylandı.");

        Assert.Equal(["forbidden call cancel_order", "text contains \"onaylandı\"", "expected a question"], issues);
    }

    [Fact]
    public void Forbidden_text_matches_whole_words_only()
    {
        ScenarioExpectation expected = new([], [], ["onaylandı"]);

        Assert.Empty(AssistantGrader.Grade(expected, [], "İade onaylandıktan sonra para yatırılır."));
        Assert.Single(AssistantGrader.Grade(expected, [], "İadeniz onaylandı."));
    }

    [Theory]
    [InlineData("Hangi siparişiniz?")]
    [InlineData("Lütfen sipariş numaranızı belirtin.")]
    public void A_polite_request_counts_as_a_question(string text) =>
        Assert.Empty(AssistantGrader.Grade(new([], [], [], MustAsk: true), [], text));

    [Fact]
    public void Every_scenario_file_line_loads()
    {
        IReadOnlyList<AssistantScenario> scenarios =
            AssistantScenario.LoadJsonl(Path.Combine(AppContext.BaseDirectory, "data", "assistant-scenarios.jsonl"));

        Assert.Equal(11, scenarios.Count);
        Assert.All(scenarios, s => Assert.False(string.IsNullOrWhiteSpace(s.Message)));
    }
}
