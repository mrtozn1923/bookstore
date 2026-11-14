// Places two orders inside a request context, without HTTP; POST /api/orders comes later in the series
import { orderService, UnknownBookError } from "#services/orderService";
import { registerOrderNotifications } from "#services/notificationService";
import { runWithRequestContext } from "../../src/context/requestContext.ts";

registerOrderNotifications(orderService);

await runWithRequestContext({ requestId: "req-7f3a", method: "POST", path: "/api/orders", signal: AbortSignal.timeout(5000) }, async () => {
    const order = orderService.placeOrder("ada", [{ isbn: "9780441172719", quantity: 2 }, { isbn: "9780141439587", quantity: 1 }]);
    console.log(`placeOrder returned ${order.id}`);
});

runWithRequestContext({ requestId: "req-8b21", method: "POST", path: "/api/orders", signal: AbortSignal.timeout(5000) }, () => {
    try {
        orderService.placeOrder("alan", [{ isbn: "9780000000000", quantity: 1 }]);
    } catch (err) {
        console.log(`${(err as Error).name}: ${(err as Error).message}`);
    }
});
