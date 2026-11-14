import sql from "mssql";

const pool = await new sql.ConnectionPool(process.env.DB_CONNECTION_STRING!).connect();

// 1) decimal -> JavaScript number: fine for prices, lossy beyond 2^53
const big = await pool.request().query(`
    SELECT CAST(19.99 AS decimal(10, 2)) AS Price,
           CAST(19.99 * 100 AS int) AS PriceMinor,
           CAST(12345678901234567.89 AS decimal(19, 2)) AS Huge,
           CAST(CAST(12345678901234567.89 AS decimal(19, 2)) AS varchar(30)) AS HugeText`);
console.log(big.recordset[0]);

// 2) datetime rounds to 1/300 s, datetime2(3) keeps milliseconds
const at = new Date("2026-11-14T10:00:00.005Z");
const times = await pool
    .request()
    .input("dt", sql.DateTime, at)
    .input("dt2", sql.DateTime2(3), at)
    .query("SELECT @dt AS AsDatetime, @dt2 AS AsDatetime2");
console.log(times.recordset[0]);

await pool.close();

// 3) useUTC: false reads the same stored value as local time of the Node process
const config = sql.ConnectionPool.parseConnectionString(process.env.DB_CONNECTION_STRING!);
for (const useUTC of [true, false]) {
    const p = await new sql.ConnectionPool({ ...config, options: { ...config.options, useUTC } }).connect();
    const r = await p.request().query("SELECT CAST('2026-11-14T10:00:00' AS datetime2(0)) AS CreatedAtUtc");
    console.log(`useUTC=${useUTC}:`, r.recordset[0].CreatedAtUtc.toISOString());
    await p.close();
}
