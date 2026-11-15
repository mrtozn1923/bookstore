using Bookstore.Fp.CSharp15;

Paid paid = new("PAY-1001");
OrderStatus[] history = [new Pending(), paid, new Shipped("PAY-1001", "TRK-778"), OrderWorkflow.Cancel(paid, "customer request")];

foreach (OrderStatus status in history)
    Console.WriteLine(OrderWorkflow.Describe(status));
