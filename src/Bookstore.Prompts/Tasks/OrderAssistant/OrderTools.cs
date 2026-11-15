using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tasks.OrderAssistant;

// Tools act for the signed-in customer only; the model never passes a customer id
public static class OrderTools
{
    public static List<AITool> Create(OrderStore store, string customerId, bool approvalInCode = false)
    {
        AIFunction cancel = AIFunctionFactory.Create(
            (string orderNumber) => store.Cancel(customerId, orderNumber),
            "cancel_order", "Cancels one of the customer's orders that has not been shipped yet.");
        return
        [
            AIFunctionFactory.Create(
                (string orderNumber) => store.Describe(customerId, orderNumber),
                "get_order", "Returns the status, books and shipping address of one of the customer's orders."),
            AIFunctionFactory.Create(
                (string orderNumber, string newAddress) => store.ChangeAddress(customerId, orderNumber, newAddress),
                "change_shipping_address", "Changes the shipping address of an order that has not been shipped yet."),
            approvalInCode ? new ApprovalRequiredAIFunction(cancel) : cancel,
            AIFunctionFactory.Create(
                (string orderNumber, string reason) => store.RequestReturn(customerId, orderNumber, reason),
                "create_return_request",
                "Creates a return request for a delivered order. A person from the returns team reviews it; this tool does not approve returns or refunds."),
            AIFunctionFactory.Create(
                (string summary) => store.HandOff(summary),
                "hand_off_to_person", "Passes the conversation to a customer service person, with a short summary in English."),
        ];
    }
}
