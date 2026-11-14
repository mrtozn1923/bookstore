// Usage: node labs/13/stream-cancel.ts cancel|keep  -> stops reading after 1000 rows; when does SQL Server stop sending?
import sql from "mssql";

const mode = process.argv[2] ?? "cancel";
const config = sql.ConnectionPool.parseConnectionString(process.env.DB_CONNECTION_STRING!);
const pool = await new sql.ConnectionPool({ ...config, requestTimeout: 120_000 }).connect();
const request = pool.request();
const rows = request.toReadableStream({ highWaterMark: 500 });
const started = performance.now();
let read = 0;

// 'done' (or an error) marks the moment the request gives its connection back
const finished = new Promise<string>((resolve) => {
    request.on("done", () => resolve("done"));
    request.on("error", (err: sql.RequestError) => resolve(err.code ?? err.message));
});
request.query("SELECT i.OrderId, i.LineNumber, i.Isbn FROM dbo.OrderItems AS i ORDER BY i.OrderId, i.LineNumber");

for await (const row of rows) {
    if (++read === 1000) {
        break; // leaving for-await destroys the stream, as a client disconnect would
    }
}
if (mode === "cancel") {
    request.cancel();
}
console.log(`${mode}: stopped reading at ${Math.round(performance.now() - started)} ms, request ended with "${await finished}" at ${Math.round(performance.now() - started)} ms`);
await pool.close();
