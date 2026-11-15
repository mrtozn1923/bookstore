import express from "express";
import helmet from "helmet";
import cors from "cors";
import { rateLimit } from "express-rate-limit";
import { requestContext } from "#middleware/requestContext";
import { notFound, errorHandler } from "#middleware/errorHandler";
import { authenticate, requireRole } from "#middleware/auth";
import { health, readiness } from "#routes/health";
import { booksRouter } from "#routes/books";
import { ordersRouter } from "#routes/orders";
import { reportsRouter } from "#routes/reports";
import { diagnosticsRouter } from "#routes/diagnostics";
import { shipmentsRouter, type TrackingLookup } from "#routes/shipments";
import { paymentWebhookRouter, type PaymentEventHandler } from "./payments/paymentWebhook.ts";
import { catalogRouter } from "#routes/catalog";
import type { CatalogParser } from "#services/catalogParsePool";
import { coversRoot } from "#storage/coverStorage";
import { HttpError } from "#http/errors";
import type { BookService } from "#services/bookService";
import type { OrderService } from "#services/orderService";
import type { ReadinessCheck } from "./lifecycle/readiness.ts";
import type { TokenVerifier } from "./auth/tokenVerifier.ts";

export interface AppServices {
    books: BookService;
    orders: OrderService;
    readiness: ReadinessCheck;
    carrier: TrackingLookup;
    paymentEvents: PaymentEventHandler;
    catalogParser: CatalogParser;
}

export interface AppOptions {
    corsOrigins: readonly string[];
    trustProxy: number;
    rateLimitPerMinute: number;
    verifyToken: TokenVerifier;
    paymentWebhookSecret: string;
}

// Builds the Express app without listening; services come in from outside (constructor injection, no container)
export function createApp(services: AppServices, options: AppOptions): express.Express {
    const app = express();
    // helmet removes the header too; this keeps it off even for responses that never reach helmet
    app.disable("x-powered-by");
    // req.ip comes from X-Forwarded-For only for this many proxy hops; 0 = the socket address
    app.set("trust proxy", options.trustProxy);

    // Global middleware runs for every request, in this order
    app.use(requestContext);
    // same-site instead of same-origin: the Angular client on another port of the same site can show covers
    app.use(helmet({ crossOriginResourcePolicy: { policy: "same-site" } }));
    app.use(
        cors({
            origin: [...options.corsOrigins],
            methods: ["GET", "POST"],
            allowedHeaders: ["authorization", "content-type", "x-request-id"],
            exposedHeaders: ["location", "x-request-id"],
            maxAge: 600,
        }),
    );
    app.use(
        "/api",
        rateLimit({
            windowMs: 60_000,
            limit: options.rateLimitPerMinute,
            standardHeaders: "draft-8",
            legacyHeaders: false,
            // 429 as Problem Details, like every other error
            handler: (req, res, next) => next(new HttpError(429, "too many requests, try again later")),
        }),
    );
    // Before express.json: the webhook needs the raw bytes to check their signature
    app.use("/api/payments/webhook", paymentWebhookRouter(options.paymentWebhookSecret, services.paymentEvents));
    app.use(express.json({ limit: "64kb" }));

    // Book covers saved in post 08; ETag and Last-Modified come for free, dotfiles are ignored
    app.use("/covers", express.static(coversRoot, { maxAge: "1d", index: false }));

    const authenticated = authenticate(options.verifyToken);
    app.get("/health", health);
    app.get("/health/ready", readiness(services.readiness));
    app.use("/api/books", booksRouter(services.books));
    app.use("/api/orders", ordersRouter(services.orders, authenticated));
    app.use("/api/reports", authenticated, requireRole("StoreStaff"), reportsRouter);
    app.use("/api/shipments", authenticated, requireRole("StoreStaff"), shipmentsRouter(services.carrier));
    app.use("/api/catalog", authenticated, requireRole("StoreStaff"), catalogRouter(services.catalogParser));
    app.use("/diagnostics", authenticated, requireRole("Admin"), diagnosticsRouter);

    // Nothing matched: 404; then the error handler, always last
    app.use(notFound);
    app.use(errorHandler);
    return app;
}
