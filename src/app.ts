import express from "express";
import { requestContext } from "#middleware/requestContext";
import { notFound, errorHandler } from "#middleware/errorHandler";
import { health, readiness } from "#routes/health";
import { booksRouter } from "#routes/books";
import { ordersRouter } from "#routes/orders";
import { reportsRouter } from "#routes/reports";
import { diagnosticsRouter } from "#routes/diagnostics";
import { coversRoot } from "#storage/coverStorage";
import type { BookService } from "#services/bookService";
import type { OrderService } from "#services/orderService";
import type { ReadinessCheck } from "./lifecycle/readiness.ts";

export interface AppServices {
    books: BookService;
    orders: OrderService;
    readiness: ReadinessCheck;
}

// Builds the Express app without listening; services come in from outside (constructor injection, no container)
export function createApp(services: AppServices): express.Express {
    const app = express();

    // Global middleware runs for every request, in this order
    app.use(requestContext);
    app.use(express.json({ limit: "64kb" }));

    // Book covers saved in post 08; ETag and Last-Modified come for free, dotfiles are ignored
    app.use("/covers", express.static(coversRoot, { maxAge: "1d", index: false }));

    app.get("/health", health);
    app.get("/health/ready", readiness(services.readiness));
    app.use("/api/books", booksRouter(services.books));
    app.use("/api/orders", ordersRouter(services.orders));
    app.use("/api/reports", reportsRouter);
    app.use("/diagnostics", diagnosticsRouter);

    // Nothing matched: 404; then the error handler, always last
    app.use(notFound);
    app.use(errorHandler);
    return app;
}
