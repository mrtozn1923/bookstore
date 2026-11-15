using Bookstore.Prompts.Evaluation;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tasks.OrderExtraction;

public static class OrderExtractionPrompts
{
    public static readonly PromptVersion V1 = new(
        Task: "order-extraction",
        Version: "v1",
        System: "Extract the book order from the email as JSON.",
        BuildUser: email => email,
        Format: ChatResponseFormat.ForJsonSchema<OrderRequestV1>());

    public static readonly PromptVersion V2 = new(
        Task: "order-extraction",
        Version: "v2",
        System: """
            You read purchase emails that schools and libraries send to Bookstore's
            institutional sales team and extract the order request.

            ## Rules
            - Extract only what the email states. Do not use outside knowledge to fill fields.
            - A field the email does not state is null. Words like "a few", "same as last year"
              or "we will confirm later" are not quantities: use null.
            - Write numbers given in words as digits ("twenty-five" is 25).
            - If the sender changes an earlier quantity, use the new one.
            - ISBN: digits only, without hyphens, and only if it is written in the email body.
              "ISBNs are attached" means null.
            - Copy SourceQuote and DeliveryDateText exactly as they appear in the email.

            Return only a JSON object that matches the schema.
            """,
        BuildUser: email => $"<email>\n{email}\n</email>",
        Format: ChatResponseFormat.ForJsonSchema<OrderRequest>());

    // v3: v2 plus "list every book" and one worked example
    public static readonly PromptVersion V3 = V2 with
    {
        Version = "v3",
        System = V2.System.Replace("Return only a JSON object that matches the schema.", """
            - Every book the sender wants to order goes into Items, even when its quantity is null.
            - Use JSON null, never the string "null".

            ## Example
            <email>
            Sent: 2026-10-05 (Monday)

            Hello, we need twelve copies of Persuasion and some copies of Walden, I will confirm
            the number. ISBN for Walden: 978-0-00-000000-2. Delivery by 20 October please.
            </email>
            {"items": [
              {"title": "Persuasion", "isbn": null, "quantity": 12,
               "sourceQuote": "we need twelve copies of Persuasion"},
              {"title": "Walden", "isbn": "9780000000002", "quantity": null,
               "sourceQuote": "some copies of Walden, I will confirm the number"}],
             "deliveryDateText": "by 20 October", "deliveryDate": "2026-10-20"}

            Return only a JSON object that matches the schema.
            """),
    };
}
