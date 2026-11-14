import express from "express";
import { requestContext } from "#middleware/requestContext";
import { notFound, errorHandler } from "#middleware/errorHandler";
import { health } from "#routes/health";
import { booksRouter } from "#routes/books";
import { ordersRouter } from "#routes/orders";
import { reportsRouter } from "#routes/reports";
import { diagnosticsRouter } from "#routes/diagnostics";
import { coversRoot } from "#storage/coverStorage";

// Builds the Express app without listening, so tests can use it without a port (post 16)
export function createApp(): express.Express {
    const app = express();

    // Global middleware runs for every request, in this order
    app.use(requestContext);
    app.use(express.json({ limit: "64kb" }));

    // Book covers saved in post 08; ETag and Last-Modified come for free, dotfiles are ignored
    app.use("/covers", express.static(coversRoot, { maxAge: "1d", index: false }));

    app.get("/health", health);
    app.use("/api/books", booksRouter);
    app.use("/api/orders", ordersRouter);
    app.use("/api/reports", reportsRouter);
    app.use("/diagnostics", diagnosticsRouter);

    // Nothing matched: 404; then the error handler, always last
    app.use(notFound);
    app.use(errorHandler);
    return app;
}
