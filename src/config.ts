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
    // Comma-separated allow-list of browser origins (the Angular client); never "*"
    CORS_ORIGINS: z
        .string()
        .default("http://localhost:4200")
        .transform((text) => text.split(",").map((o) => o.trim()))
        .pipe(z.array(z.url({ protocol: /^https?$/ }))),
    // How many proxies in front of the API may add X-Forwarded-For: 0 = none, 1 = one load balancer
    TRUST_PROXY: z.coerce.number().int().min(0).max(5).default(0),
    RATE_LIMIT_PER_MINUTE: z.coerce.number().int().min(1).default(120),
    AUTH_ISSUER: z.string().min(1),
    AUTH_AUDIENCE: z.string().min(1).default("bookstore-api"),
    AUTH_JWKS_URL: z.url({ protocol: /^https?$/ }),
    CARRIER_API_URL: z.url({ protocol: /^https?$/ }).default("http://127.0.0.1:4002"),
    // Optional only because the dev carrier accepts any value; set it everywhere else
    CARRIER_API_KEY: z.string().default(""),
    CARRIER_TIMEOUT_MS: z.coerce.number().int().min(100).max(30_000).default(2_000),
    CARRIER_ATTEMPTS: z.coerce.number().int().min(1).max(5).default(3),
});

export interface AppConfig {
    env: "development" | "production" | "test";
    host: string;
    port: number;
    logLevel: "fatal" | "error" | "warn" | "info" | "debug" | "trace";
    db: { connectionString: string };
    shutdown: { drainMs: number; timeoutMs: number };
    http: { corsOrigins: string[]; trustProxy: number; rateLimitPerMinute: number };
    auth: { issuer: string; audience: string; jwksUrl: string };
    carrier: { baseUrl: string; apiKey: string; timeoutMs: number; attempts: number };
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
        http: Object.freeze({ corsOrigins: e.CORS_ORIGINS, trustProxy: e.TRUST_PROXY, rateLimitPerMinute: e.RATE_LIMIT_PER_MINUTE }),
        auth: Object.freeze({ issuer: e.AUTH_ISSUER, audience: e.AUTH_AUDIENCE, jwksUrl: e.AUTH_JWKS_URL }),
        carrier: Object.freeze({ baseUrl: e.CARRIER_API_URL, apiKey: e.CARRIER_API_KEY, timeoutMs: e.CARRIER_TIMEOUT_MS, attempts: e.CARRIER_ATTEMPTS }),
    });
}
