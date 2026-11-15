// The API with in-memory repositories and a fixed test token, but the real CarrierClient: node labs/18/serve.ts
import type { Book } from "#domain/book";
import books from "#db/books.json" with { type: "json" };
import { InMemoryBookRepository } from "#repositories/bookRepository";
import { InMemoryOrderRepository } from "#repositories/orderRepository";
import { BookService } from "#services/bookService";
import { OrderService } from "#services/orderService";
import { createApp } from "../../src/app.ts";
import { InvalidTokenError } from "../../src/auth/tokenVerifier.ts";
import { logger } from "../../src/diagnostics/logger.ts";
import { CarrierClient, createCarrierAgent } from "../../src/integrations/carrierClient.ts";

const catalog = books as Book[];
const bookRepository = new InMemoryBookRepository(catalog);
const carrierAgent = createCarrierAgent();
const carrier = new CarrierClient({
    baseUrl: process.env.CARRIER_API_URL ?? "http://127.0.0.1:4002",
    apiKey: "fake-carrier-key-for-dev",
    timeoutMs: 2_000,
    attempts: 3,
    dispatcher: carrierAgent,
    onRetry: ({ attempt, delayMs, err }) => logger.warn({ attempt, delayMs, reason: (err as Error).message }, "retrying carrier call"),
});

const app = createApp(
    {
        books: new BookService(bookRepository),
        orders: new OrderService(bookRepository, new InMemoryOrderRepository(catalog, new Map())),
        readiness: async () => ({ status: "ready", checks: {} }),
        carrier,
        paymentEvents: async () => true,
        catalogParser: async () => ({ ok: false, reason: "not used" }),
    },
    {
        corsOrigins: [],
        trustProxy: 0,
        rateLimitPerMinute: 100,
        paymentWebhookSecret: "test-webhook-secret-not-real",
        // Lab only: one fixed token for a store staff member, no signatures
        verifyToken: async (token) => {
            if (token !== "lab-staff") {
                throw new InvalidTokenError("ERR_LAB_UNKNOWN_TOKEN");
            }
            return { userId: "grace", roles: ["StoreStaff"] };
        },
    },
);
const port = Number(process.env.PORT ?? 3000);
app.listen(port, "127.0.0.1", () => logger.info(`lab API on http://127.0.0.1:${port}`));
