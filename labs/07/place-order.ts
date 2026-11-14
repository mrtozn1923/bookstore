// Places two orders inside a request context, without HTTP (updated in post 12 for the async OrderService)
import { createServices } from "../../src/composition.ts";
import { runWithRequestContext } from "../../src/context/requestContext.ts";

const { orders } = createServices();

await runWithRequestContext({ requestId: "req-7f3a", method: "POST", path: "/api/orders", signal: AbortSignal.timeout(5000) }, async () => {
    const order = await orders.placeOrder("ada", [{ isbn: "9780441172719", quantity: 2 }, { isbn: "9780141439587", quantity: 1 }]);
    console.log(`placeOrder returned ${order.id}`);
});

await runWithRequestContext({ requestId: "req-8b21", method: "POST", path: "/api/orders", signal: AbortSignal.timeout(5000) }, async () => {
    try {
        await orders.placeOrder("alan", [{ isbn: "9780000000000", quantity: 1 }]);
    } catch (err) {
        console.log(`${(err as Error).name}: ${(err as Error).message}`);
    }
});
