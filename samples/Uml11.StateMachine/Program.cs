using System.Globalization;
using Bookstore.Institutional.StateMachines;
using Stateless;
using Stateless.Graph;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string mode = args.FirstOrDefault() ?? "naive";
DateOnly deliveryDate = new(2026, 12, 21);

switch (mode)
{
    case "naive":
        // #42 altı farklı durumdayken 1 Aralık'ta iptal
        foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
        {
            NaiveOrder order = new(status, deliveryDate);
            try
            {
                order.Cancel(new DateOnly(2026, 12, 1));
                Console.WriteLine($"{status,-25}-> {order.Status}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"{status,-25}-> {ex.Message}");
            }
        }
        break;

    case "switch":
        foreach (OrderRecord order in new[] { new OrderRecord(42, 56_700m), new OrderRecord(43, 6_840m) })
        {
            List<OrderStatus> path = [order.Status];
            order.Fire(OrderTrigger.Submit, new());
            path.Add(order.Status);
            while (order.Status is OrderStatus.PendingApprover or OrderStatus.PendingPurchasingManager)
            {
                order.Fire(OrderTrigger.Approve, new(RequiresPurchasingApproval: order.RequiresPurchasingApproval));
                path.Add(order.Status);
            }
            Console.WriteLine($"#{order.Id} ({order.Total:N0} TRY): {string.Join(" -> ", path)}");
        }
        OrderRecord o42 = new(42, 56_700m) { Status = OrderStatus.Approved };
        Try("#42 again", () => o42.Fire(OrderTrigger.Approve, new()), () => o42.Status);
        Try("#42 cancel by Ada after delivery date", () => o42.Fire(OrderTrigger.Cancel, new(DeliveryDateReached: true)),
            () => o42.Status);
        Try("#42 cancel by Margaret after delivery date",
            () => o42.Fire(OrderTrigger.Cancel, new(DeliveryDateReached: true, ByPortalAdmin: true)), () => o42.Status);
        break;

    case "stateless":
    {
        OrderRecord order = new(42, 56_700m);
        List<string> log = [];
        TransitionContext context = new();
        StateMachine<OrderState, OrderTrigger> machine = OrderStateMachine.Create(order, context, log);
        machine.Fire(OrderTrigger.Submit);
        machine.Fire(OrderTrigger.Approve);
        Console.WriteLine($"IsInState(PendingApproval): {machine.IsInState(OrderState.PendingApproval)}");
        Console.WriteLine($"Permitted: {string.Join(", ", await machine.GetPermittedTriggersAsync())}");
        machine.Fire(OrderTrigger.Approve);
        machine.Fire(OrderTrigger.Cancel);
        log.ForEach(Console.WriteLine);
        Console.WriteLine($"Stored status: {order.Status}");
        Try("", () => machine.Fire(OrderTrigger.Approve), () => machine.State);
        break;
    }

    case "mermaid":
        Console.WriteLine(MermaidGraph.Format(OrderStateMachine.Create(new(42, 56_700m), new(), []).GetInfo()));
        break;

    case "overlap":
    {
        OrderRecord order = new(42, 56_700m) { Status = OrderStatus.PendingApprover };
        Try("", () => OrderStateMachine.CreateOverlapping(order).Fire(OrderTrigger.Approve), () => order.Status);
        break;
    }
}

// Geçişi dener; başarılıysa yeni durumu, değilse exception mesajını yazar
static void Try(string label, Action action, Func<object> state)
{
    string prefix = label.Length > 0 ? $"{label}: " : "";
    try
    {
        action();
        Console.WriteLine(prefix + state());
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine(prefix + (label.Length > 0 ? ex.Message : $"{ex.GetType().Name}: {ex.Message}"));
    }
}
