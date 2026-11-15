using Bookstore.Prompts.Evaluation;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tasks.ReturnCheck;

public static class ReturnCheckPrompts
{
    // v1: the prompt that "worked" on the first message we tried
    public static readonly PromptVersion V1 = new(
        Task: "return-check",
        Version: "v1",
        System: "You help the Bookstore customer service team.",
        BuildUser: message =>
            $"Does the customer want to return a book? Reply with JSON containing isReturnRequest and orderNumber.\n\n{message}",
        Format: ChatResponseFormat.Json);

    // v2: definitions, a null rule and an output contract
    public static readonly PromptVersion V2 = new(
        Task: "return-check",
        Version: "v2",
        System: """
            You classify one customer message for the Bookstore customer service team.

            ## Definitions
            - isReturnRequest is true when the customer wants to start a new return or an exchange
              for a book they received: damaged, wrong edition, missing pages, changed their mind,
              or they use their legal right of withdrawal.
            - isReturnRequest is false for questions about the return policy, the status of an
              earlier return or refund, shipping delays, invoices and new orders.
            - orderNumber is the 5-digit Bookstore order number in the message, without "#".
              ISBNs (13 digits), prices and phone numbers are not order numbers.
            - If the message contains no order number, orderNumber is null. Never guess one.

            ## Output
            Return only a JSON object that matches the schema. No explanation.
            """,
        BuildUser: message => $"<message>\n{message}\n</message>",
        Format: ChatResponseFormat.ForJsonSchema<ReturnCheckResult>());

    // v3: v2 plus two boundary examples (not taken from the test set)
    public static readonly PromptVersion V3 = V2 with
    {
        Version = "v3",
        System = V2.System + """

            ## Examples
            <message>The refund for the book I sent back two weeks ago is still missing. Order 20117.</message>
            {"isReturnRequest": false, "orderNumber": "20117"}

            <message>I paid 389,90 TL and the cover is torn. I want to send it back.</message>
            {"isReturnRequest": true, "orderNumber": null}
            """,
    };
}
