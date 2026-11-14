import sql from "mssql";

const connectionString = process.env.DB_CONNECTION_STRING!;
const query = "SELECT TOP (10) Isbn, Title FROM dbo.Books ORDER BY Title";
const runs = 100;

// WRONG: a new pool (= a new TCP + TLS + login handshake) for every "request"
let started = performance.now();
for (let i = 0; i < runs; i++) {
    const pool = await new sql.ConnectionPool(connectionString).connect();
    await pool.request().query(query);
    await pool.close();
}
console.log(`new connection per request: ${Math.round(performance.now() - started)} ms`);

// Right: one pool for the process, connections are reused
const pool = await new sql.ConnectionPool(connectionString).connect();
started = performance.now();
for (let i = 0; i < runs; i++) {
    await pool.request().query(query);
}
console.log(`shared pool:                ${Math.round(performance.now() - started)} ms`);
await pool.close();
