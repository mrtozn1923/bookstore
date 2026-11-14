// Usage: node --env-file=.env scripts/db-setup.ts  -> creates the database if needed and runs sql/schema.sql
import { readFile } from "node:fs/promises";
import sql from "mssql";

const connectionString = process.env.DB_CONNECTION_STRING;
if (!connectionString) {
    console.error("DB_CONNECTION_STRING is not set");
    process.exit(2);
}
const config = sql.ConnectionPool.parseConnectionString(connectionString);
const database = config.database!;

// CREATE DATABASE runs against master; the name cannot be a parameter, so it is checked and quoted instead
if (!/^\w{1,100}$/.test(database)) {
    throw new Error(`unexpected database name: ${database}`);
}
const master = await new sql.ConnectionPool({ ...config, database: "master" }).connect();
await master.request().input("name", sql.NVarChar(128), database).query(`IF DB_ID(@name) IS NULL CREATE DATABASE [${database}]`);
await master.close();

// GO is a sqlcmd/SSMS separator, not T-SQL: split the script into batches ourselves
const pool = await new sql.ConnectionPool(config).connect();
const script = await readFile(new URL("../sql/schema.sql", import.meta.url), "utf8");
const batches = script.split(/^\s*GO\s*$/m).filter((b) => b.trim() !== "");
for (const batch of batches) {
    await pool.request().batch(batch);
}
await pool.close();
console.log(`database ${database} is ready (${batches.length} batches)`);
