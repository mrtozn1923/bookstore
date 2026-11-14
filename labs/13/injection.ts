import sql from "mssql";

const pool = await new sql.ConnectionPool(process.env.DB_CONNECTION_STRING!).connect();
const search = "x' OR 1=1 --";

// WRONG: user input glued into the SQL text
const glued = await pool.request().query(`SELECT Title FROM dbo.Books WHERE Title LIKE '%${search}%'`);
console.log(`string concatenation: ${glued.recordset.length} rows`);

// Right: the value travels as a parameter, SQL Server never parses it as SQL
const param = await pool.request().input("search", sql.NVarChar(102), `%${search}%`).query("SELECT Title FROM dbo.Books WHERE Title LIKE @search");
console.log(`parameter:            ${param.recordset.length} rows`);

// Tagged template: the same parameter, written as ${search}
const tagged = await pool.request().query`SELECT Title FROM dbo.Books WHERE Title LIKE ${"%" + search + "%"}`;
console.log(`tagged template:      ${tagged.recordset.length} rows`);

// A parameter is still a LIKE pattern: "%%" matches every title unless % and _ are escaped
const wildcard = await pool.request().input("search", sql.NVarChar(102), "%%%").query("SELECT Title FROM dbo.Books WHERE Title LIKE @search");
console.log(`unescaped "%%":       ${wildcard.recordset.length} rows`);

await pool.close();
