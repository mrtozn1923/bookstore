// Places two orders inside a request context, without HTTP (updated in post 12 for the async OrderService,
// in post 13 to build in-memory services itself now that createServices needs SQL Server)
import type { Book } from "#domain/book";
import books from "#db/books.json" with { type: "json" };
import { InMemoryBookRepository } from "#repositories/bookRepository";
import { InMemoryOrderRepository } from "#repositories/orderRepository";
import { OrderService } from "#services/orderService";
import { registerOrderNotifications } from "#services/notificationService";
import { runWithRequestContext } from "../../src/context/requestContext.ts";

const catalog = books as Book[];
const orders = new OrderService(new InMemoryBookRepository(catalog), new InMemoryOrderRepository(catalog, new Map(catalog.map((b) => [b.isbn, 20]))));
registerOrderNotifications(orders);

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
