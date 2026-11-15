using System.ComponentModel;
using Bookstore.Prompts.Evaluation;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tasks.LongDocument;

public sealed record ContractQuote(
    [property: Description("Clause number as written in the contract, e.g. \"17.3\" or \"Ek-3.1\".")]
    string Clause,
    [property: Description("The clause text copied word for word.")]
    string Text);

// Quotes come before the answer in the schema, so the model writes them first
public sealed record GroundedAnswer(List<ContractQuote> Quotes, string Answer);

public sealed record PlainAnswer(string Answer);

public enum Placement { DocumentFirst, QuestionFirst }

public static class ContractPrompts
{
    public const string NotFound = "NOT_FOUND";

    const string Instructions = """
        You answer questions from Bookstore's purchasing team about the supplier contract
        between Bookstore and its book supplier. Use only the contract, never outside knowledge.
        Annex protocols (Ek) at the end can change earlier clauses; check them before answering.
        """;

    const string GroundedRules = $"""
        First copy every passage that answers the question into Quotes, word for word, with
        its clause number. Then write a short Turkish answer based only on those quotes.
        If the contract does not answer the question, return no quotes and the answer {NotFound}.
        """;

    // v1: answer only. v2: quotes first, then the answer.
    public static PromptVersion Build(ContractDocument contract, bool grounded, Placement placement)
    {
        string document = $"<document source=\"{contract.Source}\">\n{contract.Text}\n</document>";
        string rules = grounded ? GroundedRules : $"Write a short Turkish answer. If the contract does not answer the question, answer {NotFound}.";
        return new PromptVersion(
            Task: "contract-qa",
            Version: (grounded ? "v2" : "v1") + "-" + placement,
            System: Instructions + "\n" + rules,
            // Stable part (the document) first, the changing question last, or the other way round
            BuildUser: question => placement == Placement.DocumentFirst
                ? $"{document}\n\n<question>{question}</question>"
                : $"<question>{question}</question>\n\n{document}",
            Format: grounded ? ChatResponseFormat.ForJsonSchema<GroundedAnswer>() : ChatResponseFormat.ForJsonSchema<PlainAnswer>());
    }

    // Map step: collect return-related clauses from one chunk
    public static readonly PromptVersion Map = new(
        Task: "contract-map",
        Version: "v2",
        System: """
            You read one part of Bookstore's supplier contract. Copy, word for word, the clauses
            in this part that are about BOOKSTORE returning books to the supplier: notice
            deadlines, limits on unsold stock, who pays return shipping, credit notes, and any
            clause that explicitly changes one of those return clauses.
            General clauses about notices, documents, cooperation, payments or penalties do not
            count. Clause is only the number, such as "17.2" or "Ek-3.1".
            Return at most 8 clauses, and an empty list if this part has none.
            """,
        BuildUser: chunk => $"<part>\n{chunk}\n</part>",
        Format: ChatResponseFormat.ForJsonSchema<MapResult>());
}

public sealed record MapResult(List<ContractQuote> Quotes);
