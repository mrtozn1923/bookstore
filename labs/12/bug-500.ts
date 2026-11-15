import { createApp } from "../../src/app.ts";
import type { BookService } from "#services/bookService";
import type { OrderService } from "#services/orderService";
import type { TrackingLookup } from "#routes/shipments";

// A BookService with a bug, to see what the client and the log get for an unexpected error
const buggyBooks = {
    list: async () => {
        throw new TypeError("Cannot read properties of undefined (reading 'priceMinor')");
    },
} as unknown as BookService;

// readiness (post 14), the options (post 15) and the carrier (post 18) are not used here
const app = createApp(
    { books: buggyBooks, orders: {} as OrderService, readiness: async () => ({ status: "ready", checks: {} }), carrier: {} as TrackingLookup, paymentEvents: async () => true, catalogParser: async () => ({ ok: false, reason: "not used" }) },
    { corsOrigins: [], trustProxy: 0, rateLimitPerMinute: 100, verifyToken: async () => ({ userId: "ada", roles: [] }), paymentWebhookSecret: "test-webhook-secret-not-real" },
);
const server = app.listen(0, "127.0.0.1", async () => {
    const { port } = server.address() as { port: number };
    const res = await fetch(`http://127.0.0.1:${port}/api/books`, { headers: { "x-request-id": "req-12z" } });
    console.log(res.status, res.headers.get("content-type"));
    console.log(await res.text());
    server.close();
});
