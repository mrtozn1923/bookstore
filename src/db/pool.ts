import sql from "mssql";
import { logger } from "../diagnostics/logger.ts";

// One pool per process, opened at startup and shared by every repository
export async function openPool(connectionString: string): Promise<sql.ConnectionPool> {
    const config = sql.ConnectionPool.parseConnectionString(connectionString);
    const pool = new sql.ConnectionPool({
        ...config,
        pool: { max: 10, min: 0, idleTimeoutMillis: 30_000 },
    });
    // Errors on idle connections arrive here; without a listener they would crash the process
    pool.on("error", (err: Error) => logger.error({ err }, "sql pool error"));
    await pool.connect();
    return pool;
}
