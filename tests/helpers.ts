import type { Book } from "#domain/book";
import books from "#db/books.json" with { type: "json" };
import { InMemoryBookRepository } from "#repositories/bookRepository";
import { InMemoryOrderRepository } from "#repositories/orderRepository";
import { BookService } from "#services/bookService";
import { OrderService } from "#services/orderService";
import { createApp, type AppOptions } from "../src/app.ts";
import { InvalidTokenError, type Principal } from "../src/auth/tokenVerifier.ts";
import { logger } from "../src/diagnostics/logger.ts";
import { TrackingNotFoundError } from "../src/integrations/carrierClient.ts";
import type { TrackingLookup } from "#routes/shipments";
import { createInMemoryPaymentEvents } from "../src/payments/paymentWebhook.ts";
import parseCatalog from "../src/workers/catalogParseWorker.ts";

export const WEBHOOK_SECRET = "test-webhook-secret-not-real";

// Tests read their results from assertions, not from log lines
logger.level = "silent";

export const catalog = books as Book[];
export const DUNE = "9780441172719";
export const EMMA = "9780141439587";

// Fake repositories: the in-memory implementations, with a stock level chosen per test
export function createTestServices(stock: Record<string, number> = { [DUNE]: 5, [EMMA]: 5 }) {
    const bookRepository = new InMemoryBookRepository(catalog);
    const orderRepository = new InMemoryOrderRepository(catalog, new Map(Object.entries(stock)));
    return {
        bookRepository,
        orderRepository,
        books: new BookService(bookRepository),
        orders: new OrderService(bookRepository, orderRepository),
    };
}

// A fake token verifier: tokens are just names, so tests never deal with keys or signatures
const PRINCIPALS: Record<string, Principal> = {
    "token-ada": { userId: "ada", roles: ["Customer"] },
    "token-alan": { userId: "alan", roles: ["Customer"] },
    "token-grace": { userId: "grace", roles: ["StoreStaff"] },
};

// A fake carrier that knows one parcel; the real client is tested against undici's MockAgent
export const fakeCarrier: TrackingLookup = {
    getTracking: async (trackingNumber) => {
        if (trackingNumber !== "BK1001TR") {
            throw new TrackingNotFoundError(trackingNumber);
        }
        return { trackingNumber, status: "in_transit", events: [] };
    },
};

export function createTestApp(stock?: Record<string, number>, options: Partial<AppOptions> = {}, carrier: TrackingLookup = fakeCarrier) {
    const services = createTestServices(stock);
    const app = createApp(
        { books: services.books, orders: services.orders, readiness: async () => ({ status: "ready", checks: {} }), carrier, paymentEvents: createInMemoryPaymentEvents(),
            // The worker's function called directly: route tests need no threads
            catalogParser: async (csv) => parseCatalog({ csv: new Uint8Array(csv).buffer }) },
        {
            corsOrigins: ["http://localhost:4200"],
            trustProxy: 0,
            rateLimitPerMinute: 1_000,
            paymentWebhookSecret: WEBHOOK_SECRET,
            verifyToken: async (token) => {
                const principal = PRINCIPALS[token];
                if (!principal) {
                    throw new InvalidTokenError("ERR_TEST_UNKNOWN_TOKEN");
                }
                return principal;
            },
            ...options,
        },
    );
    return { app, services };
}
