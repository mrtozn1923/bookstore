import { z } from "zod";

// Everything the process reads from its environment, validated once at startup (appsettings + Options pattern)
const environmentSchema = z.object({
    NODE_ENV: z.enum(["development", "production", "test"]).default("development"),
    HOST: z.string().min(1).default("127.0.0.1"),
    PORT: z.coerce.number().int().min(1).max(65535).default(3000),
    LOG_LEVEL: z.enum(["fatal", "error", "warn", "info", "debug", "trace"]).default("info"),
    DB_CONNECTION_STRING: z.string().min(1),
    SHUTDOWN_DRAIN_MS: z.coerce.number().int().min(0).max(60_000).default(0),
    SHUTDOWN_TIMEOUT_MS: z.coerce.number().int().min(1_000).max(120_000).default(10_000),
});

export interface AppConfig {
    env: "development" | "production" | "test";
    host: string;
    port: number;
    logLevel: "fatal" | "error" | "warn" | "info" | "debug" | "trace";
    db: { connectionString: string };
    shutdown: { drainMs: number; timeoutMs: number };
}

export class ConfigError extends Error {
    constructor(details: string) {
        super(`invalid configuration:\n${details}`);
        this.name = "ConfigError";
    }
}

// Reads process.env (or a test's own object); every problem is reported at once, not one per restart
export function loadConfig(environment: Record<string, string | undefined> = process.env): AppConfig {
    const result = environmentSchema.safeParse(environment);
    if (!result.success) {
        throw new ConfigError(z.prettifyError(result.error));
    }
    const e = result.data;
    return Object.freeze({
        env: e.NODE_ENV,
        host: e.HOST,
        port: e.PORT,
        logLevel: e.LOG_LEVEL,
        db: Object.freeze({ connectionString: e.DB_CONNECTION_STRING }),
        shutdown: Object.freeze({ drainMs: e.SHUTDOWN_DRAIN_MS, timeoutMs: e.SHUTDOWN_TIMEOUT_MS }),
    });
}
