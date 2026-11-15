import type { Server } from "node:http";
import { setTimeout as delay } from "node:timers/promises";
import type sql from "mssql";
import type { AppConfig } from "../config.ts";
import type { Lifecycle } from "./readiness.ts";
import { logger } from "../diagnostics/logger.ts";

interface ShutdownTargets {
    server: Server;
    pool: sql.ConnectionPool;
    lifecycle: Lifecycle;
    config: AppConfig["shutdown"];
    // Outgoing connection pools, worker pools...: closed after the server, before the database
    closeAfterServer?: Array<() => Promise<unknown>>;
}

// SIGTERM (orchestrators, docker stop) and SIGINT (Ctrl+C) start the same graceful shutdown
export function registerGracefulShutdown({ server, pool, lifecycle, config, closeAfterServer = [] }: ShutdownTargets): void {
    const shutdown = async (signal: NodeJS.Signals): Promise<void> => {
        if (lifecycle.shuttingDown) {
            return;
        }
        lifecycle.shuttingDown = true;
        const started = performance.now();
        logger.info({ signal }, "shutdown started: readiness now reports 503");

        // 1) Give the load balancer time to notice the 503 and stop sending traffic
        await delay(config.drainMs);

        // 2) No new connections; idle keep-alive connections close now, busy ones after their response
        const closed = new Promise<void>((resolve) => server.close(() => resolve()));
        const forced = delay(config.timeoutMs, "timeout" as const, { ref: false });
        if ((await Promise.race([closed.then(() => "closed" as const), forced])) === "timeout") {
            logger.warn({ timeoutMs: config.timeoutMs }, "requests still running at the deadline, closing their connections");
            server.closeAllConnections();
            await closed;
        }

        await Promise.allSettled(closeAfterServer.map((close) => close()));

        // 3) Only now close the database pool: no request can need it any more
        await pool.close();
        logger.info({ durationMs: Math.round(performance.now() - started) }, "shutdown complete");

        // Nothing should keep the event loop alive now; if something does, do not hang forever
        setTimeout(() => {
            logger.error("event loop still busy after shutdown, forcing exit");
            process.exit(1);
        }, 2_000).unref();
    };

    process.once("SIGTERM", shutdown);
    process.once("SIGINT", shutdown);
}
