import type sql from "mssql";

export interface ReadinessReport {
    status: "ready" | "not-ready";
    checks: Record<string, string>;
}

export type ReadinessCheck = () => Promise<ReadinessReport>;

// Shared between the shutdown code (writes) and the readiness endpoint (reads)
export interface Lifecycle {
    shuttingDown: boolean;
}

// Ready = not shutting down and SQL Server answers within a second
export function createReadinessCheck(pool: sql.ConnectionPool, lifecycle: Lifecycle): ReadinessCheck {
    return async (): Promise<ReadinessReport> => {
        if (lifecycle.shuttingDown) {
            return { status: "not-ready", checks: { shutdown: "in progress" } };
        }
        try {
            const request = pool.request();
            const timer = setTimeout(() => request.cancel(), 1_000);
            await request.query("SELECT 1 AS ok").finally(() => clearTimeout(timer));
            return { status: "ready", checks: { sql: "ok" } };
        } catch (err) {
            return { status: "not-ready", checks: { sql: (err as Error).message } };
        }
    };
}
