import sql from "mssql";

const pool = await new sql.ConnectionPool(process.env.DB_CONNECTION_STRING!).connect();
const tx = new sql.Transaction(pool);
await tx.begin();

// A transaction owns exactly one connection: two requests at the same time do not fit
const results = await Promise.allSettled([
    new sql.Request(tx).query("SELECT COUNT(*) AS Books FROM dbo.Books"),
    new sql.Request(tx).query("SELECT COUNT(*) AS Orders FROM dbo.Orders"),
]);
for (const r of results) {
    console.log(r.status === "fulfilled" ? r.value.recordset[0] : `${r.reason.code}: ${r.reason.message}`);
}
await tx.rollback();
await pool.close();
