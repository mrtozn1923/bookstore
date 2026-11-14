// Usage: node --env-file=.env labs/13/seed-orders.ts 500000  -> test orders with 1-3 lines each, written set-based by SQL Server
import sql from "mssql";

const count = Number(process.argv[2] ?? 500_000);
// A set-based insert of a million rows takes longer than the default 15 s request timeout
const config = sql.ConnectionPool.parseConnectionString(process.env.DB_CONNECTION_STRING!);
const pool = await new sql.ConnectionPool({ ...config, requestTimeout: 600_000 }).connect();
const started = performance.now();
await pool.request().input("count", sql.Int, count).batch(`
    SET NOCOUNT ON;
    CREATE TABLE #o (i int PRIMARY KEY, Id int NOT NULL);
    INSERT #o (i, Id)
    SELECT i, NEXT VALUE FOR dbo.OrderNumbers OVER (ORDER BY i)
    FROM (SELECT TOP (@count) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects AS a CROSS JOIN sys.all_objects AS b) AS n;

    INSERT dbo.Orders (Id, CustomerId, Status, Currency, CreatedAtUtc)
    SELECT Id, CONCAT('customer-', RIGHT(CONCAT('0000', i % 50000), 5)),
           CHOOSE(i % 5 + 1, 'Pending', 'Paid', 'Shipped', 'Delivered', 'Cancelled'), 'TRY', DATEADD(SECOND, i * 7, '2026-01-01')
    FROM #o;

    WITH b AS (SELECT Isbn, Price, ROW_NUMBER() OVER (ORDER BY Isbn) - 1 AS k FROM dbo.Books WHERE Currency = 'TRY')
    INSERT dbo.OrderItems (OrderId, LineNumber, Isbn, Quantity, UnitPrice)
    SELECT o.Id, l.LineNumber, b.Isbn, (o.i + l.LineNumber) % 3 + 1, b.Price
    FROM #o AS o
    CROSS JOIN (VALUES (1), (2), (3)) AS l (LineNumber)
    JOIN b ON b.k = (o.i + l.LineNumber) % (SELECT COUNT(*) FROM b)
    WHERE l.LineNumber <= o.i % 3 + 1;`);
const totals = await pool.request().query("SELECT (SELECT COUNT(*) FROM dbo.Orders) AS Orders, (SELECT COUNT(*) FROM dbo.OrderItems) AS OrderItems");
console.log(totals.recordset[0], `${Math.round(performance.now() - started)} ms`);
await pool.close();
