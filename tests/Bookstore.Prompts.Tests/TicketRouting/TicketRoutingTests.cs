using System.Text.Json;
using Bookstore.Prompts.Evaluation;
using Bookstore.Prompts.Tasks.TicketRouting;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tests.TicketRouting;

public class TicketRoutingTests
{
    [Fact]
    public void Reversed_prompt_reverses_the_schema_enum_too()
    {
        PromptVersion reversed = TicketRoutingPrompts.Build(withExamples: false, reverse: true);

        JsonElement schema = ((ChatResponseFormatJson)reversed.Format!).Schema!.Value;
        string[] labels = [.. schema.GetProperty("properties").GetProperty("label").GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString()!)];

        Assert.Equal(["Human", "Institutional", "Payments", "Shipping", "Returns"], labels);
        Assert.Equal("v2-reversed", reversed.Version);
    }

    [Fact]
    public void Every_label_definition_is_in_the_prompt()
    {
        PromptVersion prompt = TicketRoutingPrompts.Build(withExamples: true);

        Assert.All(TicketLabels.All, label => Assert.Contains($"- {label.Name}: {label.Definition}", prompt.System));
    }

    [Fact]
    public void Matrix_counts_labels_outside_the_set_in_the_last_column()
    {
        ConfusionMatrix matrix = new(["Returns", "Human"]);
        matrix.Add("Returns", "Returns");
        matrix.Add("Human", "Refund");

        string[] lines = matrix.Render().Split('\n');

        Assert.Equal("Human               .     .     1", lines[2].TrimEnd());
        Assert.Equal("accuracy 1/2", lines[^1]);
    }

    // Renaming or splitting a label must break this test until the test set is updated
    [Fact]
    public void Test_set_uses_only_current_labels()
    {
        IReadOnlyList<TestCase<string>> cases =
            TestSet.LoadJsonl<string>(Path.Combine(AppContext.BaseDirectory, "data", "tickets.jsonl"));

        Assert.All(cases, c => Assert.Contains(c.Expected, TicketLabels.All.Select(l => l.Name)));
        Assert.All(TicketLabels.All, l => Assert.Contains(cases, c => c.Expected == l.Name));
    }
}
