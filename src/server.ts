import { createApp } from "./app.ts";
import { createServices } from "./composition.ts";
import { openPool } from "./db/pool.ts";
import { ConfigError, loadConfig, type AppConfig } from "./config.ts";
import { logger } from "./diagnostics/logger.ts";
import { createReadinessCheck, type Lifecycle } from "./lifecycle/readiness.ts";
import { registerGracefulShutdown } from "./lifecycle/shutdown.ts";

let config: AppConfig;
try {
    config = loadConfig();
} catch (err) {
    // Fail fast with every problem listed; nothing has been started yet
    console.error((err as ConfigError).message);
    process.exit(1);
}
logger.level = config.logLevel;
logger.info({ config }, "configuration loaded");

// Top-level await: the API starts listening only after the first SQL connection succeeded
const pool = await openPool(config.db.connectionString);
const lifecycle: Lifecycle = { shuttingDown: false };
const app = createApp({ ...createServices(pool), readiness: createReadinessCheck(pool, lifecycle) });
app.set("env", config.env);

// Express 5 passes listen errors (EADDRINUSE...) to this callback instead of throwing them
const server = app.listen(config.port, config.host, (err?: Error) => {
    if (err) {
        logger.fatal({ err }, `cannot listen on ${config.host}:${config.port}`);
        process.exit(1);
    }
    logger.info({ url: `http://${config.host}:${config.port}`, env: config.env, pid: process.pid }, "Bookstore API listening");
});

// app.listen returns the node:http server, so the timeouts from post 10 still apply here
server.headersTimeout = 10_000;
server.requestTimeout = 30_000;
server.keepAliveTimeout = 5_000;

registerGracefulShutdown({ server, pool, lifecycle, config: config.shutdown });
