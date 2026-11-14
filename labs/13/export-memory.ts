// Usage: node labs/13/export-memory.ts all|stream  -> reads every order line once and reports peak RSS
import sql from "mssql";
import { Writable } from "node:stream";
import { pipeline } from "node:stream/promises";

const mode = process.argv[2] ?? "stream";
const query = `SELECT o.Id, o.CreatedAtUtc, o.CustomerId, i.Isbn, b.Title, i.Quantity, i.UnitPrice, o.Currency, o.Status
               FROM dbo.Orders AS o JOIN dbo.OrderItems AS i ON i.OrderId = o.Id JOIN dbo.Books AS b ON b.Isbn = i.Isbn
               ORDER BY o.Id, i.LineNumber`;

const config = sql.ConnectionPool.parseConnectionString(process.env.DB_CONNECTION_STRING!);
const pool = await new sql.ConnectionPool({ ...config, requestTimeout: 120_000 }).connect();
let peak = 0;
const sampler = setInterval(() => (peak = Math.max(peak, process.memoryUsage.rss())), 50);
const started = performance.now();
let rows = 0;

if (mode === "all") {
    // WRONG for big results: the whole recordset is materialised as JavaScript objects first
    const result = await pool.request().query(query);
    rows = result.recordset.length;
} else {
    // Rows arrive one by one; the request pauses while the consumer is behind
    const request = pool.request();
    const source = request.toReadableStream({ highWaterMark: 500 });
    request.query(query);
    await pipeline(source, new Writable({ objectMode: true, write(row, enc, cb) { rows++; cb(); } }));
}

clearInterval(sampler);
peak = Math.max(peak, process.memoryUsage.rss());
console.log(`${mode}: ${rows} rows in ${Math.round(performance.now() - started)} ms, peak RSS ${Math.round(peak / 1024 / 1024)} MB`);
await pool.close();
