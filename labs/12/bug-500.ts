import { createApp } from "../../src/app.ts";
import type { BookService } from "#services/bookService";
import type { OrderService } from "#services/orderService";

// A BookService with a bug, to see what the client and the log get for an unexpected error
const buggyBooks = {
    list: async () => {
        throw new TypeError("Cannot read properties of undefined (reading 'priceMinor')");
    },
} as unknown as BookService;

// readiness (post 14) and the options (post 15) are not used here
const app = createApp(
    { books: buggyBooks, orders: {} as OrderService, readiness: async () => ({ status: "ready", checks: {} }) },
    { corsOrigins: [], trustProxy: 0, rateLimitPerMinute: 100, verifyToken: async () => ({ userId: "ada", roles: [] }) },
);
const server = app.listen(0, "127.0.0.1", async () => {
    const { port } = server.address() as { port: number };
    const res = await fetch(`http://127.0.0.1:${port}/api/books`, { headers: { "x-request-id": "req-12z" } });
    console.log(res.status, res.headers.get("content-type"));
    console.log(await res.text());
    server.close();
});
