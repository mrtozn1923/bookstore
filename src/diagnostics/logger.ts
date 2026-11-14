import { pino } from "pino";
import { currentRequestContext } from "../context/requestContext.ts";

// One JSON line per entry on stdout; collecting and shipping logs is the platform's job
export const logger = pino({
    level: "info",
    base: { service: "bookstore-api" },
    timestamp: pino.stdTimeFunctions.isoTime,
    // "level":"info" instead of "level":30, as most log platforms expect
    formatters: { level: (label) => ({ level: label }) },
    // Added to every entry: the request id travels with AsyncLocalStorage, not as a parameter
    mixin() {
        const context = currentRequestContext();
        return context ? { requestId: context.requestId } : {};
    },
    // Secrets never reach the log, even when someone logs the whole object
    redact: {
        paths: ["config.db.connectionString", "*.password", "req.headers.authorization", "req.headers.cookie"],
        censor: "[redacted]",
    },
});
