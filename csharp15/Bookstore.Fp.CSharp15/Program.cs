using Bookstore.Fp.CSharp15;

switch (args.FirstOrDefault())
{
    case "08-closed": Post08.ClosedHierarchy(); break;
    case "08-result": Post08.Results(); break;
    case "08-json": Post08.Json(); break;
    default:
        Paid paid = new("PAY-1001");
        OrderStatus[] history = [new Pending(), paid, new Shipped("PAY-1001", "TRK-778"), OrderWorkflow.Cancel(paid, "customer request")];

        foreach (OrderStatus status in history)
            Console.WriteLine(OrderWorkflow.Describe(status));
        break;
}
