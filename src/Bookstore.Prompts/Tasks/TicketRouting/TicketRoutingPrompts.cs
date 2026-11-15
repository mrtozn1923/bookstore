using System.Text;
using System.Text.Json;
using Bookstore.Prompts.Evaluation;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tasks.TicketRouting;

public sealed record TicketRoute(string Label, double? Confidence);

public static class TicketRoutingPrompts
{
    // v1: label names only, label is a free string
    public static readonly PromptVersion V1 = new(
        Task: "ticket-routing",
        Version: "v1",
        System: "Route the Bookstore customer ticket to one team: Returns, Shipping, Payments, Institutional or Human. Reply with JSON: {\"label\": \"...\"}",
        BuildUser: ticket => ticket,
        Format: ChatResponseFormat.Json);

    // Boundary cases that are not in the test set
    static readonly (string Ticket, string Label)[] Examples =
    [
        ("Gönderdiğim iade kargosu size ulaştı mı?", "Returns"),
        ("Paket geldi ama içinden başka bir kitap çıktı.", "Returns"),
        ("Kurumsal siparişimizin faturası hâlâ gelmedi.", "Institutional"),
        ("İndirim kodum çalışmadı, ayrıca kargom da gecikti.", "Human"),
    ];

    // v2: definitions + schema enum; v3: v2 + boundary examples. reverse changes only the label order.
    public static PromptVersion Build(bool withExamples, bool reverse = false, bool askConfidence = false)
    {
        List<LabelDefinition> labels = reverse ? [.. TicketLabels.All.Reverse()] : [.. TicketLabels.All];

        StringBuilder system = new();
        system.AppendLine("You route one Bookstore customer ticket to exactly one team.");
        system.AppendLine();
        system.AppendLine("## Teams");
        foreach (LabelDefinition label in labels)
        {
            system.AppendLine($"- {label.Name}: {label.Definition}");
        }
        system.AppendLine();
        system.AppendLine($"If no definition clearly fits, or two fit equally, choose {TicketLabels.Human}.");
        if (withExamples)
        {
            system.AppendLine();
            system.AppendLine("## Examples");
            foreach ((string ticket, string label) in Examples)
            {
                system.AppendLine($"<ticket>{ticket}</ticket> -> {label}");
            }
        }
        if (askConfidence)
        {
            system.AppendLine();
            system.AppendLine("Also give your confidence that the label is correct, from 0 to 1.");
        }

        string version = (withExamples ? "v3" : "v2") + (reverse ? "-reversed" : "") + (askConfidence ? "-confidence" : "");
        return new PromptVersion(
            Task: "ticket-routing",
            Version: version,
            System: system.ToString().TrimEnd(),
            BuildUser: ticket => $"<ticket>\n{ticket}\n</ticket>",
            Format: ChatResponseFormat.ForJsonSchema(Schema(labels, askConfidence), "ticket_route"));
    }

    // The label list in the schema follows the same order as the prompt
    static JsonElement Schema(IEnumerable<LabelDefinition> labels, bool askConfidence)
    {
        Dictionary<string, object> properties = new()
        {
            ["label"] = new { type = "string", @enum = labels.Select(l => l.Name).ToArray() },
        };
        List<string> required = ["label"];
        if (askConfidence)
        {
            properties["confidence"] = new { type = "number", minimum = 0, maximum = 1 };
            required.Add("confidence");
        }
        return JsonSerializer.SerializeToElement(new { type = "object", properties, required, additionalProperties = false });
    }
}
